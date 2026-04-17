# Документация структуры приложения

**Допустим корневая папка приложения начинается с `/`**
## Файлы конфигурации
В корневой папке приложения находятся
несколько файлов развёртывания и конфигурации приложения,
а именно
- `Dockerfile` (Создаёт image приложения(не БД))
- `docker-compose.yml` (конфигурация запуска контейнеров)
- .env (переменные окружения при запуске `docker compose` будут
подставлены вместо одноимённых переменных в `docker-compose.yml`)
- `Motivation.sln` (это автоматически изменяется, поэтому не трогать)
- `Motivation/appsettings.Development.json` (Загружается, когда указана переменная
окружения `ASPNETCORE_ENVIRONMENT=Development` в разделе `environment` в сервисе `app`
в `docker-compose.yml`)
- `Motivation/appsettings.json` (Загружается, когда указана переменная
окружения `ASPNETCORE_ENVIRONMENT=Production` в разделе `environment` в сервисе `app`
в `docker-compose.yml`)
- `Motivation/Properties/launchSettings.json` (Нужно для запуска приложения,
лучше не трогать без причины)
- `Motivation/Motivation.csproj` (Это тоже лучше не трогать без причины,
т.к. оно само заполняется, аналогично `package.json`)
- `Motivation/Options/AuthOptions` (Настройки JWT)
- `Motivation/Options/PostgresOptions` (Настройки БД)

## Ресурсы, статичные файлы
В `Motivation/wwwroot/` содержатся статичный файлы (css, js, картинки, библиотеки и т.д),
подключаются в `Motivation/Program.cs` через `app.UseStaticFiles()`)

## Приложение
- В `Motivation/Views/` содержатся страницы приложения. Похоже на Laravel,
но со своими фичами.
- `Motivation/ViewModels/` используются в методах
котроллеров `Motivation/Controllers/` и импортируются в страницах `Motivation/Views/`.
В контроллерах аггрегируются данные, затем эти данные привязываются к ViewModels
и передаются на страницу. Например,
```cs
[HttpGet]
public async Task<IActionResult> Add()
{
    var departments = await _departmentsRepository.Entries.ToListAsync();
    var employees = await _employeesRepository.Entries.ToListAsync();
    var addDepartmentViewModel = new AddDepartmentViewModel
    {
        Departments = departments,
        Employees = employees,
    };
    return View(addDepartmentViewModel);
}
```
```cshtml
@model AddDepartmentViewModel

@{
    ViewData["Title"] = "Добавление подразделения";
}
```
- В `Motivation/Models/` лежат модели данных сущностей.
- `Motivation/Migrations/` содержит миграции для базы данных.
- `Motivation/Helper/` содержит вспомогательные фичи.
- `Motivation/Data/` содержит логику для БД.
- `Motivation/Data/Repositories/` содержит логику взаимодействия с БД.
- `Motivation/Core/` содержит бизнес-логику приложения.
_ `Motivation/Controllers/` содержит API приложения.

## О ASP .Net
Программа начинается с файла `Program.cs`, в котором:
1. Создаётся приложение с помощью паттерна `Builder`,
подключаются сервисы идентификации (отвечает за авторизацию),
2. Добавляются репозитории таким образом
```cs
builder.Services.AddScoped<IRepository<Penalty>, PenaltiesRepository>();
```
`IRepository` интерфейс с типом модели `Model` и репозиторий этой модели.

Жизненный цикл бизнес-модели выглядит так:
1. Создать модель
2. Добавить её в контекст БД
3. Создать репозиторий
4. Создать бизнес логику (опционально)
5. Создать контроллер
6. Создать ViewModel
7. Создать View
8. Подключить репозиторий в `Program.cs`
