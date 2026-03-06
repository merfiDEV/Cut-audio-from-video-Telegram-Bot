import logging

from aiogram import Router, F
from aiogram.filters import Command
from aiogram.types import Message

logger = logging.getLogger(__name__)

router = Router()


@router.message(Command("start"))
async def cmd_start(message: Message) -> None:
    logger.info(f"Пользователь {message.from_user.id} использовал команду /start")

    await message.answer(
        "👋 Привет! Я бот для извлечения аудио из видео.\n\n"
        "📹 Отправь мне видео или видео-сообщение (кружок),\n"
        "и я предложу выбрать формат аудио для конвертации.\n\n"
        "🎧 Поддерживаемые форматы: MP3, WAV, OGG, M4A",
    )
