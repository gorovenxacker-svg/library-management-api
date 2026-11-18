namespace Library.Core;

public interface ICatalogRepository
{
    Task<Author?> GetAuthorAsync(Guid id);

    Task<Category?> GetCategoryAsync(Guid id);

    Task<Book?> GetBookAsync(Guid id);

    Task<bool> InventoryNumberExistsAsync(string inventoryNumber);

    Task AddAuthorAsync(Author author);

    Task AddCategoryAsync(Category category);

    Task AddBookAsync(Book book);

    Task AddBookCopyAsync(BookCopy bookCopy);

    Task SaveChangesAsync();
}
