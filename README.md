# Event & Booking API

Простой REST API для управления событиями и бронированием мест на них. Данные хранятся в PostgreSQL через EF Core (`AppDbContext`) — состояние переживает перезапуск приложения.

## Архитектура

Решение разбито на 4 отдельные сборки (проекта) по принципу чистой архитектуры — направление зависимостей идёт только внутрь, к `Domain`, и физически закреплено через `<ProjectReference>`, так что компилятор не даст его случайно нарушить:

```
EventApi.Presentation  →  EventApi.Application  →  EventApi.Domain
        ↓                         ↑
EventApi.Infrastructure ----------┘
```

| Проект | Назначение | Зависит от |
|---|---|---|
| `EventApi.Domain` | Доменные сущности (`Event`, `Booking`, `BookingStatus`, `User`, `UserRole`) и доменные исключения (`NotFoundException`, `NoAvailableSeatsException`, `EventHasBookingsException`, `EventAlreadyStartedException`, `BookingLimitExceededException`, `ForbiddenException`). Никаких ссылок на фреймворки — ни ASP.NET Core, ни EF Core. | — |
| `EventApi.Application` | Бизнес-логика: `EventService`/`BookingService`/`UserService` (use cases), `BookingBackgroundService`, DTO, порты — интерфейсы `IEventRepository`/`IBookingRepository`/`IUserRepository`/`IPasswordHasher`/`IJwtTokenService` (описывают, что нужно от хранилища и инфраструктуры безопасности, но не как это устроено). | `EventApi.Domain` |
| `EventApi.Infrastructure` | Реализация портов: `AppDbContext`, `EventRepository`/`BookingRepository`/`UserRepository`, EF-конфигурации (`IEntityTypeConfiguration`), миграции, `PasswordHasher` (SHA-256), `JwtTokenService` (генерация JWT). Здесь и только здесь есть зависимость на `Microsoft.EntityFrameworkCore`/`Npgsql`/`System.IdentityModel.Tokens.Jwt`. | `EventApi.Application`, `EventApi.Domain` |
| `EventApi.Presentation` | Контроллеры (включая `AuthController`), `GlobalExceptionHandlingMiddleware` (маппинг доменных исключений в HTTP-статусы), JWT-аутентификация/авторизация, `Program.cs` — composition root, регистрирующий зависимости через `AddApplicationServices()`/`AddInfrastructureServices()`. | `EventApi.Application`, `EventApi.Infrastructure` |

Ключевое правило: `EventApi.Application` **не** ссылается на `EventApi.Infrastructure` — бизнес-логика ничего не знает о конкретном способе хранения данных, только об абстракциях (`IEventRepository`/`IBookingRepository`).

## Требования

- .NET SDK 10.0
- PostgreSQL (локально или в контейнере)
- Docker — нужен для интеграционных тестов (Testcontainers поднимает PostgreSQL в контейнере автоматически) и, при желании, для локального запуска PostgreSQL

## Настройка строки подключения

Строка подключения задаётся в `EventApi.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

Поправь `Host`/`Port`/`Username`/`Password` под свой инстанс PostgreSQL (или удобнее — через `appsettings.Development.json` / переменные окружения, чтобы не коммитить реальные креды).

## Настройка JWT

Параметры JWT-токена задаются в `EventApi.Presentation/appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "замени-на-длинную-случайную-строку-минимум-32-символа",
    "Issuer": "EventApi",
    "Audience": "EventApiClient",
    "ExpiryMinutes": 60
  }
}
```

- `Secret` — ключ, которым подписывается и проверяется токен (алгоритм HMAC-SHA256). **Важно**: значение в репозитории — только для локальной разработки и учебных целей. В продакшне секрет должен быть настоящим случайным значением (например, `openssl rand -base64 32`), храниться вне репозитория (переменная окружения, секрет-менеджер типа Azure Key Vault / AWS Secrets Manager) и никогда не попадать в git.
- `Issuer`/`Audience` — должны совпадать между генерацией токена (`JwtTokenService`) и его проверкой (`Program.cs`, `TokenValidationParameters`) — иначе валидный токен будет отклонён.
- `ExpiryMinutes` — время жизни токена в минутах.

## Запуск

```bash
git clone git@github.com:UnityStand/Practice.git
cd Practice
dotnet build Practice.sln
dotnet run --project EventApi.Presentation
```

Схема базы данных (таблицы `Events`, `Bookings`, внешний ключ между ними) управляется миграциями EF Core, а не создаётся вручную. При старте `Program.cs` вызывает `db.Database.Migrate()` — он применяет все ещё не применённые миграции из папки `Migrations/` и сам создаёт базу, если её не существует. Повторные запуски ничего не ломают: уже применённые миграции просто пропускаются (EF Core отслеживает это в служебной таблице `__EFMigrationsHistory`).

## Миграции EF Core

`AppDbContext` и миграции лежат в `EventApi.Infrastructure/Migrations/`, а не в стартовом проекте — это нормально для чистой архитектуры (Presentation ничего не знает про EF Core напрямую), но означает, что `--project` и `--startup-project` для `dotnet ef` теперь указывают на **разные** проекты. Для работы с миграциями нужен установленный `dotnet-ef`:

```bash
dotnet tool install --global dotnet-ef
```

Применять миграции руками не нужно — это делает `Program.cs` при каждом старте приложения (`Database.Migrate()`). Команды ниже нужны только для **разработки** — когда меняется модель (`Event`/`Booking`/конфигурации `IEntityTypeConfiguration`) и нужно сгенерировать новую миграцию:

```bash
# создать новую миграцию после изменения модели
dotnet ef migrations add <ИмяМиграции> --project EventApi.Infrastructure --startup-project EventApi.Presentation

# применить миграции к базе вручную, без запуска приложения (например, для отладки)
dotnet ef database update --project EventApi.Infrastructure --startup-project EventApi.Presentation
```

`--project` указывает, куда положить файлы миграции и где искать `AppDbContext` (`EventApi.Infrastructure`); `--startup-project` — какой проект запускать, чтобы прочитать конфигурацию (строку подключения) и собрать DI-контейнер (`EventApi.Presentation`, там `Program.cs` и `appsettings.json`). Без явного указания `dotnet ef` не сможет однозначно выбрать между несколькими проектами решения.

## Swagger

Интерактивная документация и тестирование API доступны по адресу:

```
http://localhost:5047/swagger
```

## Аутентификация и роли

API защищено JWT-аутентификацией (`Microsoft.AspNetCore.Authentication.JwtBearer`). У пользователя (`User`) есть логин, хеш пароля (SHA-256, реализация — `PasswordHasher` в `EventApi.Infrastructure`) и роль:

| Роль | Значение в enum `UserRole` | Права |
|---|---|---|
| Обычный пользователь | `Customer` | бронирование событий, просмотр и отмена **своих** броней |
| Администратор | `Admin` | всё то же плюс создание/редактирование/удаление событий, отмена **любой** чужой брони |

### Эндпоинты аутентификации

Базовый путь: `/auth`. Оба эндпоинта доступны без токена.

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| POST | `/auth/register` | регистрация нового пользователя | 204 | 400 (логин уже занят / ошибка валидации) |
| POST | `/auth/login` | вход, возвращает JWT-токен | 200 | 400 (неверный логин или пароль) |

Тело `POST /auth/register`:
```json
{
  "login": "user1",
  "password": "Pass123!",
  "role": "Customer"
}
```
`role` необязательное поле, по умолчанию `Customer`; для удобства тестирования допустимо передать `"Admin"`, чтобы сразу создать администратора.

Тело `POST /auth/login`:
```json
{
  "login": "user1",
  "password": "Pass123!"
}
```
Ответ:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

При неверном логине и при неверном пароле возвращается **одно и то же** сообщение об ошибке (`"Invalid login or password"`) — это защита от перебора: по ответу нельзя понять, существует ли такой логин в системе.

### Как получить и использовать токен в Swagger

1. Откройте `http://localhost:<port>/swagger`.
2. Выполните `POST /auth/register`, затем `POST /auth/login` (через "Try it out") — в ответе будет поле `token`.
3. Скопируйте значение `token` (без слова `Bearer`, только сам токен).
4. Нажмите кнопку **Authorize** вверху страницы Swagger, вставьте токен в поле и подтвердите — Swagger сам добавит заголовок `Authorization: Bearer <токен>` ко всем последующим запросам из UI.
5. Теперь запросы к защищённым эндпоинтам будут проходить аутентификацию. Роль в токене определяет, какие из них доступны (см. таблицы ниже — `[Authorize(Roles = "Admin")]` на создании/изменении/удалении событий).

### Защищённые эндпоинты

- `POST /events`, `PUT /events/{id}`, `DELETE /events/{id}` — только роль `Admin` (401 без токена, 403 для `Customer`).
- `POST /events/{id}/book`, `GET /bookings/{id}`, `DELETE /bookings/{id}` — любой аутентифицированный пользователь (401 без токена); `userId` берётся из claim токена (`ClaimTypes.NameIdentifier`), а не из тела запроса.
- `GET /events`, `GET /events/{id}` — открыты без токена.

## Модель Event

| Поле | Тип | Обязательное | Описание |
|---|---|---|---|
| `Id` | Guid | генерируется сервером | уникальный идентификатор |
| `Title` | string | да (минимум 1 символ) | заголовок события |
| `Description` | string? | нет | описание события |
| `StartAt` | DateTime | да | дата и время начала |
| `EndAt` | DateTime | да | дата и время окончания, должна быть строго позже `StartAt` |
| `TotalSeats` | int | да, при создании (> 0) | общее количество мест на событии |
| `AvailableSeats` | int | генерируется сервером | текущее количество свободных мест; при создании равно `TotalSeats` |

## Эндпоинты Event

Базовый путь: `/events`

| Метод | Путь | Описание | Успех | Ошибки | Доступ |
|---|---|---|---|---|---|
| GET | `/events` | список событий (с фильтрацией и пагинацией) | 200 | — | любой |
| GET | `/events/{id}` | событие по id | 200 | 404 | любой |
| POST | `/events` | создать событие | 201 | 400, 401, 403 | только `Admin` |
| PUT | `/events/{id}` | обновить событие целиком | 200 | 400, 404, 401, 403 | только `Admin` |
| DELETE | `/events/{id}` | удалить событие | 204 | 404, 401, 403 | только `Admin` |

## Фильтрация и пагинация (`GET /events`)

Все параметры необязательные и передаются через query string. Фильтры работают совместно (логическое И). Результат всегда отсортирован по `StartAt` по возрастанию — порядок детерминирован и не зависит от внутреннего порядка хранения в сторе.

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `title` | string | — | поиск по названию, регистронезависимый, частичное совпадение |
| `from` | DateTime | — | вернуть события, которые начинаются не раньше указанной даты (`StartAt >= from`) |
| `to` | DateTime | — | вернуть события, которые заканчиваются не позже указанной даты (`EndAt <= to`) |
| `page` | int | `1` | номер страницы |
| `pageSize` | int | `10` | количество элементов на странице |

Ответ — объект `PaginatedResult`:

```json
{
  "totalCount": 23,
  "items": [ /* события на текущей странице */ ],
  "page": 2,
  "pageSize": 10
}
```

### Примеры запросов

```
GET /events?title=встреча
GET /events?from=2026-01-01&to=2026-12-31
GET /events?title=стендап&page=2&pageSize=5
```

### Пример тела запроса (POST / PUT)

```json
{
  "title": "Стендап",
  "description": "Ежедневная синхронизация команды",
  "startAt": "2026-07-06T10:00:00",
  "endAt": "2026-07-06T10:30:00",
  "totalSeats": 10
}
```

## Валидация

- `Title`, `StartAt`, `EndAt`, `TotalSeats` обязательны, `Title` не может быть пустой строкой.
- `EndAt` должен быть строго позже `StartAt` (равенство тоже считается ошибкой) — проверяется дважды: на входе, в `CreateEventDto`/`EventRequestDto` (`IValidatableObject`, дешёвый `400` без похода в БД), и в самой доменной модели (`Event.Create(...)`/`Event.UpdateInfo(...)`), которая гарантирует инвариант независимо от того, откуда её вызвали.
- `TotalSeats` должен быть больше нуля — эту проверку выполняет сама доменная модель, `Event.Create(...)`, и бросает `ValidationException`, если условие нарушено. Это гарантирует инвариант, даже если `Event` создаётся в обход HTTP-запроса (например, из тестов).
- Ответы API (`EventResponseDto`) отделены от доменной модели `Event` — контроллер никогда не сериализует сущность напрямую. Это не просто стиль: у `Event` есть навигационное свойство `Bookings`, а у `Booking` — обратная ссылка `Event`, и сериализация сущности напрямую привела бы к циклической ссылке в JSON.

## Модель Booking

| Поле | Тип | Описание |
|---|---|---|
| `Id` | Guid | генерируется сервером |
| `EventId` | Guid | id события, к которому относится бронь |
| `UserId` | Guid | id пользователя-владельца брони (берётся из JWT при создании, а не из тела запроса) |
| `Status` | `Pending` \| `Confirmed` \| `Rejected` \| `Cancelled` | текущий статус брони, сериализуется как строка |
| `CreatedAt` | DateTime | момент создания брони |
| `ProcessedAt` | DateTime? | момент, когда бронь была подтверждена/отклонена/отменена; `null`, пока бронь `Pending` |

## Эндпоинты Booking

Все эндпоинты требуют аутентификации (401 без токена).

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| POST | `/events/{id}/book` | создать бронь на событие | 202 Accepted | 400 (событие уже началось), 404 (событие не найдено), 409 (нет свободных мест или превышен лимит активных броней) |
| GET | `/bookings/{id}` | получить текущее состояние брони | 200 | 404 |
| DELETE | `/bookings/{id}` | отменить бронь | 204 | 403 (нет прав — не владелец и не `Admin`), 404 |

Бронь создаётся сразу в статусе `Pending` и подтверждается фоновым сервисом асинхронно — статус нужно перепроверять через `GET /bookings/{id}`.

### Бизнес-правила бронирования

- **Событие уже началось.** Нельзя забронировать событие, если `Event.StartAt` уже наступил — `EventAlreadyStartedException` → `400 Bad Request`.
- **Лимит активных броней на пользователя.** Настраивается в `appsettings.json` (секция `BookingSettings:MaxActiveBookingsPerUser`, по умолчанию `10`) и учитывает брони в статусах `Pending`/`Confirmed`. При превышении — `BookingLimitExceededException` → `409 Conflict`, сообщение содержит само значение лимита. Лимиты разных пользователей друг на друга не влияют.
- **Отмена брони.** Пользователь может отменить только свою бронь; администратор (`Admin`) — любую. Нарушение — `ForbiddenException` → `403 Forbidden`. Повторная отмена уже отменённой брони запрещена доменной моделью (`400 Bad Request`).

### Пример ответа

```json
{
  "bookingId": "8bf479b5-17f2-4b8c-a4e2-aa5af9c964cc",
  "eventId": "7b112ddf-4745-4876-b106-fda919893a59",
  "status": "Pending",
  "createdAt": "2026-08-07T00:57:40.96",
  "processedAt": null
}
```

(`userId` в ответе не возвращается намеренно — клиент и так знает, кто он, по своему собственному токену)

## База данных и EF Core

Данные хранятся в PostgreSQL через `AppDbContext` (`EventApi.Infrastructure/Persistence/AppDbContext.cs`). Маппинг сущностей на таблицы описан через Fluent API в `EventApi.Infrastructure/Persistence/Configurations/EventConfiguration.cs`/`BookingConfiguration.cs`/`UserConfiguration.cs`:

- Таблица `Users`: `Login` с уникальным индексом (`HasIndex(...).IsUnique()`) — на уровне БД гарантирует то же самое, что `UserService.RegisterAsync` уже проверяет на уровне приложения. `Role` хранится как строка (`HasConversion<string>()`), как и `Booking.Status`.
- `Bookings.UserId` — внешний ключ на `Users.Id` (`OnDelete(DeleteBehavior.Restrict)`), настроен в `BookingConfiguration.cs`.
- `Id` у сущностей — `ValueGeneratedNever()`: идентификатор генерируется в коде (в `Event.Create(...)`/`Booking.Create(...)`/`User.Create(...)`), а не базой данных.
- `Booking.Status` (enum) хранится в БД как строка (`HasConversion<string>()`), а не как число — это защищает существующие данные от порчи, если порядок значений `BookingStatus` когда-нибудь изменится.
- Связь `Event` → `Booking` (один-ко-многим) настроена с `OnDelete(DeleteBehavior.Restrict)`: удалить событие с активными бронями нельзя — `EventService.DeleteEvent` сначала проверяет наличие броней через репозиторий и бросает `EventHasBookingsException` (`409 Conflict`), не давая базе самой отказать менее понятной ошибкой нарушения внешнего ключа.

`Event`/`Booking` — приватные конструкторы без параметров (нужны EF Core для создания объектов через рефлексию при чтении из БД) плюс публичные статические фабрики `Create(...)` с валидацией инвариантов. Внешний код не может создать эти сущности через `new` напрямую.

### Репозитории

Прямая работа с `AppDbContext` вынесена из сервисов в отдельный слой репозиториев. Интерфейсы `IEventRepository`/`IBookingRepository` (порты) описаны в `EventApi.Application/Abstractions/`, а их реализации (`EventRepository.cs`, `BookingRepository.cs`) — в `EventApi.Infrastructure/Persistence/`. `EventService`/`BookingService` больше не знают про `DbSet`/LINQ-запросы к базе — они работают только с интерфейсами репозиториев и доменными объектами, а вся персистентность (запросы, `SaveChangesAsync`) инкапсулирована в реализациях слоя Infrastructure. Оба репозитория зарегистрированы в DI как `Scoped` (в `EventApi.Infrastructure`, extension-метод `AddInfrastructureServices`); `BookingBackgroundService` (как `Hosted Service`-singleton, живёт в `EventApi.Application`) получает их не через конструктор напрямую, а через `IServiceScopeFactory.CreateScope()` на каждую единицу работы — иначе возник бы захват Scoped-зависимости синглтоном (captive dependency).

## Синхронизация и защита от гонок

В проекте два разных примитива синхронизации — под два разных сценария. Оба — `static SemaphoreSlim`, а не `lock`: `EventService`/`BookingService` зарегистрированы как `Scoped` (так как `AppDbContext` — `Scoped`), а значит каждый HTTP-запрос получает свой экземпляр сервиса — обычное (не `static`) поле-семафор в этих условиях защищало бы только само себя, а не запросы друг от друга.

**`static SemaphoreSlim(1, 1)` в `BookingService.CreateBookingAsync`.** Без защиты возможен overbooking: два параллельных запроса одновременно проверяют `AvailableSeats > 0`, оба видят "места есть" и оба создают бронь — в сумме броней окажется больше, чем мест. Семафор с ёмкостью 1 — взаимоисключающая секция (аналог `lock`, но с `await` внутри, что для обычного `lock` запрещено компилятором): `WaitAsync()`/`Release()` в `try/finally` вокруг «проверка мест (`TryReserveSeats`) + создание брони + `SaveChangesAsync()`».

**`SemaphoreSlim(Environment.ProcessorCount, ...)` в `BookingBackgroundService.ProcessBookingAsync`.** Здесь семафор — не про корректность (у каждой обрабатываемой брони свой собственный `AppDbContext`, полученный через `IServiceScopeFactory.CreateScope()` — сущности разных задач никак не пересекаются), а чисто про троттлинг: ограничивает, сколько броней обрабатывается параллельно, вместо того чтобы отправить в БД сразу все запросы одним махом.

**Один `AppDbContext` — одна единица работы.** `BookingService.CreateBookingAsync` вызывает `SaveChangesAsync()` один раз, хотя меняет два объекта (новая `Booking` и уменьшенный `AvailableSeats` у `Event`) — оба отслеживаются одним и тем же контекстом в рамках запроса, так что EF Core сохраняет оба изменения одной транзакцией.

**Компенсация при сбое обработки.** Если `BookingBackgroundService.ProcessBookingAsync` падает с ошибкой после захвата места (например, БД временно недоступна на середине операции), `CompensateAsync` открывает **новый** scope/`AppDbContext` (не переиспользует потенциально повреждённый после сбоя) и явно отклоняет бронь и возвращает место — иначе место осталось бы зарезервированным навсегда.

## Формат ошибок

Ошибки возвращаются в формате JSON, но конкретная форма зависит от источника:

**Автоматическая валидация модели** (не прошли аннотации `[Required]`/`[MinLength]` в DTO) — стандартный ответ ASP.NET Core `ValidationProblemDetails`, `400 Bad Request`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["The Title field is required."]
  }
}
```

**Остальные ошибки** (бизнес-валидация, `404 Not Found`, `409 Conflict`, `500 Internal Server Error`) — обрабатываются глобальным `GlobalExceptionHandlingMiddleware` и возвращаются в едином упрощённом формате:

```json
{
  "status": 409,
  "detail": "No available seats for this event"
}
```

Для `500` в `detail` попадает нейтральное сообщение ("An unexpected error occurred"), а не текст реального исключения — чтобы не раскрывать детали реализации. Все необработанные исключения дополнительно логируются через `ILogger`.

## Пример сценария с овербукингом

Все запросы ниже, кроме `GET`, требуют заголовок `Authorization: Bearer <токен>` (см. раздел «Аутентификация и роли»).

```
POST /events   { "title": "Test Event", "totalSeats": 3, ... }   (токен Admin)
  → 201, тело содержит Id события

POST /events/{id}/book   (x3, токен любого пользователя)
  → 202 Accepted, "status": "Pending" в каждом ответе

POST /events/{id}/book   (4-й раз)
  → 409 Conflict, { "status": 409, "detail": "No available seats for this event" }

# подождать несколько секунд, пока BookingBackgroundService обработает брони

GET /bookings/{bookingId}
  → 200, "status": "Confirmed", "processedAt" заполнено
```

## Тесты

Юнит- и интеграционные тесты — два разных проекта, `tests/ASP.NET Core Web API.Tests` и `tests/Integration.Tests`, — запускаются одной командой `dotnet test` из корня решения (или `dotnet test Practice.sln`).

### Юнит-тесты

Находятся в `tests/ASP.NET Core Web API.Tests` и используют **InMemory-провайдер EF Core** (`Microsoft.EntityFrameworkCore.InMemory`), а не реальную PostgreSQL — `AppDbContext` регистрируется через `ServiceCollection`/`AddDbContext` с `UseInMemoryDatabase(dbName)`, сервисы резолвятся из DI как `IEventService`/`IBookingService`, ровно как в реальном приложении.

Каждый тестовый класс получает свою собственную, уникальную InMemory-базу (новый `Guid` на конструктор класса — xUnit создаёт новый экземпляр класса на каждый `[Fact]`, так что тесты гарантированно не влияют друг на друга). Важный нюанс: имя базы обязательно выносится в переменную **до** лямбды `AddDbContext(...)` — если вызвать `Guid.NewGuid()` прямо внутри неё, каждый `CreateScope()` получит свою отдельную базу, и данные между scope-ами перестанут быть общими.

- `EventServiceTest.cs` — CRUD-сценарии `EventService`: создание (включая валидацию `TotalSeats`/дат), получение по id, фильтрация, пагинация, обновление, удаление (включая `EventHasBookingsException`, когда у события есть активные брони).
- `BookingServiceTest.cs` — сценарии `BookingService`: успешное и неуспешное создание брони, уменьшение `AvailableSeats`, исчерпание мест (`NoAvailableSeatsException`), восстановление места после `Reject()`/`ReleaseSeats()`, переходы статуса брони (`Confirm`/`Reject`/`Cancel`), а также тесты на конкурентность — защита от овербукинга и уникальность Id брони при параллельных запросах. Для параллельных тестов каждая задача открывает свой `_serviceProvider.CreateScope()` (свой `AppDbContext`), а не переиспользует общий сервис — иначе тест проверял бы не реальную гонку, а последовательный доступ к одному объекту.
  - Отдельно покрыты новые бизнес-правила: бронирование уже начавшегося события (`EventAlreadyStartedException`), превышение лимита активных броней одного пользователя (`BookingLimitExceededException`, включая проверку, что место при этом не резервируется), независимость лимитов между разными пользователями, и отмена брони — успешная для владельца/администратора и `ForbiddenException` для постороннего пользователя.

Тестовый проект ссылается напрямую на `EventApi.Application` и `EventApi.Infrastructure` (а не на `EventApi.Presentation`) — юнит-тестам не нужен веб-слой, только бизнес-логика и (для `AppDbContext`/InMemory-провайдера) слой доступа к данным. `EventService`/`BookingService` — публичные классы в `EventApi.Application.Services`, доступ к ним не требует никаких `InternalsVisibleTo`.

### Интеграционные тесты

Находятся в `tests/Integration.Tests` и проверяют `EventRepository`/`BookingRepository` на **реальном PostgreSQL**, поднятом через [Testcontainers](https://dotnet.testcontainers.org/) (`Testcontainers.PostgreSql`) — **для их запуска обязательно должен быть запущен Docker**, контейнер поднимается и удаляется автоматически, вручную ничего готовить не нужно.

- `Fixtures/PostgresContainerFixture.cs` — стартует один контейнер PostgreSQL на весь прогон тестов (`IAsyncLifetime`), `Fixtures/DatabaseCollection.cs` (`[CollectionDefinition]` + `ICollectionFixture`) раздаёт этот единственный экземпляр всем тестовым классам, помеченным `[Collection("Database")]` — так контейнер не пересоздаётся на каждый класс.
- `RepositoryTestBase.cs` — общий базовый класс: перед **каждым** тестом (`InitializeAsync`, xUnit создаёт новый экземпляр класса на каждый `[Fact]`) пересоздаёт схему через `EnsureDeletedAsync()` + `MigrateAsync()`. Это одновременно даёт тестам чистое состояние базы и попутно проверяет, что миграции реально способны поднять схему с нуля.
- `EventRepositoryTests.cs` — все методы `IEventRepository`, включая варианты фильтров `GetPagedAsync` (по названию, по датам `from`/`to`) и пагинацию (проверка общего количества и отсутствия пересечения элементов между страницами).
- `BookingRepositoryTests.cs` — все методы `IBookingRepository`, включая `ExistsForEventAsync` (для проверки перед удалением события) и `GetPendingIdsAsync` (выборка только `Pending`-броней для фонового сервиса).

`Integration.Tests` ссылается напрямую на `EventApi.Infrastructure` (там лежат `EventRepository`/`BookingRepository`/`AppDbContext` — все они публичные, отдельных `InternalsVisibleTo` не требуется).

### Запуск всех тестов

```bash
dotnet test
```

Юнит-тесты выполняются за доли секунды (InMemory), интеграционные — на пару секунд дольше первого запуска (Docker тянет образ `postgres:16-alpine`, если его ещё нет локально), при повторных запусках образ уже закэширован.
