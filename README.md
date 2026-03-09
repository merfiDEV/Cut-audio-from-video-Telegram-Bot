# 🎬 Video to Audio Converter Bot (C#)

[![Platform](https://img.shields.io/badge/platform-windows%20%7C%20linux%20%7C%20macos-2ea44f?style=flat-square)](https://dotnet.microsoft.com/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4?style=flat-square)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-512bd4?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Telegram](https://img.shields.io/badge/Telegram-Bot-2ca5e0?style=flat-square&logo=telegram)](https://t.me/)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-Required-007808?style=flat-square&logo=ffmpeg)](https://ffmpeg.org/)
[![License](https://img.shields.io/badge/License-MIT-4cc61e?style=flat-square)](LICENSE)

[![Попробовать в Telegram](https://img.shields.io/badge/Попробовать%20бота-@Spmoderbot__bot-2ca5e0?style=for-the-badge&logo=telegram&logoColor=white)](https://t.me/Spmoderbot_bot)

Open source Telegram-бот для извлечения аудио из видео. Поддерживает обычные видео, видео-заметки (кружки) и видеодокументы.

## ✨ Возможности

- 🎥 Извлечение аудио из видео файлов
- 📹 Конвертация video note (кружков)
- 📁 Поддержка видеодокументов
- 📊 Команда `/stats` (только администратор)
- 🎵 **Поддерживаемые форматы:** MP3, WAV, OGG, M4A

## ✅ Плюсы бота

| Плюс | Описание |
|---|---|
| ⚡ Быстрая конвертация | Оптимизированный пайплайн на FFmpeg с прозрачными логами |
| 📁 Универсальная поддержка | Видео, кружки и видеодокументы в одном обработчике |
| 🎯 Четкий выбор формата | Удобные inline-кнопки для MP3/WAV/OGG/M4A |
| 🧠 Статистика администратора | /stats с аптаймом, ошибками и счетчиками |
| 🧹 Чистый temp | Авто-очистка временных файлов после отправки |
| 🧩 Легко развивать | Четкая структура и разделение по слоям |

## 🧰 Требования

- .NET 10 SDK (для сборки) или .NET 10 Runtime (для запуска)
- FFmpeg в PATH

## ⚙️ Настройка окружения

Создай `.env` в корне репозитория:

```env
BOT_TOKEN=your_bot_token_here
ADMIN_USER_ID=123456789
MAX_FILE_SIZE=52428800
FFMPEG_TIMEOUT=300
```

Пояснение параметров:

- `BOT_TOKEN` — токен Telegram бота
- `ADMIN_USER_ID` — Telegram user id администратора
- `MAX_FILE_SIZE` — максимальный размер файла (в байтах)
- `FFMPEG_TIMEOUT` — таймаут конвертации (в секундах)

## 🚀 Быстрый запуск

```bash
dotnet restore csharp-bot/Bot.csproj
dotnet run --project csharp-bot/Bot.csproj
```

## 🛠 Сборка в .exe

### Вариант 1: обычная сборка (нужен .NET Runtime)

```bash
dotnet publish csharp-bot/Bot.csproj -c Release -r win-x64 --self-contained false
```

Готовый .exe появится в:

`csharp-bot/bin/Release/net10.0/win-x64/publish/Bot.exe`

### Вариант 2: self-contained (без установленного .NET)

```bash
dotnet publish csharp-bot/Bot.csproj -c Release -r win-x64 --self-contained true
```

Готовый .exe появится в:

`csharp-bot/bin/Release/net10.0/win-x64/publish/Bot.exe`

## ▶️ Запуск .exe

1) Убедись, что рядом с `Bot.exe` есть `.env` и установлен FFmpeg.
2) Запусти:

```bash
csharp-bot\bin\Release\net10.0\win-x64\publish\Bot.exe
```

## 📂 Структура проекта

```
csharp-bot/
├── Program.cs                 # Точка входа
├── Config/
│   └── AppConfig.cs            # Конфигурация
├── Handlers/
│   ├── StartHandler.cs         # /start
│   ├── StatsHandler.cs         # /stats
│   ├── VideoHandler.cs         # Обработка видео
│   └── UpdateDispatcher.cs     # Диспетчер обновлений
├── Keyboards/
│   ├── FormatKeyboard.cs       # Inline клавиатуры
│   └── GithubKeyboard.cs       # GitHub кнопка
├── Services/
│   └── Converter.cs            # Конвертация через FFmpeg
└── Utils/
    ├── Env.cs                  # Чтение переменных окружения
    ├── EnvLoader.cs            # Загрузка .env
    ├── FileManager.cs          # Управление файлами
    ├── RuntimeStats.cs         # Метрики времени/ошибок
    └── SimpleLogger.cs         # Логи
```

## 📊 Команда /stats

Доступна только администратору (по `ADMIN_USER_ID`).

Пример ответа:

```
📊 Статистика бота

⏱ Время работы: 56м 56с
✅ Всего конвертаций: 12

📦 По форматам:
   MP3: 8
   WAV: 2
   OGG: 1
   M4A: 1

🗓 Дата запуска: 2026-03-09 10:10:10
---
⚠️ Ошибок: 0
---
[ admin only ]
```

## 📄 Лицензия

MIT License — подробности в файле [LICENSE](LICENSE).

---

⭐ Если проект полезен — поставь звезду!
