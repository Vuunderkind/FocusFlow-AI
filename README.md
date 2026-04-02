<div align="center">

# ✦ FocusFlow AI

**Платформа для объединения и автоматизации искусственного интеллекта**

[![.NET](https://img.shields.io/badge/.NET-10.0-7C5CFC?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-9B7FFF?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-4CAF50?style=for-the-badge)](LICENSE)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)

*Умный AI-агент с поддержкой 5 провайдеров, автоматической маршрутизацией и FUSION-режимом*

</div>

---

## 🌟 О проекте

**FocusFlow AI** — это десктопное Windows-приложение, которое объединяет несколько ведущих AI-провайдеров в единую платформу. Вместо того чтобы использовать каждую модель отдельно, FocusFlow автоматически выбирает лучший ИИ под каждый запрос или комбинирует несколько моделей для получения максимально качественного ответа.

> Практическая работа по дисциплине **ПМ3 «Разработка программных модулей для компьютерных систем»**  
> Колледж Hexlet, Алматы

---

## ✨ Ключевые возможности

### 🤖 Три режима работы с ИИ

| Режим | Описание |
|-------|----------|
| **✦ AUTO** | Система автоматически анализирует запрос и выбирает оптимальную модель — максимум качества при минимуме расходов |
| **⚡ FUSION** | Два разных ИИ отвечают параллельно, третья модель синтезирует лучшее из обоих ответов |
| **☰ ВРУЧНУЮ** | Пользователь сам выбирает провайдера и конкретную модель |

### 🌐 Поддерживаемые AI-провайдеры

<table>
<tr>
<td align="center"><b>Anthropic</b><br>Claude 3.5 Sonnet<br>Claude 3 Haiku</td>
<td align="center"><b>OpenAI</b><br>GPT-4o<br>GPT-4o Mini</td>
<td align="center"><b>Google</b><br>Gemini 1.5 Pro<br>Gemini 1.5 Flash</td>
<td align="center"><b>Mistral</b><br>Mistral Large<br>Mistral Small</td>
<td align="center"><b>Groq</b><br>Llama 3.1 70B<br>Mixtral 8x7B</td>
</tr>
</table>

### 📋 Остальные функции

- 💬 **Чат с историей** — все разговоры сохраняются в SQLite, поиск и закрепление чатов
- 🤖 **AI-агенты** — 8 встроенных специализированных агентов (кодер, писатель, аналитик, маркетолог и др.)
- ⚡ **Автоматизация** — визуальный конструктор воркфлоу: цепочки шагов с разными агентами
- 🕐 **История** — просмотр всех прошлых диалогов с фильтрацией
- ⚙️ **Настройки** — управление API-ключами, температурой, токенами, режимом оркестрации

---

## 🏗️ Архитектура

```
FocusFlow LMS/
├── Controls/               # Кастомные UI-компоненты
│   ├── MessageBubble.cs    # Пузыри сообщений (кастомная отрисовка)
│   ├── RoundedPanel.cs     # Скруглённые панели, кнопки с градиентом
│   ├── AgentCard.cs        # Карточка агента
│   └── InputDialog.cs      # Диалог ввода текста
│
├── Data/                   # Репозитории (паттерн Repository)
│   ├── DatabaseManager.cs  # SQLite, инициализация БД
│   ├── ConversationRepository.cs
│   ├── MessageRepository.cs
│   ├── AgentRepository.cs
│   └── WorkflowRepository.cs
│
├── Forms/                  # Страницы приложения
│   ├── MainForm.cs         # Главное окно, навигация, сайдбар
│   ├── ChatPage.cs         # Чат с AUTO/FUSION/MANUAL режимами
│   ├── AgentsPage.cs       # Управление агентами
│   ├── AutomationPage.cs   # Конструктор воркфлоу
│   ├── HistoryPage.cs      # История диалогов
│   └── SettingsPage.cs     # Настройки и API-ключи
│
├── Models/                 # Модели данных
│   ├── AIProvider.cs       # ProviderType, ModelInfo, RouteDecision
│   ├── AppConfig.cs        # Конфигурация приложения
│   ├── AIAgent.cs          # Модель агента
│   ├── Conversation.cs     # Разговор
│   ├── Message.cs          # Сообщение (AiMessage)
│   └── WorkflowStep.cs     # Шаг воркфлоу
│
└── Services/               # Бизнес-логика
    ├── ILLMProvider.cs         # Интерфейс провайдера
    ├── AnthropicProvider.cs    # Anthropic Claude API
    ├── OpenAICompatProvider.cs # OpenAI / Mistral / Groq
    ├── GeminiProvider.cs       # Google Gemini API
    ├── ProviderRegistry.cs     # Реестр всех провайдеров
    ├── AIRouter.cs             # Умная маршрутизация запросов
    ├── OrchestrationService.cs # AUTO / FUSION / MANUAL логика
    ├── AIService.cs            # Фасад для работы с ИИ
    └── WorkflowService.cs      # Выполнение воркфлоу
```

### 🧠 Как работает умная маршрутизация (AUTO)

```
Запрос пользователя
       ↓
  AIRouter.Classify()        ← анализ ключевых слов
  (Code / Math / Creative /
   Analysis / Research / Simple)
       ↓
  Scoring для каждой модели:
  QualityScore × 10
  + бонус за тип запроса
  − штраф за стоимость
       ↓
  Выбирается лучшая модель
       ↓
  ProviderRegistry → нужный провайдер → API
```

### ⚡ Как работает FUSION

```
Запрос пользователя
       ↓
  AIRouter.PickFusionPair()  ← топ-2 модели разных провайдеров
       ↓
  Task.WhenAll([Model A, Model B])  ← параллельные запросы
       ↓
  Синтез: "Объедини два ответа в один лучший"
  → самая быстрая/дешёвая доступная модель
       ↓
  Итоговый ответ пользователю
```

---

## 🚀 Быстрый старт

### Требования

- Windows 10/11
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- API-ключ хотя бы одного провайдера

### Установка и запуск

```bash
# Клонировать репозиторий
git clone https://github.com/Vuunderkind/FocusFlow-AI.git
cd FocusFlow-AI

# Собрать и запустить
cd "FocusFlow LMS"
dotnet run
```

При первом запуске откроется диалог с предложением добавить API-ключ в **Настройки**.

### Получение API-ключей

| Провайдер | Где получить | Бесплатный тариф |
|-----------|-------------|-----------------|
| Anthropic | [console.anthropic.com](https://console.anthropic.com) | $5 при регистрации |
| OpenAI | [platform.openai.com](https://platform.openai.com) | $5 при регистрации |
| Google Gemini | [aistudio.google.com](https://aistudio.google.com) | ✅ Есть |
| Mistral | [console.mistral.ai](https://console.mistral.ai) | ✅ Есть |
| Groq | [console.groq.com](https://console.groq.com) | ✅ Есть (быстрый) |

---

## 🎨 Дизайн

Приложение использует тёмную тему с фиолетовой палитрой:

| Элемент | Цвет | HEX |
|---------|------|-----|
| Фон | Почти чёрный | `#0D0D12` |
| Акцент | Фиолетовый | `#7C5CFC` |
| Акцент светлый | Лавандовый | `#9B7FFF` |
| Карточки | Тёмно-синий | `#1A1A26` |
| Текст | Белый | `#E8E8F0` |

Все UI-элементы нарисованы вручную через `Graphics.DrawPath` — без сторонних UI-библиотек.

---

## 🗄️ База данных

SQLite (`%AppData%\FocusFlowAI\focusflow.db`), схема 3НФ:

```sql
Conversations  →  Messages
Agents         →  (используются в чатах)
Workflows      →  WorkflowSteps
```

---

## 🛠️ Стек технологий

| Технология | Версия | Назначение |
|-----------|--------|-----------|
| C# / .NET | 10.0 | Язык и платформа |
| Windows Forms | 10.0 | UI-фреймворк |
| SQLite (`Microsoft.Data.Sqlite`) | 9.0.4 | Локальная база данных |
| Newtonsoft.Json | 13.0.3 | Сериализация конфига |
| HttpClient | встроен | HTTP-запросы к API |

---

## 📄 Лицензия

MIT License — свободное использование, модификация и распространение.

---

<div align="center">

Сделано с ❤️ в рамках практики в **Hexlet College**, Алматы

</div>
