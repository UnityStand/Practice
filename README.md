# Event & Booking — микросервисы на .NET и Kafka

Система управления событиями и бронированием мест. Бывший монолит разделён на **три независимых сервиса**. У каждого своя база данных, а синхронизация идёт через событие `BookingConfirmed` в Apache Kafka. Прямых HTTP-вызовов между сервисами нет.

## Состав системы

| Сервис | Отвечает за | Порт | База данных | Kafka |
|---|---|---|---|---|
| **Users** | регистрация, вход, хеширование пароля, **выдача JWT** | `5001` | `user_db` | — |
| **Events** | CRUD событий, учёт свободных мест | `5002` | `events_db` | **подписчик** `booking-confirm` |
| **Bookings** | создание, подтверждение и отмена броней | `5003` | `booking_db` | **издатель** `booking-confirm` |

Инфраструктура: PostgreSQL 16 (порт `5432`, один сервер, три отдельные базы) и Apache Kafka 3.9 в режиме KRaft (порт `9092` для хоста, `kafka:29092` внутри Docker).

```
                 JWT (общие Secret / Issuer / Audience)
        ┌────────────────────────┬────────────────────────┐
        ▼                        ▼                        ▼
 ┌─────────────┐         ┌──────────────┐          ┌──────────────┐
 │    Users    │         │    Events    │          │   Bookings   │
 │  :5001      │         │  :5002       │          │  :5003       │
 └──────┬──────┘         └──────┬───────┘          └──────┬───────┘
        │                       │  ▲                      │
     user_db                events_db│                 booking_db
                                     │                      │
                                     │   ┌──────────────┐   │ publish
                                     └───┤    Kafka     │◄──┘ BookingConfirmed
                              subscribe  │booking-confirm│
                                         └──────────────┘
```

### Структура решения

```
src/
  Shared/EventApi.Contracts/      общий контракт: Topics, BookingConfirmed (без логики и зависимостей)
  Users/    Users.Domain | Users.Application | Users.Infrastructure | Users.Api
  Events/   Events.Domain | Events.Application | Events.Infrastructure | Events.Api
  Bookings/ Bookings.Domain | Bookings.Application | Bookings.Infrastructure | Bookings.Api
tests/
  Users.Tests | Events.Tests | Bookings.Tests   юнит-тесты (EF InMemory, без Kafka)
  Integration.Tests                             репозитории и миграции на реальном PostgreSQL (Testcontainers)
docker-compose.yml                              PostgreSQL + Kafka + три сервиса
```

Каждый сервис построен по чистой архитектуре. Зависимости направлены внутрь, к `Domain`, и закреплены через `<ProjectReference>`:

```
*.Api  →  *.Application  →  *.Domain
  ↓             ↑
*.Infrastructure┘
```

| Слой | Что лежит | Пример (Bookings) |
|---|---|---|
| Domain | сущности и доменные исключения, без фреймворков | `Booking`, `BookingStatus`, `BookingLimitExceededException` |
| Application | use cases и порты-интерфейсы | `BookingService`, `BookingBackgroundService`, `IBookingRepository`, `IBookingEventPublisher` |
| Infrastructure | реализация портов: EF Core, Kafka, JWT | `BookingDbContext`, `BookingRepository`, `KafkaBookingEventPublisher`, миграции |
| Api | контроллеры, middleware ошибок, JWT, Swagger, `Program.cs` | `BookingController`, `GlobalExceptionHandlingMiddleware` |

**Граница между сервисами:** проекты разных сервисов **не ссылаются** друг на друга. Общий только `EventApi.Contracts`. Навигационных свойств и внешних ключей между данными разных сервисов нет: `Booking.EventId` и `Booking.UserId` — обычные `Guid`.

## Поток данных `BookingConfirmed`

Контракт (`src/Shared/EventApi.Contracts`):

```csharp
public static class Topics { public const string BookingConfirmed = "booking-confirm"; }

public sealed record BookingConfirmed(Guid BookingId, Guid EventId, Guid UserId, DateTime Confirmed, int BookedSeats);
```

```
Клиент ── POST /events/{id}/book ──► Bookings: бронь Pending (202 Accepted)
                                         │
             BookingBackgroundService (раз в секунду)
                                         │ 1. booking.Confirm() + SaveChanges → booking_db
                                         │ 2. PublishAsync(BookingConfirmed), key = EventId
                                         ▼
                            Kafka topic "booking-confirm" (3 раздела)
                                         │
             BookingConfirmedConsumer (Events, группа events-service)
                                         │ новый DI-scope на каждое сообщение
                                         ▼
             BookingConfirmedHandler: TryReserveSeats + ProcessedBookings → один SaveChanges → events_db
```

**Кто публикует.** Сервис Bookings: фоновый `BookingBackgroundService` подтверждает брони в статусе `Pending`. Порядок строгий: **сначала** статус `Confirmed` сохраняется в `booking_db`, **потом** сообщение отправляется в Kafka через `IBookingEventPublisher`, реализация которого — `KafkaBookingEventPublisher`. Bookings не уменьшает места и не обращается к Events.
- Продюсер Kafka — singleton (тяжёлый потокобезопасный объект). При остановке приложения он делает `Flush` и `Dispose` (`IDisposable`).
- Ключ сообщения — `EventId`: все брони одного события попадают в один раздел и обрабатываются строго по порядку.
- `Acks = All`: брокер подтверждает запись только после надёжного сохранения.

**Кто подписан.** Сервис Events: `BookingConfirmedConsumer` (`BackgroundService` в слое Infrastructure, группа `events-service`).
- Блокирующий `Consume()` выполняется в отдельном потоке (`Task.Run`) и не мешает старту HTTP API.
- `BackgroundService` — singleton, а репозитории и `DbContext` — scoped, поэтому на каждое сообщение создаётся отдельный DI-scope.
- `EnableAutoCommit = false`: offset коммитится **после** обработки, это доставка «хотя бы один раз».
- `AutoOffsetReset = Earliest`: сообщения, отправленные до первого запуска Events, не теряются.
- При штатной остановке вызывается `consumer.Close()`, и группа сразу перераспределяет разделы.

**Что происходит при получении** (`BookingConfirmedHandler`, слой Application):

| Ситуация | Действие |
|---|---|
| `BookingId` уже есть в `ProcessedBookings` (повтор сообщения) | лог + пропуск, места **не** списываются повторно (**идемпотентность**) |
| событие не найдено (удалено или неверный id) | `LogWarning` + пропуск |
| мест не хватает | `LogWarning` + пропуск |
| иначе | `TryReserveSeats(BookedSeats)` и запись в `ProcessedBookings` **одним** `SaveChanges` (одна транзакция) |
| битый JSON | `LogWarning` + пропуск, консьюмер продолжает работу |
| временная ошибка (например, БД недоступна) | `LogError`, offset **не** коммитится, `Seek` на то же сообщение, повтор через 5 с |

Первичный ключ `ProcessedBookings.BookingId` — последняя линия защиты: даже если два одинаковых сообщения обрабатываются одновременно, второй `SaveChanges` упадёт на уникальности и откатит и отметку, и списание мест.

**Создание топика.** При старте Events `KafkaTopicInitializer` (`IHostedService`, регистрируется **до** консьюмера) создаёт топик `booking-confirm` (3 раздела) через `AdminClient`. Если топик уже есть, это нормально. Если Kafka недоступна, пишется `LogWarning`, и сервис всё равно стартует (таймаут 10 с). Автосоздание топиков в брокере выключено (`KAFKA_AUTO_CREATE_TOPICS_ENABLE=false`), поэтому система работает «из коробки» на пустом брокере именно благодаря этому компоненту.

## Запуск

### Вариант 1. Вся система в Docker (рекомендуется)

Нужен только Docker Desktop.

```bash
git clone git@github.com:UnityStand/Practice.git
cd Practice
docker compose up --build -d
docker compose ps            # 6 контейнеров: postgres, kafka, redis (healthy), users, events, bookings
```

| Swagger | URL |
|---|---|
| Users | http://localhost:5001/swagger |
| Events | http://localhost:5002/swagger |
| Bookings | http://localhost:5003/swagger |

Логи: `docker compose logs -f events`. Остановка: `docker compose down`. Данные PostgreSQL сохраняются в томе `eventapi_pgdata`; `docker compose down -v` удалит и их.

Каждый сервис собирается своим **многоступенчатым** Dockerfile (`src/<Service>/<Service>.Api/Dockerfile`): стадия `sdk:10.0` делает restore и publish, а в финальный образ `aspnet:10.0` попадают только готовые сборки. Контекст сборки — корень репозитория, потому что Events и Bookings собираются вместе с `EventApi.Contracts`. Внутри контейнера сервисы слушают порт `8080` и работают от непривилегированного пользователя.

### Вариант 2. Сервисы локально, инфраструктура в Docker

Нужен .NET SDK 10.0.

```bash
docker compose up -d postgres kafka redis
dotnet build Practice.sln
dotnet run --project src/Users/Users.Api        # http://localhost:5001
dotnet run --project src/Events/Events.Api      # http://localhost:5002
dotnet run --project src/Bookings/Bookings.Api  # http://localhost:5003
```

Миграции применяются автоматически при старте каждого сервиса (`Database.Migrate()` создаёт базу, если её нет).

## Конфигурация

Настройки лежат в `src/<Service>/<Service>.Api/appsettings.json`. В Docker они переопределяются переменными окружения в `docker-compose.yml` (двойное подчёркивание обозначает вложенность секции):

| Ключ | Users | Events | Bookings | Локально | В Docker |
|---|:-:|:-:|:-:|---|---|
| `ConnectionStrings:DefaultConnection` | ✓ | ✓ | ✓ | `Host=localhost;...` | `Host=postgres;...` |
| `Jwt:Secret` / `Issuer` / `Audience` | ✓ | ✓ | ✓ | одинаковые во всех трёх | общий блок `x-jwt` |
| `Jwt:ExpiryMinutes` | ✓ | | | `60` | — |
| `Kafka:BootstrapServers` | | ✓ | ✓ | `localhost:9092` | `kafka:29092` |
| `Kafka:ConsumerGroup` | | ✓ | | `events-service` | `events-service` |
| `BookingSettings:MaxActiveBookingsPerUser` | | | ✓ | `10` | — |
| `Redis:ConnectionString` | | ✓ | | `localhost:6379` | `redis:6379` |
| `Cache:EventTtl` / `Cache:TopEventsTtl` | | ✓ | | `00:05:00` / `00:01:00` | — |

`Jwt:Secret` в репозитории — учебное значение. В продакшене секрет хранится вне git (переменные окружения или секрет-менеджер).

## Аутентификация и роли

Токен выдаёт **только** Users. Events и Bookings проверяют его сами (`AddJwtBearer`) по **общим** `Secret`, `Issuer` и `Audience`, поэтому токен, выданный Users, принимается в других сервисах без обращения к Users.

| Роль | Права |
|---|---|
| `Customer` | бронирование, просмотр и отмена **своих** броней |
| `Admin` | плюс создание, изменение и удаление событий, отмена **любой** брони |

| Сервис | Метод | Путь | Доступ | Успех | Ошибки |
|---|---|---|---|---|---|
| Users | POST | `/auth/register` | все | 204 | 400 (логин занят) |
| Users | POST | `/auth/login` | все | 200 `{ token }` | 400 (неверный логин или пароль) |
| Events | GET | `/events`, `/events/{id}` | все | 200 | 404 |
| Events | POST | `/events` | `Admin` | 201 | 400, **401**, **403** |
| Events | PUT | `/events/{id}` | `Admin` | 200 | 400, 401, 403, 404 |
| Events | DELETE | `/events/{id}` | `Admin` | 204 | 401, 403, 404, **409** (есть учтённые брони) |
| Bookings | POST | `/events/{eventId}/book` | авторизованный | 202 `Pending` | **401**, 409 (лимит броней) |
| Bookings | GET | `/bookings/{id}` | авторизованный | 200 | 401, 404 |
| Bookings | DELETE | `/bookings/{id}` | владелец или `Admin` | 204 | 401, 403, 404 |

Идентификатор пользователя берётся из claim токена (`ClaimTypes.NameIdentifier`), а не из тела запроса. Для удобства проверки при регистрации можно передать `"role": "Admin"`.

**Swagger и JWT:** в Swagger каждого сервиса есть кнопка **Authorize**. Получите токен в Swagger Users (`/auth/login`), вставьте его без префикса `Bearer` в Authorize сервиса Events или Bookings, и Swagger сам добавит заголовок `Authorization: Bearer <token>`.

## Сквозная проверка

1. **Users** (`:5001`): `POST /auth/register` с `{"login":"admin","password":"123","role":"Admin"}`, затем `POST /auth/login`, скопировать `token`.
2. **Events** (`:5002`): Authorize, затем `POST /events` с `totalSeats: 5`, запомнить `eventId`. `GET /events/{eventId}` покажет `availableSeats: 5`.
3. **Bookings** (`:5003`): Authorize, затем `POST /events/{eventId}/book` → `202`, статус `Pending`. Через 1–2 секунды `GET /bookings/{bookingId}` покажет `Confirmed`.
4. **Events:** `GET /events/{eventId}` → `availableSeats: 4`. Место списано **только** через обработку события из Kafka: Bookings не вызывает Events.

Сообщения в топике можно посмотреть напрямую:

```bash
docker exec kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 \
  --topic booking-confirm --from-beginning --property print.key=true --timeout-ms 5000
```

## Фильтрация и пагинация (`GET /events`)

| Параметр | По умолчанию | Описание |
|---|---|---|
| `title` | — | поиск по названию, регистронезависимый, частичное совпадение |
| `from` | — | события с `StartAt >= from` |
| `to` | — | события с `EndAt <= to` |
| `page` / `pageSize` | `1` / `10` | пагинация; результат отсортирован по `StartAt` |

Пример тела `POST /events`:

```json
{
  "title": "Стендап",
  "description": "Ежедневная синхронизация команды",
  "startAt": "2027-07-06T10:00:00Z",
  "endAt": "2027-07-06T10:30:00Z",
  "totalSeats": 10
}
```

## Формат ошибок

Ошибки валидации DTO возвращаются стандартным `ValidationProblemDetails` (400). Остальные ошибки обрабатывает `GlobalExceptionHandlingMiddleware` каждого сервиса:

```json
{ "status": 409, "detail": "Booking limit exceeded: maximum 10 active bookings allowed" }
```

| Исключение | Код |
|---|---|
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `ForbiddenException` | 403 |
| `BookingLimitExceededException`, `EventHasBookingsException` | 409 |
| прочее | 500 с нейтральным текстом, детали только в логе |

## Согласованность в конечном счёте: известные ограничения

Разделение данных между сервисами означает, что проверки, которые в монолите выполнялись в одной транзакции, теперь асинхронны:

- **Бронь не проверяет событие.** Bookings не знает о событиях и не может без HTTP-вызова проверить, что событие существует, ещё не началось и в нём есть места. Бронь станет `Confirmed`, а Events при нехватке мест или отсутствии события **пропустит** сообщение с логом. Статус брони в этом случае не откатывается: для этого нужен обратный поток событий (например, `SeatsReservationFailed` → `Booking.Reject()`).
- **Отмена не возвращает места.** `DELETE /bookings/{id}` меняет только статус в Bookings: отмена в Kafka не публикуется (`BookingCancelled` вне рамок задания).
- **Проверка удаления события** опирается на локальную проекцию `ProcessedBookings` в `events_db`. Бронь, которая ещё не дошла до Events, не помешает удалить событие.
- **Потеря сообщения при долгой недоступности Kafka.** Статус сохраняется до публикации. Если брокер недоступен дольше `message.timeout.ms` (5 минут по умолчанию), бронь останется `Confirmed` без сообщения. Надёжное решение — паттерн **Transactional Outbox**: сообщение пишется в таблицу `booking_db` в той же транзакции, а отдельный процесс пересылает его в Kafka. Благодаря абстракции `IBookingEventPublisher` и идемпотентному подписчику Outbox можно добавить без изменений в Events.
- В Docker сервисы запускаются с `ASPNETCORE_ENVIRONMENT=Development`, чтобы был доступен Swagger. Для продакшена это нужно убрать.

## Кеширование (Redis)

Events кеширует два самых частых запроса на чтение. Redis спрятан за интерфейсом `ICacheService` (`Events.Application/Abstractions`), реализация `RedisCacheService` лежит в `Events.Infrastructure/Caching`. Application не зависит от StackExchange.Redis. `IConnectionMultiplexer` регистрируется в DI как singleton: соединение тяжёлое и потокобезопасное, поэтому создаётся один раз на всё приложение.

### Что кешируется

| Ключ | Данные | TTL | Обновление |
|---|---|---|---|
| `event:{id}` | `EventResponseDto` для `GET /events/{id}` | 5 минут (`Cache:EventTtl`) | инвалидация при записи + TTL |
| `events:top10` | 10 событий с наибольшей долей проданных мест, `GET /events/top` | 1 минута (`Cache:TopEventsTtl`) | только TTL |

Все ключи собраны в `CacheKeys`, а TTL задаются в секции `Cache` файла `appsettings.json` (класс `CacheOptions`). В кеш кладётся DTO, а не доменная сущность `Event`: у сущности приватный конструктор и сеттеры, и JSON-сериализатор не смог бы её восстановить.

### Паттерн Cache-Aside

1. Сервис ищет значение в кеше по ключу.
2. **Попадание**: значение сразу возвращается, база данных не вызывается.
3. **Промах**: данные читаются из базы, кладутся в кеш с TTL и возвращаются клиенту.

Кеш заполняется только на чтении и только в одном месте для каждого ключа (`EventService.GetEventById` и `EventService.GetTopEvents`).

### Почему такие TTL

- **`event:{id}` — 5 минут.** Карточку события запрашивают часто, а меняется она редко. Устаревание здесь заметно (название, даты, свободные места), поэтому ключ явно инвалидируется при каждом изменении. TTL служит страховкой на случай изменений в обход сервиса (например, правка базы вручную) и не даёт кешу хранить редко запрашиваемые события вечно.
- **`events:top10` — 1 минута.** Это рейтинговый агрегат: пересчитывать его после каждого бронирования было бы избыточно, а небольшое отставание рейтинга некритично. Короткий TTL ограничивает устаревание минутой и при этом снимает с базы тяжёлый запрос с сортировкой по всей таблице.

### Изменение данных

Для `event:{id}` выбрана **инвалидация при записи**: после изменения ключ удаляется, и следующий GET прогревает кеш из базы.

| Источник изменения | Где инвалидируется |
|---|---|
| `PUT /events/{id}` | `EventService.UpdateEvent` |
| `DELETE /events/{id}` | `EventService.DeleteEvent` |
| сообщение Kafka `BookingConfirmed` (меняет `AvailableSeats`) | `BookingConfirmedHandler` |

**Порядок операций: сначала база, потом кеш.** Если процесс оборвётся между шагами, база уже будет в актуальном состоянии, а устаревший ключ исчезнет не позже чем через TTL. При обратном порядке параллельный GET мог бы успеть прочитать из базы старые данные и снова положить их в кеш на весь TTL.

**Почему инвалидация, а не обновление при записи.** Удалить ключ проще и надёжнее: объект для кеша собирается только в одном месте, и нет риска записать в кеш данные в другом формате. Обработчику Kafka не нужно знать, как выглядит DTO. Повторная доставка того же сообщения безопасна: обработчик идемпотентен (`ProcessedBookings`), а повторное удаление ключа ничего не ломает.

Кеш `events:top10` явно не инвалидируется и обновляется по TTL. Поэтому после бронирования или изменения события рейтинг может отставать до одной минуты.

### Если Redis недоступен

Кеш деградирует без ошибки для клиента:

- соединение создаётся с `AbortOnConnectFail = false`, поэтому сервис стартует и без Redis, а подключение восстанавливается в фоне;
- `RedisCacheService` перехватывает `RedisConnectionException` и `RedisTimeoutException` и пишет предупреждение в лог;
- `GetAsync` при сбое возвращает `null`, то есть работает как промах: запрос уходит в базу. `SetAsync` и `DeleteAsync` просто пропускаются.

Если Redis был недоступен в момент изменения события и ключ не удалился, устаревшее значение проживёт не дольше `EventTtl`.

## Миграции EF Core

У каждого сервиса свой `DbContext` и свои миграции в `src/<Service>/<Service>.Infrastructure/Persistence/Migrations`:

| Сервис | DbContext | Миграции | Таблицы |
|---|---|---|---|
| Users | `UsersDbContext` | `InitialUsers` | `Users` |
| Events | `EventDbContext` | `InitialEvents`, `AddProcessedBookings` | `Events`, `ProcessedBookings` |
| Bookings | `BookingDbContext` | `InitialBookings` | `Bookings` (без FK на другие сервисы) |

```bash
# новая миграция (пример для Events)
dotnet ef migrations add <Name> -p src/Events/Events.Infrastructure -s src/Events/Events.Api
```

## Тесты

```bash
dotnet test Practice.sln
```

| Проект | Что проверяет |
|---|---|
| `Users.Tests` | регистрация и вход, хеширование пароля; токен Users проходит проверку с параметрами Events и Bookings и отклоняется при другом секрете |
| `Events.Tests` | CRUD и фильтры `EventService`, запрет удаления при учтённых бронях; `BookingConfirmedHandler`: списание мест, **идемпотентность** повторного сообщения, пропуск при отсутствии события или мест; **кеширование** (`EventServiceCachingTests`, заглушки `FakeCacheService` и `StubEventRepository`): попадание не вызывает репозиторий, промах читает репозиторий и кладёт результат в кеш с нужным TTL, `PUT`/`DELETE` и `BookingConfirmed` инвалидируют `event:{id}`, а `events:top10` не трогают |
| `Bookings.Tests` | лимит активных броней (в том числе при конкурентных запросах), права на отмену; `BookingBackgroundService` публикует `BookingConfirmed` **только после** сохранения статуса в БД (фейковый издатель читает статус из базы в момент публикации) |
| `Integration.Tests` | репозитории всех трёх сервисов на реальном PostgreSQL (Testcontainers): миграции каждого сервиса создают **только свои** таблицы, `ProcessedBookings` + места сохраняются одной транзакцией, дубль `BookingId` отклоняется первичным ключом |

Для `Integration.Tests` нужен запущенный Docker: Testcontainers сам поднимает временный контейнер PostgreSQL.
