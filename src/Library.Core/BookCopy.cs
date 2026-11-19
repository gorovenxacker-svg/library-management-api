namespace Library.Core;

public sealed class BookCopy
{
    private BookCopy()
    {
        Book = null!;
        InventoryNumber = null!;
    }

    public BookCopy(Book book, string inventoryNumber)
    {
        if (book is null)
        {
            throw new ArgumentNullException(nameof(book));
        }

        if (string.IsNullOrWhiteSpace(inventoryNumber))
        {
            throw new ArgumentException("Inventory number cannot be empty.", nameof(inventoryNumber));
        }

        Book = book;
        InventoryNumber = inventoryNumber;
        Status = BookCopyStatus.Available;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Book Book { get; private set; } = null!;

    public string InventoryNumber { get; private set; } = null!;

    public BookCopyStatus Status { get; private set; }
}
