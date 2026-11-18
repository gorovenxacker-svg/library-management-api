namespace Library.Core;

public sealed class Book
{
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
        Authors = Array.AsReadOnly(authorList);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; }

    public Category Category { get; }

    public IReadOnlyCollection<Author> Authors { get; }
}
