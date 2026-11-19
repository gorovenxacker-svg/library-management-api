# HTTP API каталогу SCRUM-6

## Ручний запуск у Visual Studio

1. Запустіть `Library.Api` як startup project.
2. API створює SQLite-файл `library.db` за connection string `ConnectionStrings:Library`.
3. Використовуйте `src/Library.Api/Library.Api.http` для HTTP-запитів. OpenAPI JSON доступний за `/openapi/v1.json` у Development; Swagger UI не встановлено.
4. Integration tests запускаються окремо через Test Explorer і використовують ізольовану SQLite in-memory базу. 19.11.2026 після повної перебудови рішення всі 47 тестів пройшли: 40 модульних і 7 у проєкті IntegrationTests. Два з них — початкові шаблонні тести (по одному в кожному проєкті); шість нових інтеграційних тестів перевіряють HTTP API і SQLite.

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

### Book copies

- `POST /api/books/{bookId}/copies` — створити примірник: `{ "inventoryNumber": "INV-001" }`.
- `GET /api/copies` — отримати колекцію примірників.
- `GET /api/copies/{id}` — отримати примірник або `404`.

Успішне створення повертає `201 Created` із `Location` через `CreatedAtAction`. Некоректний або порожній ввід повертає `400 ProblemDetails`, невідомий ресурс — `404`, повторний інвентарний номер — `409`.

## Persistence і обмеження

`Library.Infrastructure` використовує `LibraryDbContext` та `EfCatalogRepository` через `ICatalogRepository`. Connection string за замовчуванням — `Data Source=library.db`; її можна змінити в конфігурації.

Для навчального етапу API викликає `EnsureCreated()` під час запуску. Це не замінює міграції: схема не версіонується, а зміни моделі в існуючій базі не керуються міграціями. Міграції будуть окремим наступним кроком.

Для `Book`–`Author` використано many-to-many, для книги–категорії та книги–примірника — зв’язки з зовнішніми ключами. На `InventoryNumber` створено унікальний SQLite-індекс із точним case-sensitive порівнянням. Сервісна перевірка доповнюється обмеженням БД для конкурентних конфліктів.

Пошук, видача та резервування ще не реалізовані.
