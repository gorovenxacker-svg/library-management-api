namespace Library.Core;

public sealed class Reader
{
    private Reader()
    {
        Name = null!;
    }

    public Reader(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reader name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = null!;
}
