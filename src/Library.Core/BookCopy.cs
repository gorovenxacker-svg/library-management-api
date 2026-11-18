namespace Library.Core;

public sealed class BookCopy
{
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

    public Book Book { get; }

    public string InventoryNumber { get; }

    public BookCopyStatus Status { get; private set; }
}
