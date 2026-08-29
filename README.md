# Event & Booking API

Простой REST API для управления событиями и бронированием мест на них. Данные хранятся в PostgreSQL через EF Core (`AppDbContext`) — состояние переживает перезапуск приложения.

## Требования

- .NET SDK 10.0
- PostgreSQL (локально или в контейнере)

## Настройка строки подключения

Строка подключения задаётся в `ASP.NET Core Web API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

Поправь `Host`/`Port`/`Username`/`Password` под свой инстанс PostgreSQL (или удобнее — через `appsettings.Development.json` / переменные окружения, чтобы не коммитить реальные креды).

## Запуск

```bash
git clone git@github.com:UnityStand/Practice.git
cd Practice
dotnet build "ASP.NET Core Web API"
dotnet run --project "ASP.NET Core Web API"
```

Схема базы данных (таблицы `events`, `bookings`) создаётся автоматически при первом запуске через `Database.EnsureCreated()` в `Program.cs` — отдельно накатывать миграции не нужно. `EnsureCreated` ничего не делает при повторных запусках, если схема уже существует, но **не совместим с EF Core миграциями** — если в будущем понадобятся миграции, БД нужно будет пересоздать или заменить `EnsureCreated()` на `Migrate()`.

## Swagger

Интерактивная документация и тестирование API доступны по адресу:

```
http://localhost:5047/swagger
```

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

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| GET | `/events` | список событий (с фильтрацией и пагинацией) | 200 | — |
| GET | `/events/{id}` | событие по id | 200 | 404 |
| POST | `/events` | создать событие | 201 | 400 |
| PUT | `/events/{id}` | обновить событие целиком | 200 | 400, 404 |
| DELETE | `/events/{id}` | удалить событие | 204 | 404 |

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
| `Status` | `Pending` \| `Confirmed` \| `Rejected` | текущий статус брони, сериализуется как строка |
| `CreatedAt` | DateTime | момент создания брони |
| `ProcessedAt` | DateTime? | момент, когда фоновый сервис подтвердил или отклонил бронь; `null`, пока бронь `Pending` |

## Эндпоинты Booking

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| POST | `/events/{id}/book` | создать бронь на событие | 202 Accepted | 404 (событие не найдено), 409 (нет свободных мест) |
| GET | `/bookings/{id}` | получить текущее состояние брони | 200 | 404 |

Бронь создаётся сразу в статусе `Pending` и подтверждается фоновым сервисом асинхронно — статус нужно перепроверять через `GET /bookings/{id}`.

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

## База данных и EF Core

Данные хранятся в PostgreSQL через `AppDbContext` (`DataAccess/AppDbContext.cs`). Маппинг сущностей на таблицы описан через Fluent API в `DataAccess/Configurations/EventConfiguration.cs`/`BookingConfiguration.cs`:

- `Id` у обеих сущностей — `ValueGeneratedNever()`: идентификатор генерируется в коде (в `Event.Create(...)`/`Booking.Create(...)`), а не базой данных.
- `Booking.Status` (enum) хранится в БД как строка (`HasConversion<string>()`), а не как число — это защищает существующие данные от порчи, если порядок значений `BookingStatus` когда-нибудь изменится.
- Связь `Event` → `Booking` (один-ко-многим) настроена с `OnDelete(DeleteBehavior.Restrict)`: удалить событие с активными бронями нельзя — `EventService.DeleteEvent` сначала проверяет `context.Bookings.AnyAsync(...)` и бросает `EventHasBookingsException` (`409 Conflict`), не давая базе самой отказать менее понятной ошибкой нарушения внешнего ключа.

`Event`/`Booking` — приватные конструкторы без параметров (нужны EF Core для создания объектов через рефлексию при чтении из БД) плюс публичные статические фабрики `Create(...)` с валидацией инвариантов. Внешний код не может создать эти сущности через `new` напрямую.

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

```
POST /events   { "title": "Test Event", "totalSeats": 3, ... }
  → 201, тело содержит Id события

POST /events/{id}/book   (x3)
  → 202 Accepted, "status": "Pending" в каждом ответе

POST /events/{id}/book   (4-й раз)
  → 409 Conflict, { "status": 409, "detail": "No available seats for this event" }

# подождать несколько секунд, пока BookingBackgroundService обработает брони

GET /bookings/{bookingId}
  → 200, "status": "Confirmed", "processedAt" заполнено
```

## Тесты

Юнит-тесты (xUnit) находятся в `tests/ASP.NET Core Web API.Tests` и используют **InMemory-провайдер EF Core** (`Microsoft.EntityFrameworkCore.InMemory`), а не реальную PostgreSQL — `AppDbContext` регистрируется через `ServiceCollection`/`AddDbContext` с `UseInMemoryDatabase(dbName)`, сервисы резолвятся из DI как `IEventService`/`IBookingService`, ровно как в реальном приложении.

Каждый тестовый класс получает свою собственную, уникальную InMemory-базу (новый `Guid` на конструктор класса — xUnit создаёт новый экземпляр класса на каждый `[Fact]`, так что тесты гарантированно не влияют друг на друга). Важный нюанс: имя базы обязательно выносится в переменную **до** лямбды `AddDbContext(...)` — если вызвать `Guid.NewGuid()` прямо внутри неё, каждый `CreateScope()` получит свою отдельную базу, и данные между scope-ами перестанут быть общими.

- `EventServiceTest.cs` — CRUD-сценарии `EventService`: создание (включая валидацию `TotalSeats`/дат), получение по id, фильтрация, пагинация, обновление, удаление (включая `EventHasBookingsException`, когда у события есть активные брони).
- `BookingServiceTest.cs` — сценарии `BookingService`: успешное и неуспешное создание брони, уменьшение `AvailableSeats`, исчерпание мест (`NoAvailableSeatsException`), восстановление места после `Reject()`/`ReleaseSeats()`, переходы статуса брони (`Confirm`/`Reject`), а также тесты на конкурентность — защита от овербукинга и уникальность Id брони при параллельных запросах. Для параллельных тестов каждая задача открывает свой `_serviceProvider.CreateScope()` (свой `AppDbContext`), а не переиспользует общий сервис — иначе тест проверял бы не реальную гонку, а последовательный доступ к одному объекту.

`EventService`/`BookingService` — классы `internal` (реализации скрыты, наружу торчат только `IEventService`/`IBookingService`); тестовая сборка получает доступ к ним через `InternalsVisibleTo` в `ASP.NET Core Web API.csproj`.

Запуск:

```bash
dotnet test
```
