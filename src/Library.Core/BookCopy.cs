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
        Version = Guid.NewGuid();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid Version { get; private set; } = Guid.NewGuid();

    public Book Book { get; private set; } = null!;

    public string InventoryNumber { get; private set; } = null!;

    public BookCopyStatus Status { get; private set; }

    public void Reserve()
    {
        if (Status != BookCopyStatus.Available)
        {
            throw new ReservationConflictException("Only an available copy can be reserved.");
        }

        Status = BookCopyStatus.Reserved;
        Version = Guid.NewGuid();
    }

    public void Issue()
    {
        if (Status is not (BookCopyStatus.Available or BookCopyStatus.Reserved))
        {
            throw new LoanConflictException("Copy is not available for loan.");
        }

        Status = BookCopyStatus.Loaned;
        Version = Guid.NewGuid();
    }

    public void Return()
    {
        if (Status != BookCopyStatus.Loaned)
        {
            throw new LoanConflictException("Copy is not currently loaned.");
        }

        Status = BookCopyStatus.Available;
        Version = Guid.NewGuid();
    }

    public void CancelReservation()
    {
        if (Status != BookCopyStatus.Reserved)
        {
            throw new ReservationConflictException("Copy does not have an active reservation.");
        }

        Status = BookCopyStatus.Available;
        Version = Guid.NewGuid();
    }
}
