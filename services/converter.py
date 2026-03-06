import asyncio
import logging
from pathlib import Path

from config import FFMPEG_TIMEOUT, AVAILABLE_FORMATS

logger = logging.getLogger(__name__)


class ConverterError(Exception):
    pass


async def convert_video_to_audio(
    input_path: Path,
    output_path: Path,
    format_name: str,
) -> Path:
    if format_name not in AVAILABLE_FORMATS:
        raise ConverterError(f"Неподдерживаемый формат: {format_name}")

    format_config = AVAILABLE_FORMATS[format_name]
    codec = format_config["codec"]
    bitrate = format_config.get("bitrate")

    command = [
        "ffmpeg",
        "-i", str(input_path),
        "-vn",
        "-acodec", codec,
    ]

    if bitrate:
        command.extend(["-b:a", bitrate])

    command.extend([
        "-y",
        str(output_path),
    ])

    logger.info(f"Запуск конвертации: {input_path} -> {output_path} (формат: {format_name})")
    logger.debug(f"FFmpeg команда: {' '.join(command)}")

    try:
        process = await asyncio.create_subprocess_exec(
            *command,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
        )

        stdout, stderr = await asyncio.wait_for(
            process.communicate(),
            timeout=FFMPEG_TIMEOUT,
        )

        if process.returncode != 0:
            logger.error(f"Ошибка FFmpeg: код {process.returncode}")
            raise ConverterError(f"FFmpeg вернул код {process.returncode}")

        if not output_path.exists():
            raise ConverterError("Выходной файл не был создан")

        logger.info(f"Конвертация успешно завершена: {output_path}")
        return output_path

    except asyncio.TimeoutError:
        logger.error(f"Таймаут при конвертации файла: {input_path}")
        raise ConverterError(f"Таймаут конвертации ({FFMPEG_TIMEOUT} сек)")
    except FileNotFoundError:
        logger.error("FFmpeg не найден. Убедитесь, что FFmpeg установлен и добавлен в PATH.")
        raise ConverterError("FFmpeg не найден. Установите FFmpeg и добавьте его в PATH.")
    except Exception as e:
        logger.error(f"Неожиданная ошибка при конвертации: {e}")
        raise ConverterError(f"Ошибка конвертации: {e}")
