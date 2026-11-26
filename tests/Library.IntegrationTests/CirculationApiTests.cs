using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Library.Api.Contracts;
using Library.Core;
using Library.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Library.IntegrationTests;

public sealed class CirculationApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Circulation_PersistsAcrossRequestsAndUpdatesAvailability()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();
        var (book, copy) = await CreateCatalogAsync(client);

        var readerResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Читач" });
        Assert.Equal(HttpStatusCode.Created, readerResponse.StatusCode);
        var reader = await readerResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);
        Assert.NotNull(readerResponse.Headers.Location);
        Assert.NotNull(reader);

        var readerGet = await client.GetAsync(readerResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, readerGet.StatusCode);
        Assert.Equal(reader.Id, (await readerGet.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions))!.Id);

        var loanResponse = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = reader.Id, copyId = copy.Id, dueAtUtc = factory.Clock.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);
        var loan = await loanResponse.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.NotNull(loanResponse.Headers.Location);
        Assert.NotNull(loan);
        Assert.Equal(reader.Id, loan.ReaderId);
        Assert.Equal(copy.Id, loan.CopyId);

        var loanGet = await client.GetAsync(loanResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, loanGet.StatusCode);

        var unavailable = await client.GetAsync("/api/books?availableOnly=true");
        var unavailableBooks = await unavailable.Content.ReadFromJsonAsync<List<BookResponse>>(JsonOptions);
        Assert.DoesNotContain(unavailableBooks!, item => item.Id == book.Id);
        var unavailableReservation = await client.PostAsJsonAsync(
            "/api/reservations",
            new { readerId = reader.Id, copyId = copy.Id });
        Assert.Equal(HttpStatusCode.Conflict, unavailableReservation.StatusCode);

        var returnResponse = await client.PostAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        var returnedLoan = await returnResponse.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        Assert.NotNull(returnedLoan!.ReturnedAtUtc);

        var historyResponse = await client.GetAsync($"/api/readers/{reader.Id}/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<LoanResponse>>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.Contains(history!, item => item.Id == loan.Id && item.ReturnedAtUtc is not null);

        var reservationResponse = await client.PostAsJsonAsync(
            "/api/reservations",
            new { readerId = reader.Id, copyId = copy.Id });
        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);
        var reservation = await reservationResponse.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);
        Assert.NotNull(reservationResponse.Headers.Location);
        Assert.NotNull(reservation);
        var reservationGet = await client.GetAsync(reservationResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, reservationGet.StatusCode);
        var persistedReservation = await reservationGet.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);
        Assert.Equal(reservation.Id, persistedReservation!.Id);

        var reservedBooks = await (await client.GetAsync("/api/books?availableOnly=true"))
            .Content.ReadFromJsonAsync<List<BookResponse>>(JsonOptions);
        Assert.DoesNotContain(reservedBooks!, item => item.Id == book.Id);

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/reservations/{reservation.Id}/cancel",
            new { readerId = reader.Id });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var availableAgain = await (await client.GetAsync("/api/books?availableOnly=true"))
            .Content.ReadFromJsonAsync<List<BookResponse>>(JsonOptions);
        Assert.Contains(availableAgain!, item => item.Id == book.Id);
    }

    [Fact]
    public async Task MissingAndInvalidCirculationRequests_ReturnExpectedStatusCodes()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();

        var invalidReader = await client.PostAsJsonAsync("/api/readers", new { name = " " });
        Assert.Equal(HttpStatusCode.BadRequest, invalidReader.StatusCode);

        var missingLoan = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = Guid.NewGuid(), copyId = Guid.NewGuid(), dueAtUtc = factory.Clock.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.NotFound, missingLoan.StatusCode);

        var missingReservation = await client.PostAsJsonAsync(
            "/api/reservations",
            new { readerId = Guid.NewGuid(), copyId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, missingReservation.StatusCode);
    }

    [Fact]
    public async Task Overdue_UsesStrictBoundaryAndHistoryIsReaderSpecific()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();
        var (book, firstCopy) = await CreateCatalogAsync(client);
        var secondCopyResponse = await client.PostAsJsonAsync(
            $"/api/books/{book.Id}/copies",
            new { inventoryNumber = "CIRC-002" });
        var secondCopy = await secondCopyResponse.Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions);
        var firstReaderResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Перший" });
        var firstReader = await firstReaderResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);
        var secondReaderResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Другий" });
        var secondReader = await secondReaderResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);

        var firstLoanResponse = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = firstReader!.Id, copyId = firstCopy.Id, dueAtUtc = factory.Clock.UtcNow.AddMinutes(1) });
        var firstLoan = await firstLoanResponse.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        var secondLoanResponse = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = secondReader!.Id, copyId = secondCopy!.Id, dueAtUtc = factory.Clock.UtcNow.AddMinutes(1) });
        var secondLoan = await secondLoanResponse.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);

        factory.Clock.UtcNow = factory.Clock.UtcNow.AddMinutes(1);
        var atBoundary = await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans/overdue", JsonOptions);
        Assert.Empty(atBoundary!);

        factory.Clock.UtcNow = factory.Clock.UtcNow.AddSeconds(1);
        var afterBoundary = await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans/overdue", JsonOptions);
        Assert.Contains(afterBoundary!, loan => loan.Id == firstLoan!.Id);
        Assert.Contains(afterBoundary!, loan => loan.Id == secondLoan!.Id);

        var history = await client.GetFromJsonAsync<List<LoanResponse>>($"/api/readers/{firstReader.Id}/history", JsonOptions);
        Assert.Single(history!);
        Assert.Equal(firstLoan.Id, history[0].Id);

        var returnResponse = await client.PostAsync($"/api/loans/{firstLoan.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        var secondReturnResponse = await client.PostAsync($"/api/loans/{secondLoan!.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, secondReturnResponse.StatusCode);
        var afterReturn = await client.GetFromJsonAsync<List<LoanResponse>>("/api/loans/overdue", JsonOptions);
        Assert.Empty(afterReturn!);
    }

    [Fact]
    public async Task Reservations_EnforceOwnershipLifecycleAndConflicts()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();
        var (_, copy) = await CreateCatalogAsync(client);
        var ownerResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Власник" });
        var owner = await ownerResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);
        var otherResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Інший" });
        var other = await otherResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);

        var reservationResponse = await client.PostAsJsonAsync(
            "/api/reservations", new { readerId = owner!.Id, copyId = copy.Id });
        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);
        var reservation = await reservationResponse.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);

        var duplicateReservation = await client.PostAsJsonAsync(
            "/api/reservations", new { readerId = other!.Id, copyId = copy.Id });
        Assert.Equal(HttpStatusCode.Conflict, duplicateReservation.StatusCode);

        var foreignLoan = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = other.Id, copyId = copy.Id, dueAtUtc = factory.Clock.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.Conflict, foreignLoan.StatusCode);

        var ownerLoan = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = owner.Id, copyId = copy.Id, dueAtUtc = factory.Clock.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.Created, ownerLoan.StatusCode);
        var loan = await ownerLoan.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
        var fulfilled = await client.GetFromJsonAsync<ReservationResponse>(
            reservationResponse.Headers.Location, JsonOptions);
        Assert.Equal(ReservationStatus.Fulfilled, fulfilled!.Status);

        var duplicateLoan = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = owner.Id, copyId = copy.Id, dueAtUtc = factory.Clock.UtcNow.AddHours(2) });
        Assert.Equal(HttpStatusCode.Conflict, duplicateLoan.StatusCode);

        var cancelFulfilled = await client.PostAsJsonAsync(
            $"/api/reservations/{reservation!.Id}/cancel", new { readerId = owner.Id });
        Assert.Equal(HttpStatusCode.Conflict, cancelFulfilled.StatusCode);

        var returnResponse = await client.PostAsync($"/api/loans/{loan!.Id}/return", null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        var duplicateReturn = await client.PostAsync($"/api/loans/{loan.Id}/return", null);
        Assert.Equal(HttpStatusCode.Conflict, duplicateReturn.StatusCode);
    }

    [Fact]
    public async Task LoanDueDate_RejectsPastEqualAndNonUtcAsBadRequest()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();
        var (_, copy) = await CreateCatalogAsync(client);
        var readerResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Читач" });
        var reader = await readerResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);
        var request = new { readerId = reader!.Id, copyId = copy.Id };

        var past = await client.PostAsJsonAsync("/api/loans", new
        {
            request.readerId,
            request.copyId,
            dueAtUtc = factory.Clock.UtcNow.AddSeconds(-1)
        });
        var equal = await client.PostAsJsonAsync("/api/loans", new
        {
            request.readerId,
            request.copyId,
            dueAtUtc = factory.Clock.UtcNow
        });
        var nonUtc = await client.PostAsJsonAsync("/api/loans", new
        {
            readerId = reader.Id,
            copyId = copy.Id,
            dueAtUtc = factory.Clock.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss+02:00")
        });

        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, equal.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nonUtc.StatusCode);
    }

    [Fact]
    public async Task StaleContextReturnAfterReturnAndReissue_ThrowsConcurrencyAndPreservesNewLoan()
    {
        using var factory = new CirculationApiFactory();
        using var client = factory.CreateClient();
        var (_, copy) = await CreateCatalogAsync(client);
        var readerResponse = await client.PostAsJsonAsync("/api/readers", new { name = "Читач" });
        var reader = await readerResponse.Content.ReadFromJsonAsync<ReaderResponse>(JsonOptions);
        var issueResponse = await client.PostAsJsonAsync(
            "/api/loans",
            new { readerId = reader!.Id, copyId = copy.Id, dueAtUtc = factory.Clock.UtcNow.AddHours(1) });
        var firstLoan = await issueResponse.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);

        using var staleScope = factory.Services.CreateScope();
        var staleService = staleScope.ServiceProvider.GetRequiredService<CatalogService>();
        var staleLoan = await staleService.GetLoanAsync(firstLoan!.Id);
        Assert.NotNull(staleLoan);

        using (var currentScope = factory.Services.CreateScope())
        {
            var currentService = currentScope.ServiceProvider.GetRequiredService<CatalogService>();
            await currentService.ReturnLoanAsync(firstLoan.Id);
            var secondLoan = await currentService.IssueLoanAsync(reader.Id, copy.Id, factory.Clock.UtcNow.AddHours(2));
            Assert.Null(secondLoan.ReturnedAtUtc);
        }

        await Assert.ThrowsAsync<CirculationConcurrencyException>(() => staleService.ReturnLoanAsync(firstLoan.Id));

        using var verifyScope = factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var persistedCopy = await verifyRepository.GetBookCopyAsync(copy.Id);
        var loans = await verifyRepository.GetLoansForReaderAsync(reader.Id);
        Assert.Equal(BookCopyStatus.Loaned, persistedCopy!.Status);
        Assert.Single(loans.Where(item => item.ReturnedAtUtc is null));
    }

    private static async Task<(BookResponse Book, BookCopyResponse Copy)> CreateCatalogAsync(HttpClient client)
    {
        var authorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Автор" });
        var author = await authorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new { name = "Категорія" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        var bookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new { title = "Книга", categoryId = category!.Id, authorIds = new[] { author!.Id } });
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        Assert.NotNull(book);
        var copyResponse = await client.PostAsJsonAsync(
            $"/api/books/{book!.Id}/copies",
            new { inventoryNumber = "CIRC-001" });
        var copy = await copyResponse.Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions);
        Assert.NotNull(copy);
        return (book, copy!);
    }

    private sealed class CirculationApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public ControlledTimeProvider Clock { get; } = new(new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<LibraryDbContext>>();
                services.RemoveAll<DbContextOptions<LibraryDbContext>>();
                services.RemoveAll<LibraryDbContext>();
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(_connection);
                services.AddSingleton<TimeProvider>(Clock);
                services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(_connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    private sealed class ControlledTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }
}
