# 🎬 Video to Audio Converter Bot

[![Telegram](https://img.shields.io/badge/Telegram-Bot-blue?style=flat-square&logo=telegram)](https://t.me/)
[![Python](https://img.shields.io/badge/Python-3.11+-3776AB?style=flat-square&logo=python)](https://www.python.org/)
[![aiogram](https://img.shields.io/badge/aiogram-3.x-FF6F00?style=flat-square)](https://docs.aiogram.dev/)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-Required-007808?style=flat-square&logo=ffmpeg)](https://ffmpeg.org/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

Telegram-бот для извлечения аудио из видео. Поддерживает обычные видео, видео-заметки (кружки) и видеодокументы.

## ✨ Возможности

- 🎥 Извлечение аудио из видео файлов
- 📹 Конвертация video note (кружков)
- 📁 Поддержка видеодокументов
- 📊 Команда `/stats` (только администратор)
- 🎵 **Поддерживаемые форматы:**
  - MP3 (192kbps)
  - WAV (без сжатия)
  - OGG (160kbps)
  - M4A (192kbps)

## 🛠 Установка

### 1. Клонируйте репозиторий

```bash
git clone <repo-url>
cd inlinwai
```

### 2. Установите FFmpeg

| OS | Команда |
|---|---|
| **Windows** | [Скачать](https://ffmpeg.org/download.html) и добавить `bin` в PATH |
| **Linux** | `sudo apt update && sudo apt install ffmpeg` |
| **macOS** | `brew install ffmpeg` |

### 3. Настройте виртуальное окружение

```bash
python -m venv venv
venv\Scripts\activate  # Windows
source venv/bin/activate  # Linux/macOS
```

### 4. Установите зависимости

```bash
pip install -r requirements.txt
```

### 5. Настройте переменные окружения

```bash
cp .env.example .env
```

Отредактируйте `.env`:
```env
BOT_TOKEN=your_bot_token_here
ADMIN_USER_ID=123456789
```

### 6. Запустите бота

```bash
python bot.py
```

## 📝 Получение токена бота

1. Откройте [@BotFather](https://t.me/BotFather) в Telegram
2. Отправьте `/newbot`
3. Следуйте инструкциям, введите имя бота
4. Скопируйте полученный токен в `.env`

## 📊 Команда /stats

Доступна только администратору (по `ADMIN_USER_ID`).

Пример ответа:
```
📊 Статистика бота
---
⏱ Время работы: 56м 56с
🗓 Дата запуска: 2026-03-06 21:39:11
---
⚠️ Ошибок: 0
---
[ admin only ]
```

## 📂 Структура проекта

```
inlinwai/
├── bot.py                 # Точка входа
├── config.py              # Конфигурация
├── handlers/
│   ├── start.py           # Обработчик /start
│   ├── stats.py           # Обработчик /stats
│   └── video.py           # Обработчик видео
├── keyboards/
│   └── format_kb.py       # Inline клавиатуры
├── services/
│   └── converter.py       # Конвертация через FFmpeg
├── utils/
│   ├── file_manager.py    # Управление файлами
│   └── runtime_stats.py   # Метрики времени/ошибок
└── temp/                  # Временные файлы
```

## ⚙️ Конфигурация

| Параметр | Описание | По умолчанию |
|----------|----------|--------------|
| `BOT_TOKEN` | Токен Telegram бота | — |
| `ADMIN_USER_ID` | Telegram user id администратора | 0 |
| `MAX_FILE_SIZE` | Максимальный размер файла (байт) | 50 MB |
| `FFMPEG_TIMEOUT` | Таймаут конвертации (сек) | 300 |

## 📄 Лицензия

MIT License — подробности в файле [LICENSE](LICENSE).

---

⭐ Если этот проект полезен — поставьте звезду!
