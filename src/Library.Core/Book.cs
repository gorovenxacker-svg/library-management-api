namespace Library.Core;

public sealed class Book
{
    private readonly List<Author> _authors = new();

    private Book()
    {
        Title = null!;
        Category = null!;
    }

    public Book(string title, Category category, IEnumerable<Author> authors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Book title cannot be empty.", nameof(title));
        }

        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        if (authors is null)
        {
            throw new ArgumentNullException(nameof(authors));
        }

        var authorList = authors.ToArray();

        if (authorList.Length == 0)
        {
            throw new ArgumentException("At least one author is required.", nameof(authors));
        }

        if (authorList.Any(author => author is null))
        {
            throw new ArgumentException("Authors cannot contain null.", nameof(authors));
        }

        Title = title;
        Category = category;
        _authors.AddRange(authorList);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; } = null!;

    public Category Category { get; private set; } = null!;

    public IReadOnlyCollection<Author> Authors => _authors.AsReadOnly();
}
