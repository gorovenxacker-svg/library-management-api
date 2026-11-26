using Library.Core;

namespace Library.UnitTests;

public class CatalogServiceTests
{
    [Fact]
    public async Task AddAuthorAsync_ShouldAddAndSaveAuthor()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository);

        var author = await service.AddAuthorAsync("Леся Українка");

        Assert.Equal("Леся Українка", author.Name);
        Assert.Contains(author, repository.Authors);
        Assert.True(repository.SaveChangesCallCount > 0);
    }

    [Fact]
    public async Task AddCategoryAsync_ShouldAddAndSaveCategory()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository);

        var category = await service.AddCategoryAsync("Драма");

        Assert.Equal("Драма", category.Name);
        Assert.Contains(category, repository.Categories);
        Assert.True(repository.SaveChangesCallCount > 0);
    }

    [Fact]
    public async Task AddBookAsync_ShouldAddBookWithTwoAuthors()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var firstAuthor = await SeedAuthor(repository, "Леся Українка");
        var secondAuthor = await SeedAuthor(repository, "Тарас Шевченко");
        var service = new CatalogService(repository);

        var book = await service.AddBookAsync(
            "Лісова пісня",
            category.Id,
            new[] { firstAuthor.Id, secondAuthor.Id });

        Assert.Equal("Лісова пісня", book.Title);
        Assert.Same(category, book.Category);
        Assert.Equal(2, book.Authors.Count);
        Assert.Contains(firstAuthor, book.Authors);
        Assert.Contains(secondAuthor, book.Authors);
        Assert.Contains(book, repository.Books);
        Assert.True(repository.SaveChangesCallCount > 0);
    }

    [Fact]
    public async Task AddCopyAsync_ShouldAddAvailableCopy()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var book = await SeedBook(repository, "Лісова пісня", category, author);
        var service = new CatalogService(repository);

        var copy = await service.AddCopyAsync(book.Id, "INV-001");

        Assert.Same(book, copy.Book);
        Assert.Equal("INV-001", copy.InventoryNumber);
        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.Contains(copy, repository.BookCopies);
        Assert.True(repository.SaveChangesCallCount > 0);
    }

    [Fact]
    public async Task AddBookAsync_WithUnknownCategory_ShouldThrowArgumentExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var author = await SeedAuthor(repository, "Леся Українка");
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddBookAsync("Лісова пісня", Guid.NewGuid(), new[] { author.Id }));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddBookAsync_WithUnknownAuthor_ShouldThrowArgumentExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddBookAsync("Лісова пісня", category.Id, new[] { Guid.NewGuid() }));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddCopyAsync_WithUnknownBook_ShouldThrowKeyNotFoundExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddCopyAsync(Guid.NewGuid(), "INV-001"));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddCopyAsync_WithDuplicateInventoryNumber_ShouldThrowDuplicateInventoryNumberExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var book = await SeedBook(repository, "Лісова пісня", category, author);
        await SeedCopy(repository, book, "INV-001");
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<DuplicateInventoryNumberException>(() =>
            service.AddCopyAsync(book.Id, "INV-001"));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddBookAsync_WithNullAuthorIds_ShouldThrowArgumentNullExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.AddBookAsync("Лісова пісня", category.Id, null!));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddBookAsync_WithEmptyAuthorIds_ShouldThrowArgumentExceptionWithoutSave()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddBookAsync("Лісова пісня", category.Id, Array.Empty<Guid>()));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AddCopyAsync_WithDuplicateInventoryNumber_ShouldNotIncreaseBookCopies()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var book = await SeedBook(repository, "Лісова пісня", category, author);
        await SeedCopy(repository, book, "INV-001");
        var initialCount = repository.BookCopies.Count;
        var service = new CatalogService(repository);

        await Assert.ThrowsAsync<DuplicateInventoryNumberException>(() =>
            service.AddCopyAsync(book.Id, "INV-001"));

        Assert.Equal(initialCount, repository.BookCopies.Count);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DuplicateInventoryNumber_CheckShouldBeCaseSensitive()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var book = await SeedBook(repository, "Лісова пісня", category, author);
        await SeedCopy(repository, book, "INV-001");
        var service = new CatalogService(repository);

        var copy = await service.AddCopyAsync(book.Id, "inv-001");

        Assert.Equal("inv-001", copy.InventoryNumber);
    }

    [Fact]
    public async Task SearchBooksAsync_ShouldCombineTrimmedFiltersWithAnd()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        await SeedBook(repository, "Лісова пісня", category, author);
        await SeedBook(repository, "Кайдашева сім'я", category, author);
        var service = new CatalogService(repository);

        var result = await service.SearchBooksAsync("  ЛІСОВА ", "  УКРАЇНКА ", " ДРАМ ");

        var book = Assert.Single(result);
        Assert.Equal("Лісова пісня", book.Title);
    }

    [Fact]
    public async Task SearchBooksAsync_ShouldMatchUnicodeAndAnyAuthor()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Фентезі");
        var firstAuthor = await SeedAuthor(repository, "Марія Матіос");
        var secondAuthor = await SeedAuthor(repository, "Оксана Лущевська");
        var book = new Book("Українська історія", category, new[] { firstAuthor, secondAuthor });
        await repository.AddBookAsync(book);
        var service = new CatalogService(repository);

        var result = await service.SearchBooksAsync(author: "ЛУЩЕВСЬКА");

        Assert.Same(book, Assert.Single(result));
    }

    [Fact]
    public async Task SearchBooksAsync_WithBlankFilters_ShouldIgnoreThem()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var book = await SeedBook(repository, "Лісова пісня", category, author);
        var service = new CatalogService(repository);

        var result = await service.SearchBooksAsync(" ", "\t", "   ");

        Assert.Same(book, Assert.Single(result));
    }

    [Fact]
    public async Task SearchBooksAsync_WithNoMatchingFilters_ShouldReturnEmpty()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        await SeedBook(repository, "Лісова пісня", category, author);
        var service = new CatalogService(repository);

        var result = await service.SearchBooksAsync(title: "Немає такої книги");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchBooksAsync_AvailableOnly_ShouldReturnBooksWithAvailableCopyOnce()
    {
        var repository = new InMemoryCatalogRepository();
        var category = await SeedCategory(repository, "Драма");
        var author = await SeedAuthor(repository, "Леся Українка");
        var availableBook = await SeedBook(repository, "Лісова пісня", category, author);
        await SeedCopy(repository, availableBook, "INV-001");
        await SeedCopy(repository, availableBook, "INV-002");
        await SeedBook(repository, "Кайдашева сім'я", category, author);
        var service = new CatalogService(repository);

        var result = await service.SearchBooksAsync(availableOnly: true);

        var book = Assert.Single(result);
        Assert.Same(availableBook, book);
    }

    private static async Task<Category> SeedCategory(InMemoryCatalogRepository repository, string name)
    {
        var category = new Category(name);
        await repository.AddCategoryAsync(category);
        return category;
    }

    private static async Task<Author> SeedAuthor(InMemoryCatalogRepository repository, string name)
    {
        var author = new Author(name);
        await repository.AddAuthorAsync(author);
        return author;
    }

    private static async Task<Book> SeedBook(InMemoryCatalogRepository repository, string title, Category category, Author author)
    {
        var book = new Book(title, category, new[] { author });
        await repository.AddBookAsync(book);
        return book;
    }

    private static async Task SeedCopy(InMemoryCatalogRepository repository, Book book, string inventoryNumber)
    {
        await repository.AddBookCopyAsync(new BookCopy(book, inventoryNumber));
    }

    private sealed class InMemoryCatalogRepository : ICatalogRepository
    {
        public List<Author> Authors { get; } = new();
        public List<Category> Categories { get; } = new();
        public List<Book> Books { get; } = new();
        public List<BookCopy> BookCopies { get; } = new();
        public List<Reader> Readers { get; } = new();
        public List<Loan> Loans { get; } = new();
        public List<Reservation> Reservations { get; } = new();
        public int SaveChangesCallCount { get; private set; }

        public Task<Author?> GetAuthorAsync(Guid id)
        {
            return Task.FromResult(Authors.FirstOrDefault(author => author.Id == id));
        }

        public Task<IReadOnlyList<Author>> GetAuthorsAsync()
        {
            return Task.FromResult<IReadOnlyList<Author>>(Authors);
        }

        public Task<Category?> GetCategoryAsync(Guid id)
        {
            return Task.FromResult(Categories.FirstOrDefault(category => category.Id == id));
        }

        public Task<IReadOnlyList<Category>> GetCategoriesAsync()
        {
            return Task.FromResult<IReadOnlyList<Category>>(Categories);
        }

        public Task<Book?> GetBookAsync(Guid id)
        {
            return Task.FromResult(Books.FirstOrDefault(book => book.Id == id));
        }

        public Task<IReadOnlyList<Book>> GetBooksAsync()
        {
            return Task.FromResult<IReadOnlyList<Book>>(Books);
        }

        public Task<BookCopy?> GetBookCopyAsync(Guid id)
        {
            return Task.FromResult(BookCopies.FirstOrDefault(copy => copy.Id == id));
        }

        public Task<IReadOnlyList<BookCopy>> GetBookCopiesAsync()
        {
            return Task.FromResult<IReadOnlyList<BookCopy>>(BookCopies);
        }

        public Task<Reader?> GetReaderAsync(Guid id)
        {
            return Task.FromResult(Readers.FirstOrDefault(reader => reader.Id == id));
        }

        public Task<Loan?> GetLoanAsync(Guid id)
        {
            return Task.FromResult(Loans.FirstOrDefault(loan => loan.Id == id));
        }

        public Task<IReadOnlyList<Loan>> GetLoansForReaderAsync(Guid readerId)
        {
            return Task.FromResult<IReadOnlyList<Loan>>(Loans.Where(loan => loan.Reader.Id == readerId).ToList());
        }

        public Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime nowUtc)
        {
            return Task.FromResult<IReadOnlyList<Loan>>(Loans
                .Where(loan => loan.ReturnedAtUtc is null && loan.DueAtUtc < nowUtc)
                .ToList());
        }

        public Task<Reservation?> GetReservationAsync(Guid id)
        {
            return Task.FromResult(Reservations.FirstOrDefault(reservation => reservation.Id == id));
        }

        public Task<Reservation?> GetActiveReservationForCopyAsync(Guid copyId)
        {
            return Task.FromResult(Reservations.FirstOrDefault(reservation =>
                reservation.Copy.Id == copyId && reservation.Status == ReservationStatus.Active));
        }

        public Task<bool> InventoryNumberExistsAsync(string inventoryNumber)
        {
            var exists = BookCopies.Any(copy =>
                string.Equals(copy.InventoryNumber, inventoryNumber, StringComparison.Ordinal));

            return Task.FromResult(exists);
        }

        public Task AddAuthorAsync(Author author)
        {
            Authors.Add(author);
            return Task.CompletedTask;
        }

        public Task AddCategoryAsync(Category category)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task AddBookAsync(Book book)
        {
            Books.Add(book);
            return Task.CompletedTask;
        }

        public Task AddBookCopyAsync(BookCopy bookCopy)
        {
            BookCopies.Add(bookCopy);
            return Task.CompletedTask;
        }

        public Task AddReaderAsync(Reader reader)
        {
            Readers.Add(reader);
            return Task.CompletedTask;
        }

        public Task AddLoanAsync(Loan loan)
        {
            Loans.Add(loan);
            return Task.CompletedTask;
        }

        public Task AddReservationAsync(Reservation reservation)
        {
            Reservations.Add(reservation);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
