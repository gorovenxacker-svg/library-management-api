using System.ComponentModel.DataAnnotations;
using Library.Core;

namespace Library.Api.Contracts;

public sealed class CreateReaderRequest
{
    [Required]
    public string? Name { get; init; }
}

public sealed class CreateLoanRequest
{
    public Guid ReaderId { get; init; }

    public Guid CopyId { get; init; }

    public DateTime DueAtUtc { get; init; }
}

public sealed class CreateReservationRequest
{
    public Guid ReaderId { get; init; }

    public Guid CopyId { get; init; }
}

public sealed class CancelReservationRequest
{
    public Guid ReaderId { get; init; }
}

public sealed record ReaderResponse(Guid Id, string Name);

public sealed record LoanResponse(
    Guid Id,
    Guid ReaderId,
    Guid CopyId,
    DateTime IssuedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc);

public sealed record ReservationResponse(
    Guid Id,
    Guid ReaderId,
    Guid CopyId,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    ReservationStatus Status);
