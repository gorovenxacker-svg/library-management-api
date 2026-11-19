namespace Library.Core;

public sealed class Author
{
    private Author()
    {
        Name = null!;
    }

    public Author(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Author name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = null!;
}
