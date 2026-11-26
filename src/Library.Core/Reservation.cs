namespace Library.Core;

public sealed class Reservation
{
    private Reservation()
    {
        Reader = null!;
        Copy = null!;
    }

    public Reservation(Reader reader, BookCopy copy, DateTime createdAtUtc)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Copy = copy ?? throw new ArgumentNullException(nameof(copy));
        CreatedAtUtc = createdAtUtc;
        Status = ReservationStatus.Active;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Reader Reader { get; private set; } = null!;

    public BookCopy Copy { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public ReservationStatus Status { get; private set; }

    public void Fulfill(DateTime closedAtUtc)
    {
        EnsureActive();
        Status = ReservationStatus.Fulfilled;
        ClosedAtUtc = closedAtUtc;
    }

    public void Cancel(DateTime closedAtUtc)
    {
        EnsureActive();
        Status = ReservationStatus.Cancelled;
        ClosedAtUtc = closedAtUtc;
    }

    private void EnsureActive()
    {
        if (Status != ReservationStatus.Active)
        {
            throw new ReservationConflictException("Reservation is no longer active.");
        }
    }
}
