# library-management-api

Навчальна система керування бібліотекою — Web API на ASP.NET Core із шарами `Api`, `Core`, `Infrastructure` та xUnit-тестами.

API підтримує каталог книг, пошук, доступність примірників, читачів, видачу, повернення, прострочення, історію читача й резервування. Дані зберігаються у SQLite; для навчального запуску схема створюється через `EnsureCreated`, тому зміни існуючої схеми потребують окремого переходу на міграції.

## Запуск і перевірка у Visual Studio (російський UI)

1. Відкрийте solution у Visual Studio та виберіть `Library.Api` як **Проект запуска**.
2. Натисніть **Запуск без отладки** або `Ctrl+F5`; відкриється Swagger UI. Розгорніть GET /api/books → Try it out → Execute. Додаткові приклади є у `Library.Api.http`.
3. Відкрийте **Тест > Обозреватель тестов** і запустіть усі тести або потрібний проєкт.
4. Для SQLite persistence використовується `EnsureCreated`; воно не оновлює стару схему автоматично.

## Структура та історії

- `src/Library.Core` — доменні моделі та сервіси.
- `src/Library.Infrastructure` — EF Core і SQLite.
- `src/Library.Api` — HTTP API, DTO та controllers.
- `tests/Library.UnitTests` — unit-тести xUnit.
- `tests/Library.IntegrationTests` — HTTP/SQLite integration-тести.
- SCRUM-6 — каталог, примірники та persistence.
- SCRUM-7 — пошук і доступність.
- SCRUM-8/9 — читачі, видача, повернення, прострочення, історія та резервування.

Документація API: `docs/api-catalog.md`. TDD-нотатки: `docs/tdd-author.md`, `docs/tdd-book-category.md`, `docs/tdd-book-copy.md`, `docs/tdd-catalog-service.md`. Ручні приклади запитів: `src/Library.Api/Library.Api.http`.
