from aiogram.types import InlineKeyboardMarkup, InlineKeyboardButton

from config import AVAILABLE_FORMATS


def get_format_keyboard() -> InlineKeyboardMarkup:
    buttons = []

    row1 = [
        InlineKeyboardButton(
            text=AVAILABLE_FORMATS["mp3"]["label"],
            callback_data="format_mp3",
        ),
        InlineKeyboardButton(
            text=AVAILABLE_FORMATS["wav"]["label"],
            callback_data="format_wav",
        ),
    ]
    buttons.append(row1)

    row2 = [
        InlineKeyboardButton(
            text=AVAILABLE_FORMATS["ogg"]["label"],
            callback_data="format_ogg",
        ),
        InlineKeyboardButton(
            text=AVAILABLE_FORMATS["m4a"]["label"],
            callback_data="format_m4a",
        ),
    ]
    buttons.append(row2)

    return InlineKeyboardMarkup(
        inline_keyboard=buttons,
    )
