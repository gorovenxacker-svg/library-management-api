namespace Library.Core;

public sealed class Loan
{
    private Loan()
    {
        Reader = null!;
        Copy = null!;
    }

    public Loan(Reader reader, BookCopy copy, DateTime issuedAtUtc, DateTime dueAtUtc)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Copy = copy ?? throw new ArgumentNullException(nameof(copy));
        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Issued time must be UTC.", nameof(issuedAtUtc));
        }

        if (dueAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Due time must be UTC.", nameof(dueAtUtc));
        }

        if (dueAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException("Due time must be later than issued time.", nameof(dueAtUtc));
        }

        IssuedAtUtc = issuedAtUtc;
        DueAtUtc = dueAtUtc;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Reader Reader { get; private set; } = null!;

    public BookCopy Copy { get; private set; } = null!;

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime DueAtUtc { get; private set; }

    public DateTime? ReturnedAtUtc { get; private set; }

    public void MarkReturned(DateTime returnedAtUtc)
    {
        if (ReturnedAtUtc is not null)
        {
            throw new LoanConflictException("Loan has already been returned.");
        }

        if (returnedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Return time must be UTC.", nameof(returnedAtUtc));
        }

        ReturnedAtUtc = returnedAtUtc;
    }
}
