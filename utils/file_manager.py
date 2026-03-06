import logging
import uuid
from pathlib import Path

from config import TEMP_DIR

logger = logging.getLogger(__name__)


def generate_unique_filename(extension: str) -> str:
    unique_id = uuid.uuid4().hex
    return f"{unique_id}.{extension}"


def get_temp_path(filename: str) -> Path:
    return TEMP_DIR / filename


def ensure_temp_dir() -> None:
    TEMP_DIR.mkdir(parents=True, exist_ok=True)
    logger.info(f"Временная директория проверена: {TEMP_DIR}")


def remove_file(file_path: Path) -> bool:
    try:
        if file_path.exists():
            file_path.unlink()
            logger.debug(f"Файл удалён: {file_path}")
            return True
        logger.warning(f"Файл не найден для удаления: {file_path}")
        return False
    except OSError as e:
        logger.error(f"Ошибка при удалении файла {file_path}: {e}")
        return False


def cleanup_temp_files(*file_paths: Path) -> None:
    for file_path in file_paths:
        remove_file(file_path)


def get_file_extension(file_id: str, file_type: str) -> str:
    return "mp4"
