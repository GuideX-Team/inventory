# 📦 Grid Inventory System - Структура проекта

## 📁 Структура папок

```
Assets/
├── 🎬 Scenes/                    # Сцены Unity
│   └── SampleScene.unity         # Демо-сцена с инвентарем
│
├── 📜 Scripts/                   # Все скрипты проекта
│   └── Inventory/                # Система grid-based инвентаря
│       ├── Core/                 # Базовые классы и интерфейсы
│       ├── Editor/               # Инструменты для редактора Unity
│       ├── Serialization/        # JSON сериализация
│       ├── Shared/               # Общие утилиты
│       ├── UI/                   # UI компоненты
│       ├── Inventory.cs          # Базовый класс инвентаря
│       ├── TetrisInventory.cs    # Grid-based инвентарь (стиль Resident Evil/Tetris)
│       ├── InventoryItem.cs      # Предметы инвентаря
│       ├── BackpackInventoryItem.cs  # Контейнеры/рюкзаки
│       ├── InventoryManager.cs   # Менеджер инвентаря
│       ├── InventoryView.cs      # Визуальное отображение
│       └── Grid2D.cs             # 2D сетка для размещения предметов
│
├── 🎨 ScriptableObjects/         # Scriptable Objects для данных
│   ├── Items/                    # Предметы (еда, медикаменты, броня и т.д.)
│   │   ├── Armor/
│   │   ├── Bags/
│   │   ├── Food/
│   │   ├── Helmet/
│   │   ├── Masks/
│   │   ├── Medicine/
│   │   ├── Vests/
│   │   └── Other/
│   └── Weapons/                  # Оружие
│       ├── Assault/              # Штурмовые винтовки
│       ├── Pistol/               # Пистолеты
│       ├── Rifle/                # Винтовки
│       ├── Shotgun/              # Дробовики
│       ├── SMG/                  # Пистолеты-пулеметы
│       ├── LMG/                  # Легкие пулеметы
│       ├── Melee/                # Холодное оружие
│       └── Grenade/              # Гранаты
│
├── 🖼️ Sprites/                   # Спрайты и изображения
│   └── Innawoods/                # Стиль спрайтов Innawoods
│       ├── Items/
│       └── Weapons/
│
├── 🎨 UI/                        # UI Toolkit файлы
│   └── UI Toolkit/               # UXML шаблоны и USS стили
│       ├── Data/
│       └── UnityThemes/
│
├── 🔌 Plugins/                   # Сторонние плагины (если есть)
│
└── ⚙️ Settings/                  # Настройки рендера и проекта
    ├── UniversalRP.asset         # Настройки Universal Render Pipeline
    └── Renderer2D.asset          # 2D рендерер

```

## 🎮 Основные компоненты

### Grid-based Inventory System
Система инвентаря в стиле Resident Evil/Tarkov с размещением предметов на сетке.

**Основные классы:**
- `TetrisInventory` - основной класс grid-based инвентаря
- `InventoryItem` - базовый класс для предметов
- `BackpackInventoryItem` - предметы-контейнеры (рюкзаки, сумки)
- `InventoryManager` - управление инвентарями
- `InventoryView` - визуализация инвентаря

**Особенности:**
- ✅ Предметы занимают несколько клеток
- ✅ Поддержка вложенных контейнеров (рюкзаки)
- ✅ Поворот предметов
- ✅ Drag & Drop интерфейс
- ✅ JSON сериализация для сохранения
- ✅ UI Toolkit для современного UI

## 🚀 Быстрый старт

1. Откройте сцену `Scenes/SampleScene.unity`
2. Запустите игру (Play Mode)
3. Используйте систему инвентаря

## 📝 Примечания

- Проект использует **Unity UI Toolkit** для интерфейса
- Рендеринг через **Universal Render Pipeline (URP)**
- Спрайты в стиле **Innawoods**
