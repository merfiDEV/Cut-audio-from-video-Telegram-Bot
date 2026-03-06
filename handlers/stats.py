from aiogram import Router
from aiogram.filters import Command
from aiogram.types import Message

from config import ADMIN_USER_ID
from utils.runtime_stats import (
    get_start_time,
    get_error_count,
    get_uptime,
    get_conversion_count,
    get_format_counts,
)

router = Router()


def format_uptime() -> str:
    uptime = get_uptime()
    total_seconds = int(uptime.total_seconds())
    hours, remainder = divmod(total_seconds, 3600)
    minutes, seconds = divmod(remainder, 60)

    if hours > 0:
        return f"{hours}ч {minutes}м {seconds}с"
    return f"{minutes}м {seconds}с"


@router.message(Command("stats"))
async def cmd_stats(message: Message) -> None:
    if message.from_user is None or message.from_user.id != ADMIN_USER_ID:
        await message.answer("⛔️ Доступ запрещен.\n[ admin only ]")
        return

    start_time = get_start_time().strftime("%Y-%m-%d %H:%M:%S")

    format_counts = get_format_counts()

    text = (
        "📊 Статистика бота\n\n"
        f"⏱ Время работы: {format_uptime()}\n"
        f"✅ Всего конвертаций: {get_conversion_count()}\n\n"
        "📦 По форматам:\n"
        f"   MP3: {format_counts.get('mp3', 0)}\n"
        f"   WAV: {format_counts.get('wav', 0)}\n"
        f"   OGG: {format_counts.get('ogg', 0)}\n"
        f"   M4A: {format_counts.get('m4a', 0)}\n\n"
        f"🗓 Дата запуска: {start_time}\n"
        "---\n"
        f"⚠️ Ошибок: {get_error_count()}\n"
        "---\n"
        "[ admin only ]"
    )

    await message.answer(text)
