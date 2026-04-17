### принимая во внимание, что весь исходный код у тебя есть

## TOOLS:
- Docker Desktop
- dotnet (консольная комадна) https://dotnet.microsoft.com/en-us/download/dotnet/8.0

## Development
1. В сервисе `app` в `docker-composer.yaml` в разделе `environment`
поменять значение `ASPNETCORE_ENVIRONMENT=Production`
на `ASPNETCORE_ENVIRONMENT=Development` или закомментировать значение с `Production`
и добавить значение с `Development`

2. В том же разделе, что и в пункет сверху, закоментировать значения
с префиксом `ASPNETCORE_Kestrel__Certificates__`

3. В том же разделе поменять первым или вторым способом значение у переменной
`ASPNETCORE_URLS=https://+:443` на `ASPNETCORE_URLS=http://+:443`

#### В целом должно выглядеть так
```yaml
environment:
  # - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_ENVIRONMENT=Development
  # - ASPNETCORE_URLS=https://+:443
  - ASPNETCORE_URLS=http://+:443
  - POSTGRES_USER=${POSTGRES_USER}
  - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
  - POSTGRES_PORT=${POSTGRES_PORT}
  # - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/bg-a.ru.pem
  # - ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/https/bg-a.ru.key
  - TZ=Asia/Yekaterinburg
```

Таким образом избавляемся от необходимости в сертификатах на время разработки.

4. Чтобы запустить приложение вызываем команду в консоли в корневой папке приложения
```bash
docker compose up --build
```
Для запуска фонового процесса
```bash
docker compose up --build -d
```
Чтобы закрыть приложение
```bash
docker compose down
```
При этом данные из БД не будут удалены если в docker-compose.yaml подключены volumes, например
```yaml
  postgres_db:
    volumes:
      - postgres-data:/var/lib/postgresql/data
volumes:
  postgres-data:
```

PS: Если нужно подключится к базе данных через консоль из локальной среды используйте `psql`
```bash
psql postgresql://{user}:{password}@localhost:{port}/{dbname}
```
При этом, чтобы подключиться в приложении "app" к базе данных нужно указывать имя сервиса БД,
вместо localhost - postgres_db(если так называется сервис в docker-compose.yaml)

Так же можно разрабатывать в локальной среде с сертификатами сохраняя изначальные значения docker-compose:
- Скачать в корневую директорию в папку "https" ключи с сервера в папке "/home/.aspnet/https"
- Изменить на время разработки или закомментировать в разделе volumes изначальное значение и подставить новое
```yaml
volumes:
  # - /home/.aspnet/https:/https:ro
  - ./https:/https:ro
```
При этом Swagger не будет работать, потому что он работает только в локальной среде
## Production
Для продакшена всё просто - загружаем изменения, которые мы проделали по SFTP в папку `/home/motivation/`

**Конфигурационные данные должны быть в режиме `Production`**

Если в локальной среде в режиме `Production` всё работает, то всё должно быть хорошо, когда будешь загружать изменения на сервер

Чтобы изменения принялись исполни эту команду на сервере в папке приложения `/home/motivation`
```bash
docker compose up app --build
```
