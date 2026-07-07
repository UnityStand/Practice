# Event API

Простой REST API для управления событиями. Данные хранятся в памяти приложения (`List<Event>`) — при перезапуске сбрасываются.

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
| `Id` | int | генерируется сервером | уникальный идентификатор |
| `Title` | string | да | заголовок события |
| `Description` | string? | нет | описание события |
| `StartAt` | DateTime | да | дата и время начала |
| `EndAt` | DateTime | да | дата и время окончания, должна быть строго позже `StartAt` |

## Эндпоинты

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

- `Title`, `StartAt`, `EndAt` обязательны.
- `EndAt` должен быть строго позже `StartAt` (равенство тоже считается ошибкой).
- При нарушении возвращается `400 Bad Request` с деталями в формате `ProblemDetails`.

## Формат ошибок

Все ошибки (400, 404, 500) возвращаются в формате `application/problem+json` (`ProblemDetails`).

