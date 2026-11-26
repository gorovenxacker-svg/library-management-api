using Library.Core;

namespace Library.UnitTests;

public sealed class CirculationTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BookCopyTransitions_ShouldFollowAllowedStatuses()
    {
        var copy = CreateCopy();

        var initialVersion = copy.Version;
        copy.Reserve();
        Assert.Equal(BookCopyStatus.Reserved, copy.Status);
        Assert.NotEqual(initialVersion, copy.Version);
        var reservedVersion = copy.Version;
        copy.Issue();
        Assert.Equal(BookCopyStatus.Loaned, copy.Status);
        Assert.NotEqual(reservedVersion, copy.Version);
        var loanedVersion = copy.Version;
        copy.Return();
        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.NotEqual(loanedVersion, copy.Version);
    }

    [Fact]
    public void Loan_ShouldRequireUtcTimesAndStrictlyLaterDueTime()
    {
        var copy = CreateCopy();
        var reader = new Reader("Читач");

        Assert.Throws<ArgumentException>(() => new Loan(reader, copy, Now, Now.AddHours(1).ToLocalTime()));
        Assert.Throws<ArgumentException>(() => new Loan(reader, copy, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), Now.AddHours(1)));
        Assert.Throws<ArgumentException>(() => new Loan(reader, copy, Now, Now));
    }

    [Fact]
    public async Task IssueLoan_ShouldRejectDueAtNowAndBeforeNow()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var reader, out var copy);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() => service.IssueLoanAsync(reader.Id, copy.Id, Now));
        await Assert.ThrowsAsync<ArgumentException>(() => service.IssueLoanAsync(reader.Id, copy.Id, Now.AddMinutes(-1)));
    }

    [Fact]
    public async Task Overdue_ShouldUseStrictlyEarlierDueTimeAndIgnoreReturnedLoans()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var reader, out var copy);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));
        copy.Issue();
        var returnedOverdue = new Loan(reader, copy, Now.AddHours(-2), Now.AddMinutes(-1));
        returnedOverdue.MarkReturned(Now);
        repository.Loans.Add(returnedOverdue);

        var overdueCopy = CreateCopy();
        overdueCopy.Issue();
        var activeOverdue = new Loan(reader, overdueCopy, Now.AddHours(-2), Now.AddMinutes(-1));
        repository.BookCopies.Add(overdueCopy);
        repository.Loans.Add(activeOverdue);

        var equalCopy = CreateCopy();
        equalCopy.Issue();
        var dueNow = new Loan(reader, equalCopy, Now.AddHours(-2), Now);
        repository.BookCopies.Add(equalCopy);
        repository.Loans.Add(dueNow);

        var result = await service.GetOverdueLoansAsync();

        Assert.Contains(activeOverdue, result);
        Assert.DoesNotContain(returnedOverdue, result);
        Assert.DoesNotContain(dueNow, result);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task Reservation_ShouldRestrictLoanToOwnerAndCancelOnlyByOwner()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var owner, out var copy);
        var otherReader = new Reader("Інший читач");
        repository.Readers.Add(otherReader);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));
        var reservation = await service.ReserveCopyAsync(owner.Id, copy.Id);

        await Assert.ThrowsAsync<LoanConflictException>(() =>
            service.IssueLoanAsync(otherReader.Id, copy.Id, Now.AddHours(1)));
        await Assert.ThrowsAsync<ReservationConflictException>(() =>
            service.CancelReservationAsync(reservation.Id, otherReader.Id));

        var loan = await service.IssueLoanAsync(owner.Id, copy.Id, Now.AddHours(1));

        Assert.Equal(BookCopyStatus.Loaned, copy.Status);
        Assert.Equal(ReservationStatus.Fulfilled, reservation.Status);
        Assert.Same(reservation.Reader, loan.Reader);
    }

    [Fact]
    public async Task ReturnLoan_ShouldPreserveHistoryAndMakeCopyAvailable()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var reader, out var copy);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));
        var loan = await service.IssueLoanAsync(reader.Id, copy.Id, Now.AddHours(1));

        await service.ReturnLoanAsync(loan.Id);
        var history = await service.GetReaderHistoryAsync(reader.Id);

        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.Equal(Now, loan.ReturnedAtUtc);
        Assert.Contains(loan, history);
        await Assert.ThrowsAsync<LoanConflictException>(() => service.ReturnLoanAsync(loan.Id));
    }

    [Fact]
    public async Task ReturningOldLoanAfterReissue_ShouldConflictWithoutChangingCurrentLoan()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var reader, out var copy);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));
        var firstLoan = await service.IssueLoanAsync(reader.Id, copy.Id, Now.AddHours(1));

        await service.ReturnLoanAsync(firstLoan.Id);
        var secondLoan = await service.IssueLoanAsync(reader.Id, copy.Id, Now.AddHours(2));

        await Assert.ThrowsAsync<LoanConflictException>(() => service.ReturnLoanAsync(firstLoan.Id));

        Assert.Equal(BookCopyStatus.Loaned, copy.Status);
        Assert.Null(secondLoan.ReturnedAtUtc);
    }

    [Fact]
    public async Task CancellingOldReservationAfterNewReservation_ShouldConflictWithoutChangingCurrentReservation()
    {
        var repository = CreateRepositoryWithAvailableCopy(out var reader, out var copy);
        var service = new CatalogService(repository, new FixedTimeProvider(Now));
        var firstReservation = await service.ReserveCopyAsync(reader.Id, copy.Id);

        await service.CancelReservationAsync(firstReservation.Id, reader.Id);
        var secondReservation = await service.ReserveCopyAsync(reader.Id, copy.Id);

        await Assert.ThrowsAsync<ReservationConflictException>(() =>
            service.CancelReservationAsync(firstReservation.Id, reader.Id));

        Assert.Equal(BookCopyStatus.Reserved, copy.Status);
        Assert.Equal(ReservationStatus.Active, secondReservation.Status);
    }

    private static InMemoryRepository CreateRepositoryWithAvailableCopy(out Reader reader, out BookCopy copy)
    {
        var repository = new InMemoryRepository();
        reader = new Reader("Читач");
        copy = CreateCopy();
        repository.Readers.Add(reader);
        repository.BookCopies.Add(copy);
        return repository;
    }

    private static BookCopy CreateCopy()
    {
        var category = new Category("Драма");
        var author = new Author("Леся Українка");
        var book = new Book("Лісова пісня", category, new[] { author });
        return new BookCopy(book, Guid.NewGuid().ToString());
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class InMemoryRepository : ICatalogRepository
    {
        public List<Author> Authors { get; } = new();
        public List<Category> Categories { get; } = new();
        public List<Book> Books { get; } = new();
        public List<BookCopy> BookCopies { get; } = new();
        public List<Reader> Readers { get; } = new();
        public List<Loan> Loans { get; } = new();
        public List<Reservation> Reservations { get; } = new();

        public Task<Author?> GetAuthorAsync(Guid id) => Task.FromResult(Authors.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Author>> GetAuthorsAsync() => Task.FromResult<IReadOnlyList<Author>>(Authors);
        public Task<Category?> GetCategoryAsync(Guid id) => Task.FromResult(Categories.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Category>> GetCategoriesAsync() => Task.FromResult<IReadOnlyList<Category>>(Categories);
        public Task<Book?> GetBookAsync(Guid id) => Task.FromResult(Books.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Book>> GetBooksAsync() => Task.FromResult<IReadOnlyList<Book>>(Books);
        public Task<BookCopy?> GetBookCopyAsync(Guid id) => Task.FromResult(BookCopies.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<BookCopy>> GetBookCopiesAsync() => Task.FromResult<IReadOnlyList<BookCopy>>(BookCopies);
        public Task<Reader?> GetReaderAsync(Guid id) => Task.FromResult(Readers.FirstOrDefault(x => x.Id == id));
        public Task<Loan?> GetLoanAsync(Guid id) => Task.FromResult(Loans.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Loan>> GetLoansForReaderAsync(Guid readerId) => Task.FromResult<IReadOnlyList<Loan>>(Loans.Where(x => x.Reader.Id == readerId).ToList());
        public Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime nowUtc) => Task.FromResult<IReadOnlyList<Loan>>(Loans.Where(x => x.ReturnedAtUtc is null && x.DueAtUtc < nowUtc).ToList());
        public Task<Reservation?> GetReservationAsync(Guid id) => Task.FromResult(Reservations.FirstOrDefault(x => x.Id == id));
        public Task<Reservation?> GetActiveReservationForCopyAsync(Guid copyId) => Task.FromResult(Reservations.FirstOrDefault(x => x.Copy.Id == copyId && x.Status == ReservationStatus.Active));
        public Task<bool> InventoryNumberExistsAsync(string inventoryNumber) => Task.FromResult(BookCopies.Any(x => x.InventoryNumber == inventoryNumber));
        public Task AddAuthorAsync(Author author) { Authors.Add(author); return Task.CompletedTask; }
        public Task AddCategoryAsync(Category category) { Categories.Add(category); return Task.CompletedTask; }
        public Task AddBookAsync(Book book) { Books.Add(book); return Task.CompletedTask; }
        public Task AddBookCopyAsync(BookCopy copy) { BookCopies.Add(copy); return Task.CompletedTask; }
        public Task AddReaderAsync(Reader reader) { Readers.Add(reader); return Task.CompletedTask; }
        public Task AddLoanAsync(Loan loan) { Loans.Add(loan); return Task.CompletedTask; }
        public Task AddReservationAsync(Reservation reservation) { Reservations.Add(reservation); return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
