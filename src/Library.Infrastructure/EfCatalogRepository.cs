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

    public async Task SaveChangesAsync()
    {
        try
        {
            await dbContext.SaveChangesAsync();
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
