using Library.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public sealed class EfCatalogRepository(LibraryDbContext dbContext) : ICatalogRepository
{
    public Task<Author?> GetAuthorAsync(Guid id) =>
        dbContext.Authors.SingleOrDefaultAsync(author => author.Id == id);

    public async Task<IReadOnlyList<Author>> GetAuthorsAsync() =>
        await dbContext.Authors.AsNoTracking().OrderBy(author => author.Name).ToListAsync();

    public Task<Category?> GetCategoryAsync(Guid id) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync() =>
        await dbContext.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();

    public Task<Book?> GetBookAsync(Guid id) =>
        dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors)
            .SingleOrDefaultAsync(book => book.Id == id);

    public async Task<IReadOnlyList<Book>> GetBooksAsync() =>
        await dbContext.Books
            .AsNoTracking()
            .Include(book => book.Category)
            .Include(book => book.Authors)
            .OrderBy(book => book.Title)
            .ToListAsync();

    public Task<BookCopy?> GetBookCopyAsync(Guid id) =>
        dbContext.BookCopies
            .Include(copy => copy.Book)
            .SingleOrDefaultAsync(copy => copy.Id == id);

    public Task<Reader?> GetReaderAsync(Guid id) =>
        dbContext.Readers.SingleOrDefaultAsync(reader => reader.Id == id);

    public Task<Loan?> GetLoanAsync(Guid id) =>
        dbContext.Loans
            .Include(loan => loan.Reader)
            .Include(loan => loan.Copy)
            .ThenInclude(copy => copy.Book)
            .SingleOrDefaultAsync(loan => loan.Id == id);

    public async Task<IReadOnlyList<Loan>> GetLoansForReaderAsync(Guid readerId) =>
        await dbContext.Loans
            .AsNoTracking()
            .Include(loan => loan.Reader)
            .Include(loan => loan.Copy)
            .ThenInclude(copy => copy.Book)
            .Where(loan => loan.Reader.Id == readerId)
            .OrderByDescending(loan => loan.IssuedAtUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime nowUtc) =>
        await dbContext.Loans
            .AsNoTracking()
            .Include(loan => loan.Reader)
            .Include(loan => loan.Copy)
            .ThenInclude(copy => copy.Book)
            .Where(loan => loan.ReturnedAtUtc == null && loan.DueAtUtc < nowUtc)
            .OrderBy(loan => loan.DueAtUtc)
            .ToListAsync();

    public Task<Reservation?> GetReservationAsync(Guid id) =>
        dbContext.Reservations
            .Include(reservation => reservation.Reader)
            .Include(reservation => reservation.Copy)
            .ThenInclude(copy => copy.Book)
            .SingleOrDefaultAsync(reservation => reservation.Id == id);

    public Task<Reservation?> GetActiveReservationForCopyAsync(Guid copyId) =>
        dbContext.Reservations
            .Include(reservation => reservation.Reader)
            .Include(reservation => reservation.Copy)
            .Where(reservation => reservation.Copy.Id == copyId && reservation.Status == ReservationStatus.Active)
            .SingleOrDefaultAsync();

    public async Task<IReadOnlyList<BookCopy>> GetBookCopiesAsync() =>
        await dbContext.BookCopies
            .AsNoTracking()
            .Include(copy => copy.Book)
            .OrderBy(copy => copy.InventoryNumber)
            .ToListAsync();

    public Task<bool> InventoryNumberExistsAsync(string inventoryNumber) =>
        dbContext.BookCopies.AnyAsync(copy => copy.InventoryNumber == inventoryNumber);

    public Task AddAuthorAsync(Author author) =>
        dbContext.Authors.AddAsync(author).AsTask();

    public Task AddCategoryAsync(Category category) =>
        dbContext.Categories.AddAsync(category).AsTask();

    public Task AddBookAsync(Book book) =>
        dbContext.Books.AddAsync(book).AsTask();

    public Task AddBookCopyAsync(BookCopy bookCopy) =>
        dbContext.BookCopies.AddAsync(bookCopy).AsTask();

    public Task AddReaderAsync(Reader reader) =>
        dbContext.Readers.AddAsync(reader).AsTask();

    public Task AddLoanAsync(Loan loan) =>
        dbContext.Loans.AddAsync(loan).AsTask();

    public Task AddReservationAsync(Reservation reservation) =>
        dbContext.Reservations.AddAsync(reservation).AsTask();

    public async Task SaveChangesAsync()
    {
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new CirculationConcurrencyException("The copy was changed by another operation.", exception);
        }
        catch (DbUpdateException exception) when (IsInventoryNumberConstraint(exception))
        {
            var inventoryNumber = dbContext.ChangeTracker
                .Entries<BookCopy>()
                .Select(entry => entry.Entity.InventoryNumber)
                .FirstOrDefault() ?? string.Empty;

            throw new DuplicateInventoryNumberException(inventoryNumber, exception);
        }
    }

    private static bool IsInventoryNumberConstraint(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException sqliteException
            && sqliteException.SqliteErrorCode == 19
            && sqliteException.SqliteExtendedErrorCode == 2067
            && sqliteException.Message.Contains("BookCopies.InventoryNumber", StringComparison.Ordinal);
    }
}
