# Реализация синхронизации с Bitrix24

## Обзор изменений

Данный документ описывает реализацию полноценной синхронизации с Bitrix24, включая:
- Добавление сущности порталов (BitrixPortal)
- Привязка пользователей и других сущностей к порталам
- Настройки синхронизации
- Обработчик входящих вебхуков от Bitrix24

## Изменения в базе данных

### Новые таблицы

#### BitrixPortals
Хранит информацию о порталах Bitrix24:
- `Id` - первичный ключ
- `Name` - название портала
- `PortalUrl` - URL портала (например, https://company.bitrix24.ru)
- `WebhookUrl` - URL вебхука для исходящих запросов
- `IncomingSecret` - секретный ключ для проверки входящих вебхуков
- `IsActive` - флаг активного портала
- `LastSyncAt` - время последней синхронизации
- `Created`, `Updated` - временные метки

#### BitrixSettings
Хранит глобальные настройки синхронизации:
- `Id` - первичный ключ
- `Enabled` - включена ли синхронизация
- `WebhookUrl` - URL вебхука
- `EncryptedIncomingSecret` - зашифрованный секрет для входящих webhook
- `SyncIntervalMinutes` - интервал синхронизации в минутах
- `SyncTasks` - синхронизировать задачи
- `SyncDeals` - синхронизировать сделки
- `TwoWaySync` - двусторонняя синхронизация
- `LastSyncAt` - время последней синхронизации
- `CreatedAt`, `UpdatedAt` - временные метки

### Изменения в существующих таблицах

Добавлен столбец `PortalId` (int, nullable) в следующие таблицы:
- `Departments` - подразделения
- `Employees` - сотрудники
- `EmployeeTasks` - задачи

Это позволяет привязывать каждую сущность к конкретному порталу Bitrix24.

## Структура проекта

### Модели (Models/)

#### BitrixPortal.cs
```csharp
public class BitrixPortal : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PortalUrl { get; set; }
    public string WebhookUrl { get; set; }
    public string? IncomingSecret { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
```

#### Обновленные модели
- `Department.cs` - добавлено свойство `PortalId` и навигационное свойство `Portal`
- `Employee.cs` - добавлено свойство `PortalId` и навигационное свойство `Portal`
- `EmployeeTask.cs` - добавлено свойство `PortalId` и навигационное свойство `Portal`

### Репозитории (Data/Repositories/)

#### SyncEnabledRepositories.cs
Содержит реализации репозиториев с поддержкой синхронизации:

1. **ISyncEnabledRepository<T>** - интерфейс для репозиториев с поддержкой синхронизации
   - `IsSyncEnabledAsync()` - проверка включена ли синхронизация
   - `GetCurrentPortalAsync()` - получение активного портала

2. **SyncEnabledDepartmentsRepository** - репозиторий для подразделений
3. **SyncEnabledEmployeesRepository** - репозиторий для сотрудников
4. **SyncEnabledTasksRepository** - репозиторий для задач

Каждый репозиторий:
- Проверяет, включена ли синхронизация перед сохранением
- При создании/обновлении/удалении сущности выполняет синхронизацию с Bitrix24
- Сохраняет данные локально в любом случае (не ломает существующую функциональность)

### Контроллеры (Controllers/)

#### BitrixSettingsController.cs
Контроллер для управления настройками синхронизации:
- `Index()` - страница настроек
- `Update()` - сохранение общих настроек
- `AddPortal()` - добавление нового портала
- `EditPortal()` - редактирование портала
- `DeletePortal()` - удаление портала
- `SetActivePortal()` - установка активного портала

#### BitrixWebhookController.cs
API контроллер для обработки входящих вебхуков от Bitrix24:
- Маршрут: `POST /api/bitrix/webhook`
- Поддерживаемые события:
  - `onTaskAdd`, `onTaskUpdate`, `onTaskDelete` - задачи
  - `onDepartmentAdd`, `onDepartmentUpdate`, `onDepartmentDelete` - подразделения
  - `onUserAdd`, `onUserUpdate`, `onUserDelete` - пользователи
- Проверка подписи через `X-Bitrix-Signature` заголовок

### Views (Views/BitrixSettings/)

#### Index.cshtml
Страница управления настройками синхронизации:
- Форма общих настроек (включить/выключить, URL вебхука, интервал, типы сущностей)
- Таблица порталов с возможностью:
  - Сделать портал активным
  - Редактировать
  - Удалить
- Кнопка добавления нового портала

#### AddPortal.cshtml
Форма добавления нового портала Bitrix24

#### EditPortal.cshtml
Форма редактирования существующего портала

### ViewModel (ViewModels/)

#### BitrixSettingsViewModel.cs
Модель представления для страницы настроек:
```csharp
public class BitrixSettingsViewModel
{
    public BitrixSettings Settings { get; set; }
    public List<BitrixPortal> Portals { get; set; }
}
```

### Миграции (Migrations/ApplicationDb/)

#### 20260417000000_AddBitrixPortalAndSettings.cs
Миграция для создания новых таблиц и добавления столбцов PortalId

### Program.cs
Добавлена регистрация новых сервисов:
```csharp
builder.Services.AddScoped<ISyncEnabledRepository<Department>, SyncEnabledDepartmentsRepository>();
builder.Services.AddScoped<ISyncEnabledRepository<Employee>, SyncEnabledEmployeesRepository>();
builder.Services.AddScoped<ISyncEnabledRepository<EmployeeTask>, SyncEnabledTasksRepository>();
```

## Как использовать

### 1. Применение миграций
```bash
dotnet ef database update
```

### 2. Настройка синхронизации
1. Перейти на страницу `/BitrixSettings`
2. Включить синхронизацию (чекбокс "Включить синхронизацию")
3. Указать URL вебхука
4. Выбрать типы сущностей для синхронизации (задачи, сделки)
5. Добавить портал Bitrix24:
   - Название
   - URL портала
   - URL входящего вебхука
   - Секретный ключ (опционально)
6. Сделать портал активным

### 3. Настройка вебхуков в Bitrix24
В Bitrix24 необходимо настроить исходящие вебхуки на адрес:
```
https://your-domain.com/api/bitrix/webhook
```

Для событий:
- Задачи: `onTaskAdd`, `onTaskUpdate`, `onTaskDelete`
- Подразделения: `onDepartmentAdd`, `onDepartmentUpdate`, `onDepartmentDelete`
- Пользователи: `onUserAdd`, `onUserUpdate`, `onUserDelete`

### 4. Использование в коде

Для использования синхронизируемых репозиториев внедрять зависимости через DI:

```csharp
public class MyService
{
    private readonly ISyncEnabledRepository<Department> _departmentsRepo;
    
    public MyService(ISyncEnabledRepository<Department> departmentsRepo)
    {
        _departmentsRepo = departmentsRepo;
    }
    
    public async Task CreateDepartment(Department dept)
    {
        // Автоматически сохранит в БД и отправит в Bitrix24 если синхронизация включена
        await _departmentsRepo.CreateAsync(dept);
    }
}
```

## Архитектурные решения

### Не ломаем существующий код
- Старые репозитории (`IRepository<T>`) продолжают работать как прежде
- Новые репозитории (`ISyncEnabledRepository<T>`) добавляют функциональность синхронизации
- Можно постепенно переходить на новые репозитории

### Проверка включения синхронизации
Каждый метод сохранения проверяет:
1. Включена ли глобальная синхронизация (`BitrixSettings.Enabled`)
2. Включена ли синхронизация для данного типа сущности (например, `SyncTasks` для задач)
3. Существует ли активный портал

### TODO для доработки
В файле `SyncEnabledRepositories.cs` есть методы-заглушки:
- `SyncToBitrixAsync()` - требует реализации вызова API Bitrix24
- `DeleteFromBitrixAsync()` - требует реализации удаления из Bitrix24

Необходимо реализовать эти методы используя `BitrixTasksRepository` или создать аналогичные сервисы для других сущностей.

## Безопасность

### Проверка подписи вебхуков
Контроллер `BitrixWebhookController` поддерживает проверку подписи через HMAC-SHA256:
```csharp
private bool VerifyWebhookSignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedSignature = Convert.ToBase64String(hash);
    return computedSignature == signature;
}
```

## Дальнейшие улучшения

1. **Реализация методов синхронизации** - заполнить TODO в `SyncEnabledRepositories.cs`
2. **Маппинг полей** - добавить конфигурацию маппинга полей между локальной БД и Bitrix24
3. **Логирование синхронизации** - добавить таблицу логов синхронизации
4. **Планировщик задач** - реализовать фоновую задачу для периодической синхронизации
5. **Обработка конфликтов** - реализовать стратегию разрешения конфликтов при двусторонней синхронизации
6. **Стоимость задачи** - добавить синхронизацию поля `Price` для задач
