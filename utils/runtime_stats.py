from __future__ import annotations

from datetime import datetime, timedelta
from typing import Optional

_start_time: Optional[datetime] = None
_error_count = 0
_conversion_count = 0
_format_counts = {
    "mp3": 0,
    "wav": 0,
    "ogg": 0,
    "m4a": 0,
}


def mark_start() -> None:
    global _start_time
    _start_time = datetime.now()


def get_start_time() -> datetime:
    if _start_time is None:
        return datetime.now()
    return _start_time


def get_uptime(now: Optional[datetime] = None) -> timedelta:
    current_time = now or datetime.now()
    return current_time - get_start_time()


def increment_error() -> None:
    global _error_count
    _error_count += 1


def get_error_count() -> int:
    return _error_count


def increment_conversion(format_name: str) -> None:
    global _conversion_count
    _conversion_count += 1
    if format_name in _format_counts:
        _format_counts[format_name] += 1


def get_conversion_count() -> int:
    return _conversion_count


def get_format_counts() -> dict:
    return dict(_format_counts)
