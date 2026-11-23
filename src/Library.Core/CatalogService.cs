namespace Library.Core;

public sealed class CatalogService
{
    private readonly ICatalogRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CatalogService(ICatalogRepository repository, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Reader> AddReaderAsync(string name)
    {
        var reader = new Reader(name);
        await _repository.AddReaderAsync(reader);
        await _repository.SaveChangesAsync();
        return reader;
    }

    public async Task<Author> AddAuthorAsync(string name)
    {
        var author = new Author(name);

        await _repository.AddAuthorAsync(author);
        await _repository.SaveChangesAsync();

        return author;
    }

    public async Task<Category> AddCategoryAsync(string name)
    {
        var category = new Category(name);

        await _repository.AddCategoryAsync(category);
        await _repository.SaveChangesAsync();

        return category;
    }

    public async Task<Book> AddBookAsync(string title, Guid categoryId, IEnumerable<Guid> authorIds)
    {
        if (authorIds is null)
        {
            throw new ArgumentNullException(nameof(authorIds));
        }

        var distinctAuthorIds = authorIds.Distinct().ToArray();

        if (distinctAuthorIds.Length == 0)
        {
            throw new ArgumentException("At least one author is required.", nameof(authorIds));
        }

        var category = await _repository.GetCategoryAsync(categoryId);
        if (category is null)
        {
            throw new ArgumentException("Unknown category.", nameof(categoryId));
        }

        var authors = new Author[distinctAuthorIds.Length];
        for (var i = 0; i < distinctAuthorIds.Length; i++)
        {
            var author = await _repository.GetAuthorAsync(distinctAuthorIds[i]);
            if (author is null)
            {
                throw new ArgumentException("Unknown author.", nameof(authorIds));
            }

            authors[i] = author;
        }

        var book = new Book(title, category, authors);

        await _repository.AddBookAsync(book);
        await _repository.SaveChangesAsync();

        return book;
    }

    public async Task<BookCopy> AddCopyAsync(Guid bookId, string inventoryNumber)
    {
        var book = await _repository.GetBookAsync(bookId);
        if (book is null)
        {
            throw new KeyNotFoundException("Unknown book.");
        }

        var copy = new BookCopy(book, inventoryNumber);

        if (await _repository.InventoryNumberExistsAsync(copy.InventoryNumber))
        {
            throw new DuplicateInventoryNumberException(copy.InventoryNumber);
        }

        await _repository.AddBookCopyAsync(copy);
        await _repository.SaveChangesAsync();

        return copy;
    }

    public async Task<IReadOnlyList<Book>> SearchBooksAsync(
        string? title = null,
        string? author = null,
        string? category = null,
        bool availableOnly = false)
    {
        var normalizedTitle = NormalizeFilter(title);
        var normalizedAuthor = NormalizeFilter(author);
        var normalizedCategory = NormalizeFilter(category);
        var books = await _repository.GetBooksAsync();
        var copies = availableOnly ? await _repository.GetBookCopiesAsync() : Array.Empty<BookCopy>();
        var availableBookIds = availableOnly
            ? copies
                .Where(copy => copy.Status == BookCopyStatus.Available)
                .Select(copy => copy.Book.Id)
                .ToHashSet()
            : null;

        return books
            .Where(book => normalizedTitle is null || Contains(book.Title, normalizedTitle))
            .Where(book => normalizedCategory is null || Contains(book.Category.Name, normalizedCategory))
            .Where(book => normalizedAuthor is null || book.Authors.Any(item => Contains(item.Name, normalizedAuthor)))
            .Where(book => !availableOnly || availableBookIds!.Contains(book.Id))
            .ToList();
    }

    public Task<Reader?> GetReaderAsync(Guid id) => _repository.GetReaderAsync(id);

    public Task<Loan?> GetLoanAsync(Guid id) => _repository.GetLoanAsync(id);

    public Task<Reservation?> GetReservationAsync(Guid id) => _repository.GetReservationAsync(id);

    public async Task<Loan> IssueLoanAsync(Guid readerId, Guid copyId, DateTime dueAtUtc)
    {
        var now = UtcNow;
        if (dueAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Due date must be UTC.", nameof(dueAtUtc));
        }

        if (dueAtUtc <= now)
        {
            throw new ArgumentException("Due date must be strictly in the future.", nameof(dueAtUtc));
        }

        var reader = await _repository.GetReaderAsync(readerId)
            ?? throw new KeyNotFoundException("Unknown reader.");
        var copy = await _repository.GetBookCopyAsync(copyId)
            ?? throw new KeyNotFoundException("Unknown copy.");
        var reservation = await _repository.GetActiveReservationForCopyAsync(copyId);

        if (copy.Status == BookCopyStatus.Reserved &&
            (reservation is null || reservation.Reader.Id != readerId))
        {
            throw new LoanConflictException("Copy is reserved for another reader.");
        }

        copy.Issue();
        var loan = new Loan(reader, copy, now, dueAtUtc);
        if (reservation is not null && reservation.Reader.Id == readerId)
        {
            reservation.Fulfill(now);
        }

        await _repository.AddLoanAsync(loan);
        await _repository.SaveChangesAsync();
        return loan;
    }

    public async Task<Loan> ReturnLoanAsync(Guid loanId)
    {
        var loan = await _repository.GetLoanAsync(loanId)
            ?? throw new KeyNotFoundException("Unknown loan.");
        if (loan.ReturnedAtUtc is not null)
        {
            throw new LoanConflictException("Loan has already been returned.");
        }

        loan.Copy.Return();
        loan.MarkReturned(UtcNow);
        await _repository.SaveChangesAsync();
        return loan;
    }

    public Task<IReadOnlyList<Loan>> GetOverdueLoansAsync() =>
        _repository.GetOverdueLoansAsync(UtcNow);

    public Task<IReadOnlyList<Loan>> GetReaderHistoryAsync(Guid readerId) =>
        _repository.GetLoansForReaderAsync(readerId);

    public async Task<Reservation> ReserveCopyAsync(Guid readerId, Guid copyId)
    {
        var reader = await _repository.GetReaderAsync(readerId)
            ?? throw new KeyNotFoundException("Unknown reader.");
        var copy = await _repository.GetBookCopyAsync(copyId)
            ?? throw new KeyNotFoundException("Unknown copy.");
        if (copy.Status != BookCopyStatus.Available)
        {
            throw new ReservationConflictException("Copy is not available for reservation.");
        }

        copy.Reserve();
        var reservation = new Reservation(reader, copy, UtcNow);
        await _repository.AddReservationAsync(reservation);
        await _repository.SaveChangesAsync();
        return reservation;
    }

    public async Task<Reservation> CancelReservationAsync(Guid reservationId, Guid readerId)
    {
        var reservation = await _repository.GetReservationAsync(reservationId)
            ?? throw new KeyNotFoundException("Unknown reservation.");
        if (reservation.Reader.Id != readerId)
        {
            throw new ReservationConflictException("Only the reservation owner can cancel it.");
        }

        if (reservation.Status != ReservationStatus.Active)
        {
            throw new ReservationConflictException("Reservation is no longer active.");
        }

        reservation.Copy.CancelReservation();
        reservation.Cancel(UtcNow);
        await _repository.SaveChangesAsync();
        return reservation;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    private static string? NormalizeFilter(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool Contains(string value, string filter) =>
        value.Contains(filter, StringComparison.OrdinalIgnoreCase);
}
