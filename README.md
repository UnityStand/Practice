# Event & Booking API

Простой REST API для управления событиями и бронированием мест на них. Данные хранятся в памяти приложения (`InMemoryEventStore`, `InMemoryBookingStore`) — при перезапуске сбрасываются.

## Требования

- .NET SDK 10.0

## Запуск

```bash
git clone git@github.com:UnityStand/Practice.git
cd Practice
dotnet build "ASP.NET Core Web API"
dotnet run --project "ASP.NET Core Web API"
```

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

Все параметры необязательные и передаются через query string. Фильтры работают совместно (логическое И).

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
- `EndAt` должен быть строго позже `StartAt` (равенство тоже считается ошибкой) — проверяется на входе, в `CreateEventDto`/`EventInfoDto` (`IValidatableObject`), до вызова контроллера.
- `TotalSeats` должен быть больше нуля — эту проверку выполняет сама доменная модель, `Event.Create(...)`, и бросает `ValidationException`, если условие нарушено. Это гарантирует инвариант, даже если `Event` создаётся в обход HTTP-запроса (например, из тестов).

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

## Синхронизация и защита от гонок

В проекте два разных примитива синхронизации — под два разных сценария.

**`lock` в `BookingService.CreateBookingAsync`.** Без защиты возможен overbooking: два параллельных запроса одновременно проверяют `AvailableSeats > 0`, оба видят "места есть" и оба создают бронь — в сумме броней окажется больше, чем мест. `lock` оборачивает атомарную пару «проверка мест (`TryReserveSeats`) + создание брони», так что в любой момент времени через неё проходит только один поток. Важно: `BookingService` зарегистрирован как `AddSingleton` — при `AddScoped` каждый запрос получал бы свой экземпляр сервиса со своим собственным `lock`-объектом, и блокировка не защищала бы вообще ничего.

**`SemaphoreSlim` в `BookingBackgroundService.ProcessBookingAsync`.** Фоновый сервис обрабатывает все `Pending`-брони параллельно через `Task.WhenAll`, а `lock` нельзя использовать вокруг `await` — компилятор не разрешит. `SemaphoreSlim(1, 1)` — асинхронный аналог мьютекса: `WaitAsync()`/`Release()` вместо `lock`, безопасен внутри `async`-кода. Задержка, имитирующая внешний вызов (`Task.Delay`), выполняется *до* захвата семафора — так все задержки идут параллельно, а под защитой семафора остаётся только сама запись в хранилище.

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

Юнит-тесты (xUnit) находятся в `tests/ASP.NET Core Web API.Tests`:

- `EventServiceTest.cs` — CRUD-сценарии `EventService`: создание (включая валидацию `TotalSeats`), получение по id, фильтрация, пагинация, обновление, удаление.
- `BookingServiceTest.cs` — сценарии `BookingService`: успешное и неуспешное создание брони, уменьшение `AvailableSeats`, исчерпание мест (`NoAvailableSeatsException`), восстановление места после `Reject()`/`ReleaseSeats()`, переходы статуса брони (`Confirm`/`Reject`), а также тесты на конкурентность — защита от овербукинга и уникальность Id брони при параллельных запросах (через `Task.Run` + `Task.WhenAll`, чтобы гонка была настоящей, а не последовательной).

Запуск:

```bash
dotnet test
```
