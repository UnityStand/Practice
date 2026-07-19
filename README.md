# Event & Booking API

Простой REST API для управления событиями и бронированием на них. Данные хранятся в памяти приложения (`List<Event>`, `List<Booking>`) — при перезапуске сбрасываются.

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

## Эндпоинты Event

Базовый путь: `/api/Event`

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| GET | `/api/Event` | список всех событий | 200 | — |
| GET | `/api/Event/{id}` | событие по id | 200 | 404 |
| POST | `/api/Event` | создать событие | 201 | 400 |
| PUT | `/api/Event/{id}` | обновить событие целиком | 200 | 400, 404 |
| DELETE | `/api/Event/{id}` | удалить событие | 200 | 404 |

### Пример тела запроса (POST / PUT)

```json
{
  "title": "Стендап",
  "description": "Ежедневная синхронизация команды",
  "startAt": "2026-07-06T10:00:00",
  "endAt": "2026-07-06T10:30:00"
}
```

## Валидация

- `Title`, `StartAt`, `EndAt` обязательны, `Title` не может быть пустой строкой.
- `EndAt` должен быть строго позже `StartAt` (равенство тоже считается ошибкой).
- Проверка выполняется на уровне DTO (`EventDTO`/`BookingRequestDto`), до вызова контроллера — через `[Required]` и `IValidatableObject`.

## Формат ошибок

Ошибки возвращаются в формате JSON:

**Автоматическая валидация модели** (не прошли аннотации `[Required]` в DTO) — стандартный ответ ASP.NET Core `ValidationProblemDetails`, `400 Bad Request`:

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

**Остальные ошибки** (например, `404 Not Found`, когда ресурс не найден) — контроллеры формируют `ProblemDetails` вручную через `Problem(statusCode:, title:)`, единый формат обеспечивается регистрацией `AddProblemDetails()`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Event not Found",
  "status": 404
}
```

## Модель Booking

| Поле | Тип | Обязательное | Описание |
|---|---|---|---|
| `Id` | Guid | генерируется сервером | уникальный идентификатор брони |
| `EventId` | Guid | да | идентификатор события, к которому относится бронь |
| `Status` | BookingStatus | да | текущий статус брони |
| `CreatedAt` | DateTime | генерируется сервером | дата и время создания брони |
| `ProcessedAt` | DateTime? | нет | дата и время обработки брони (заполняется фоновым сервисом) |

### Статусы (`BookingStatus`)

| Значение | Описание |
|---|---|
| `Pending` | бронь создана, ожидает обработки |
| `Confirmed` | бронь подтверждена |
| `Rejected` | бронь отклонена |

## Эндпоинты Booking

| Метод | Путь | Описание | Успех | Ошибки |
|---|---|---|---|---|
| POST | `/events/{id}/book` | создать бронь для события | 202 Accepted + заголовок `Location: /bookings/{bookingId}` | 404, если события нет |
| GET | `/bookings/{id}` | получить текущее состояние брони | 200 | 404, если брони нет |

### Пример ответа (`POST /events/{id}/book`, `GET /bookings/{id}`)

```json
{
  "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "status": "Pending",
  "createdAt": "2026-07-19T10:00:00Z",
  "processedAt": null
}
```

`status` сериализуется как строка (`Pending`/`Confirmed`/`Rejected`), а не как число.

## Фоновая обработка бронирований

`BookingProcessingService` (`BackgroundService`, зарегистрирован через `AddHostedService`) в фоне, всё время работы приложения:

1. Опрашивает хранилище на наличие броней в статусе `Pending`.
2. Для каждой такой брони делает искусственную задержку (имитация обращения к внешней системе).
3. После задержки переводит бронь в статус `Confirmed` и заполняет `ProcessedAt` текущим временем.
4. Между проходами опроса выдерживает паузу, чтобы не нагружать CPU впустую, когда обрабатывать нечего.
5. Корректно останавливается при остановке приложения (`CancellationToken` передаётся во все ожидания).

### Пример сценария использования

```
POST /api/Event
  → 201, тело содержит созданное событие с его Id

POST /events/{id}/book
  → 202 Accepted, Location: /bookings/{bookingId}
  → тело: { "status": "Pending", ... }

GET /bookings/{bookingId}     (сразу после создания)
  → 200, "status": "Pending"

# подождать несколько секунд, пока фоновый сервис обработает бронь

GET /bookings/{bookingId}     (через несколько секунд)
  → 200, "status": "Confirmed", "processedAt" заполнено
```

## Тесты

Юнит-тесты (xUnit) находятся в `tests/ASP.NET Core Web API.Tests`:

- `EventServiceTest.cs` — CRUD-сценарии `EventService`, успешные и неуспешные (создание, получение по id, обновление, удаление, поведение при отсутствующем событии).
- `BookingServiceTest.cs` — сценарии `BookingService`: создание брони для существующего/несуществующего/удалённого события, уникальность Id при нескольких бронях, получение брони по Id, отражение смены статуса после обработки.
- Дополнительно — тесты на `InMemoryBooking` (хранилище): `GetBookingsPending` не возвращает уже обработанные брони, `UpdateBooking` сохраняет изменения.

Запуск:

```bash
dotnet test
```
