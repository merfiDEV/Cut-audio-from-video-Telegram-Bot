import logging
from pathlib import Path
from typing import Dict, Any

from aiogram import Router, F
from aiogram.exceptions import TelegramBadRequest
from aiogram.types import Message, CallbackQuery, FSInputFile

from config import MAX_FILE_SIZE, AVAILABLE_FORMATS
from keyboards.format_kb import get_format_keyboard
from services.converter import convert_video_to_audio, ConverterError
from utils.file_manager import (
    generate_unique_filename,
    get_temp_path,
    cleanup_temp_files,
)
from utils.runtime_stats import increment_conversion

logger = logging.getLogger(__name__)

router = Router()

user_files: Dict[int, Dict[str, Any]] = {}


@router.message(F.video | F.video_note | F.document)
async def handle_video(message: Message) -> None:
    if message.video:
        file = message.video
        file_type = "video"
    elif message.video_note:
        file = message.video_note
        file_type = "video_note"
    elif message.document:
        if not message.document.mime_type or not message.document.mime_type.startswith("video"):
            return
        
        file = message.document
        file_type = "document"
    else:
        return

    if file.file_size and file.file_size > MAX_FILE_SIZE:
        max_size_mb = MAX_FILE_SIZE // (1024 * 1024)
        await message.answer(
            f"❌ Файл слишком большой.\n"
            f"Максимальный размер: {max_size_mb} MB"
        )
        logger.warning(
            f"Пользователь {message.from_user.id} отправил файл "
            f"размером {file.file_size} байт (лимит: {MAX_FILE_SIZE})"
        )
        return

    logger.info(
        f"Пользователь {message.from_user.id} отправил {file_type} "
        f"размером {file.file_size} байт"
    )

    try:
        input_filename = generate_unique_filename("mp4")
        input_path = get_temp_path(input_filename)

        await message.bot.download(file.file_id, destination=input_path)
        logger.debug(f"Файл скачан: {input_path}")

    except Exception as e:
        error_msg = str(e)
        logger.error(f"Ошибка при скачивании файла: {e}", exc_info=True)
        
        if "file is too big" in error_msg:
            max_size_mb = MAX_FILE_SIZE // (1024 * 1024)
            await message.answer(
                f"❌ <b>Ошибка</b>\n"
                f"Файл слишком большой для обработки.\n"
                f"Максимальный размер: {max_size_mb} MB"
            )
        else:
            await message.answer(
                "❌ <b>Ошибка</b>\n"
                "Произошла ошибка при обработке видео.\n"
                "Пожалуйста, попробуйте ещё раз."
            )
        return

    user_files[message.from_user.id] = {
        "input_path": input_path,
    }

    await message.answer(
        "🎵 Выберите формат аудио:",
        reply_markup=get_format_keyboard(),
    )


@router.callback_query(F.data.startswith("format_"))
async def handle_format_selection(callback: CallbackQuery) -> None:
    format_name = callback.data.replace("format_", "")

    logger.info(
        f"Пользователь {callback.from_user.id} выбрал формат: {format_name}"
    )

    file_info = user_files.get(callback.from_user.id)

    if not file_info:
        await callback.answer(
            "❌ Файл не найден. Отправьте видео ещё раз.",
            show_alert=True,
        )
        return

    input_path = file_info["input_path"]
    output_filename = f"@Spmoderbot_bot_{generate_unique_filename(format_name)}"
    output_path = get_temp_path(output_filename)

    format_label = AVAILABLE_FORMATS.get(format_name, {}).get("label", format_name.upper())

    status_message = await callback.message.edit_text(
        "⏳ <b>Обработка</b>\n"
        f"🔄 Извлечение аудио в {format_label}..."
    )

    try:
        converted_path = await convert_video_to_audio(
            input_path=input_path,
            output_path=output_path,
            format_name=format_name,
        )

        logger.info(f"Файл сконвертирован: {converted_path}")

        await status_message.edit_text(
            "⏳ <b>Обработка</b>\n"
            "📤 Отправка файла..."
        )

        audio_file = FSInputFile(converted_path)
        caption = (
            f"🎵 <b>Аудио из видео</b>\n"
            f"📦 Формат: {format_label}\n"
            f"👤 От: @Spmoderbot_bot"
        )

        if format_name == "ogg":
            await callback.message.answer_audio(
                audio=audio_file,
                caption=caption,
            )
        else:
            await callback.message.answer_document(
                document=audio_file,
                caption=caption,
            )

        increment_conversion(format_name)

        cleanup_temp_files(input_path, converted_path)
        logger.debug("Временные файлы удалены")

        user_files.pop(callback.from_user.id, None)

        await status_message.edit_text(
            "✅ <b>Готово</b>\n"
            f"Аудио в формате {format_label} отправлено!"
        )

        await callback.answer()

    except ConverterError as e:
        logger.error(f"Ошибка конвертации: {e}", exc_info=False)
        await status_message.edit_text(
            "❌ <b>Ошибка</b>\n"
            "Не удалось конвертировать видео в аудио.\n"
            "Возможно, файл повреждён или имеет неподдерживаемый формат."
        )
        cleanup_temp_files(input_path, output_path)
        user_files.pop(callback.from_user.id, None)

    except TelegramBadRequest as e:
        logger.error(f"Telegram API ошибка: {e}")
        cleanup_temp_files(input_path, output_path)
        user_files.pop(callback.from_user.id, None)

    except Exception as e:
        logger.error(f"Неожиданная ошибка: {e}", exc_info=True)
        await status_message.edit_text(
            "❌ <b>Ошибка</b>\n"
            "Произошла непредвиденная ошибка."
        )
        cleanup_temp_files(input_path, output_path)
        user_files.pop(callback.from_user.id, None)
