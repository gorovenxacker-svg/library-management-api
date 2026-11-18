namespace Library.Core;

public sealed class Category
{
    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        Name = name;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; }
}
