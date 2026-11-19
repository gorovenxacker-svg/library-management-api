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

    Task<bool> InventoryNumberExistsAsync(string inventoryNumber);

    Task AddAuthorAsync(Author author);

    Task AddCategoryAsync(Category category);

    Task AddBookAsync(Book book);

    Task AddBookCopyAsync(BookCopy bookCopy);

    Task SaveChangesAsync();
}
