namespace Library.Core;

public sealed class CatalogService
{
    private readonly ICatalogRepository _repository;

    public CatalogService(ICatalogRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
}
