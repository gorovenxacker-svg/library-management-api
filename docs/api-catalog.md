# HTTP API каталогу SCRUM-6

## Ручний запуск у Visual Studio

1. Запустіть `Library.Api` як startup project.
2. API створює SQLite-файл `library.db` за connection string `ConnectionStrings:Library`.
3. Використовуйте `src/Library.Api/Library.Api.http` для HTTP-запитів. OpenAPI JSON доступний за `/openapi/v1.json` у Development; Swagger UI не встановлено.
4. Integration tests запускаються окремо через Test Explorer і використовують ізольовану SQLite in-memory базу. Поточний блок SCRUM-8/9 додав тести циркуляції; їх запуск після внесених змін ще не підтверджувався.

## Endpoints

### Authors

- `POST /api/authors` — створити автора: `{ "name": "Леся Українка" }`.
- `GET /api/authors` — отримати колекцію авторів.
- `GET /api/authors/{id}` — отримати автора або `404`.

### Categories

- `POST /api/categories` — створити категорію: `{ "name": "Драма" }`.
- `GET /api/categories` — отримати колекцію категорій.
- `GET /api/categories/{id}` — отримати категорію або `404`.

### Books

- `POST /api/books` — створити книгу:

```json
{
  "title": "Лісова пісня",
  "categoryId": "category-id",
  "authorIds": ["author-id"]
}
```

- `GET /api/books` — отримати колекцію книг без циклічних DTO.
- `GET /api/books/{id}` — отримати книгу або `404`.
- `GET /api/books?title=...&author=...&category=...&availableOnly=true` — пошук книг. Усі задані фільтри поєднуються через AND; `author` збігається з будь-яким автором книги.

Пошук обрізає значення фільтрів, ігнорує порожні/пробільні параметри та використовує case-insensitive substring-порівняння `Ordinal`, зокрема для українських літер. `availableOnly=true` повертає книгу, якщо вона має хоча б один примірник зі статусом `Available`; кожна книга повертається один раз.

### Book copies

- `POST /api/books/{bookId}/copies` — створити примірник: `{ "inventoryNumber": "INV-001" }`.
- `GET /api/copies` — отримати колекцію примірників.
- `GET /api/copies/{id}` — отримати примірник або `404`.

Успішне створення повертає `201 Created` із `Location` через `CreatedAtAction`. Некоректний або порожній ввід повертає `400 ProblemDetails`, невідомий ресурс — `404`, повторний інвентарний номер — `409`.

## Persistence і обмеження

`Library.Infrastructure` використовує `LibraryDbContext` та `EfCatalogRepository` через `ICatalogRepository`. Connection string за замовчуванням — `Data Source=library.db`; її можна змінити в конфігурації.

Для навчального етапу API викликає `EnsureCreated()` під час запуску. Це не замінює міграції: схема не версіонується, а зміни моделі в існуючій базі не оновлюють стару схему автоматично. Базу даних не видаляємо; міграції будуть окремим наступним кроком.

Для `Book`–`Author` використано many-to-many, для книги–категорії та книги–примірника — зв’язки з зовнішніми ключами. На `InventoryNumber` створено унікальний SQLite-індекс із точним case-sensitive порівнянням. `BookCopy.Version` змінюється на кожному переході статусу й використовується як optimistic concurrency token, щоб виявляти ABA-конфлікти. Сервісна перевірка доповнюється обмеженням БД для конкурентних конфліктів.

### Readers, loans і reservations

- `POST /api/readers` — створити читача: `{ "name": "Читач" }`.
- `GET /api/readers/{id}` — отримати читача або `404`.
- `POST /api/loans` — видати примірник: `{ "readerId": "...", "copyId": "...", "dueAtUtc": "2026-11-20T12:00:00Z" }`.
- `GET /api/loans/{id}` — отримати видачу.
- `POST /api/loans/{id}/return` — повернути примірник.
- `GET /api/loans/overdue` — отримати активні видачі з `DueAtUtc < now`.
- `GET /api/readers/{id}/history` — історія видач читача.
- `POST /api/reservations` — зарезервувати доступний примірник: `{ "readerId": "...", "copyId": "..." }`.
- `GET /api/reservations/{id}` — отримати резервування.
- `POST /api/reservations/{id}/cancel` — скасувати резервування: `{ "readerId": "..." }`.

Видача дозволена для `Available` або для `Reserved` власнику активного резервування; `dueAtUtc` має бути ISO-датою з UTC (`Z`) і строго в майбутньому. Повернення робить примірник доступним і зберігає історію. Резервування конкретного примірника не має черги чи автоматичного строку; скасувати його може лише власник, а видача власнику закриває резервування як `Fulfilled`. Конфлікти домену та optimistic concurrency повертаються як `409`, а не-UTC або минулий термін — як `400`.

`readerId` у запитах видачі та скасування резервування є навчальним параметром власника операції. Автентифікації та авторизації в API наразі немає.

Пошук реалізований для малого навчального каталогу шляхом отримання списків через repository та фільтрації в пам’яті. Для великого каталогу це не оптимальний підхід; подальший крок — перенесення фільтрів у запит до БД.
