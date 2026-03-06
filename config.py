import os
from pathlib import Path

from dotenv import load_dotenv

load_dotenv()

BOT_TOKEN = os.getenv("BOT_TOKEN")

BASE_DIR = Path(__file__).parent.resolve()

TEMP_DIR = BASE_DIR / "temp"

MAX_FILE_SIZE = int(os.getenv("MAX_FILE_SIZE", 50 * 1024 * 1024))

FFMPEG_TIMEOUT = int(os.getenv("FFMPEG_TIMEOUT", 300))

AVAILABLE_FORMATS = {
    "mp3": {
        "label": "MP3",
        "codec": "libmp3lame",
        "bitrate": "192k",
    },
    "wav": {
        "label": "WAV",
        "codec": "pcm_s16le",
        "bitrate": None,
    },
    "ogg": {
        "label": "OGG",
        "codec": "libvorbis",
        "bitrate": "160k",
    },
    "m4a": {
        "label": "M4A",
        "codec": "aac",
        "bitrate": "192k",
    },
}
