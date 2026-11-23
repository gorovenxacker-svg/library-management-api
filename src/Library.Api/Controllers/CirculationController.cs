using Library.Api.Contracts;
using Library.Core;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CirculationController(CatalogService service) : ControllerBase
{
    [HttpPost("readers")]
    public async Task<ActionResult<ReaderResponse>> AddReader(CreateReaderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationError(nameof(request.Name), "Reader name cannot be empty.");
        }

        var reader = await service.AddReaderAsync(request.Name!);
        return CreatedAtAction(nameof(GetReader), new { id = reader.Id }, ToResponse(reader));
    }

    [HttpGet("readers/{id:guid}")]
    public async Task<ActionResult<ReaderResponse>> GetReader(Guid id)
    {
        var reader = await service.GetReaderAsync(id);
        return reader is null ? NotFound() : Ok(ToResponse(reader));
    }

    [HttpPost("loans")]
    public async Task<ActionResult<LoanResponse>> IssueLoan(CreateLoanRequest request)
    {
        if (request.ReaderId == Guid.Empty || request.CopyId == Guid.Empty || request.DueAtUtc == default)
        {
            return ValidationError(nameof(request), "ReaderId, CopyId and DueAtUtc are required.");
        }

        if (request.DueAtUtc.Kind != DateTimeKind.Utc)
        {
            return ValidationError(nameof(request.DueAtUtc), "DueAtUtc must be a UTC timestamp.");
        }

        try
        {
            var loan = await service.IssueLoanAsync(request.ReaderId, request.CopyId, request.DueAtUtc);
            return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, ToResponse(loan));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (LoanConflictException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.ParamName ?? nameof(request), exception.Message);
        }
        catch (CirculationConcurrencyException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpGet("loans/{id:guid}")]
    public async Task<ActionResult<LoanResponse>> GetLoan(Guid id)
    {
        var loan = await service.GetLoanAsync(id);
        return loan is null ? NotFound() : Ok(ToResponse(loan));
    }

    [HttpPost("loans/{id:guid}/return")]
    public async Task<ActionResult<LoanResponse>> ReturnLoan(Guid id)
    {
        try
        {
            var loan = await service.ReturnLoanAsync(id);
            return Ok(ToResponse(loan));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (LoanConflictException exception)
        {
            return Conflict(exception.Message);
        }
        catch (CirculationConcurrencyException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpGet("loans/overdue")]
    public async Task<ActionResult<IReadOnlyList<LoanResponse>>> GetOverdueLoans()
    {
        var loans = await service.GetOverdueLoansAsync();
        return Ok(loans.Select(ToResponse).ToList());
    }

    [HttpGet("readers/{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<LoanResponse>>> GetHistory(Guid id)
    {
        var reader = await service.GetReaderAsync(id);
        if (reader is null)
        {
            return NotFound();
        }

        var loans = await service.GetReaderHistoryAsync(id);
        return Ok(loans.Select(ToResponse).ToList());
    }

    [HttpPost("reservations")]
    public async Task<ActionResult<ReservationResponse>> Reserve(CreateReservationRequest request)
    {
        if (request.ReaderId == Guid.Empty || request.CopyId == Guid.Empty)
        {
            return ValidationError(nameof(request), "ReaderId and CopyId are required.");
        }

        try
        {
            var reservation = await service.ReserveCopyAsync(request.ReaderId, request.CopyId);
            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, ToResponse(reservation));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ReservationConflictException exception)
        {
            return Conflict(exception.Message);
        }
        catch (CirculationConcurrencyException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpGet("reservations/{id:guid}")]
    public async Task<ActionResult<ReservationResponse>> GetReservation(Guid id)
    {
        var reservation = await service.GetReservationAsync(id);
        return reservation is null ? NotFound() : Ok(ToResponse(reservation));
    }

    [HttpPost("reservations/{id:guid}/cancel")]
    public async Task<ActionResult<ReservationResponse>> Cancel(Guid id, CancelReservationRequest request)
    {
        if (request.ReaderId == Guid.Empty)
        {
            return ValidationError(nameof(request.ReaderId), "ReaderId is required.");
        }

        try
        {
            var reservation = await service.CancelReservationAsync(id, request.ReaderId);
            return Ok(ToResponse(reservation));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ReservationConflictException exception)
        {
            return Conflict(exception.Message);
        }
        catch (CirculationConcurrencyException exception)
        {
            return Conflict(exception.Message);
        }
    }

    private ActionResult ValidationError(string field, string detail)
    {
        ModelState.AddModelError(field, detail);
        return ValidationProblem(ModelState);
    }

    private static ReaderResponse ToResponse(Reader reader) => new(reader.Id, reader.Name);

    private static LoanResponse ToResponse(Loan loan) => new(
        loan.Id,
        loan.Reader.Id,
        loan.Copy.Id,
        loan.IssuedAtUtc,
        loan.DueAtUtc,
        loan.ReturnedAtUtc);

    private static ReservationResponse ToResponse(Reservation reservation) => new(
        reservation.Id,
        reservation.Reader.Id,
        reservation.Copy.Id,
        reservation.CreatedAtUtc,
        reservation.ClosedAtUtc,
        reservation.Status);
}
