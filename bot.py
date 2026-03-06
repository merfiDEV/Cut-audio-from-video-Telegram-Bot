import asyncio
import logging
import sys
from pathlib import Path

from aiogram import Bot, Dispatcher
from aiogram.client.default import DefaultBotProperties
from aiogram.enums import ParseMode
from aiogram.filters import ExceptionTypeFilter
from aiogram.types import ErrorEvent

from config import BOT_TOKEN, TEMP_DIR
from handlers import start, video
from utils.file_manager import ensure_temp_dir

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(
            Path(__file__).parent / "bot.log",
            encoding="utf-8",
        ),
    ],
)

logger = logging.getLogger(__name__)


async def on_startup() -> None:
    logger.info("Бот запускается...")
    ensure_temp_dir()
    logger.info("Временная директория готова")


async def on_shutdown() -> None:
    logger.info("Бот останавливается...")


async def global_error_handler(event: ErrorEvent) -> None:
    logger.error(
        f"Глобальная ошибка: {event.exception}",
        exc_info=event.exception,
    )

    if event.update.message:
        try:
            await event.update.message.answer(
                "❌ Произошла ошибка при обработке запроса.\n"
                "Пожалуйста, попробуйте ещё раз позже."
            )
        except Exception as e:
            logger.error(f"Не удалось отправить сообщение об ошибке: {e}")


def create_dispatcher() -> Dispatcher:
    dp = Dispatcher()

    dp.include_router(start.router)
    dp.include_router(video.router)

    dp.error.register(global_error_handler, ExceptionTypeFilter(Exception))

    dp.startup.register(on_startup)
    dp.shutdown.register(on_shutdown)

    return dp


async def main() -> None:
    if not BOT_TOKEN:
        logger.error("BOT_TOKEN не найден в переменных окружения!")
        logger.error("Создайте .env файл с переменной BOT_TOKEN")
        sys.exit(1)

    bot = Bot(
        token=BOT_TOKEN,
        default=DefaultBotProperties(parse_mode=ParseMode.HTML),
    )

    dp = create_dispatcher()

    try:
        logger.info("Запуск бота в режиме polling...")
        await dp.start_polling(bot)
    except KeyboardInterrupt:
        logger.info("Получен сигнал остановки (KeyboardInterrupt)")
    except Exception as e:
        logger.error(f"Критическая ошибка: {e}", exc_info=True)
    finally:
        await bot.session.close()
        logger.info("Сессия бота закрыта")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("Бот остановлен пользователем")
    except Exception as e:
        logger.critical(f"Фатальная ошибка: {e}", exc_info=True)
        sys.exit(1)
