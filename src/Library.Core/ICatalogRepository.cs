namespace Library.Core;

public interface ICatalogRepository
{
    Task<Author?> GetAuthorAsync(Guid id);

    Task<IReadOnlyList<Author>> GetAuthorsAsync();

    Task<Category?> GetCategoryAsync(Guid id);

    Task<IReadOnlyList<Category>> GetCategoriesAsync();

    Task<Book?> GetBookAsync(Guid id);

    Task<IReadOnlyList<Book>> GetBooksAsync();

    Task<BookCopy?> GetBookCopyAsync(Guid id);

    Task<IReadOnlyList<BookCopy>> GetBookCopiesAsync();

    Task<Reader?> GetReaderAsync(Guid id);

    Task<Loan?> GetLoanAsync(Guid id);

    Task<IReadOnlyList<Loan>> GetLoansForReaderAsync(Guid readerId);

    Task<IReadOnlyList<Loan>> GetOverdueLoansAsync(DateTime nowUtc);

    Task<Reservation?> GetReservationAsync(Guid id);

    Task<Reservation?> GetActiveReservationForCopyAsync(Guid copyId);

    Task<bool> InventoryNumberExistsAsync(string inventoryNumber);

    Task AddAuthorAsync(Author author);

    Task AddCategoryAsync(Category category);

    Task AddBookAsync(Book book);

    Task AddBookCopyAsync(BookCopy bookCopy);

    Task AddReaderAsync(Reader reader);

    Task AddLoanAsync(Loan loan);

    Task AddReservationAsync(Reservation reservation);

    Task SaveChangesAsync();
}
