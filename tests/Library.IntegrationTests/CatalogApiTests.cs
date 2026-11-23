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

public sealed class CatalogApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CatalogResources_CanBeCreatedAndReadBack()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var authorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Леся Українка" });
        Assert.Equal(HttpStatusCode.Created, authorResponse.StatusCode);
        var author = await authorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        Assert.NotNull(author);

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new { name = "Драма" });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        Assert.NotNull(category);

        var bookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title = "Лісова пісня",
                categoryId = category!.Id,
                authorIds = new[] { author!.Id }
            });
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        Assert.NotNull(book);

        var copyResponse = await client.PostAsJsonAsync(
            $"/api/books/{book!.Id}/copies",
            new { inventoryNumber = "INV-001" });
        Assert.Equal(HttpStatusCode.Created, copyResponse.StatusCode);
        var copy = await copyResponse.Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions);
        Assert.NotNull(copy);

        Assert.NotNull(authorResponse.Headers.Location);
        Assert.NotNull(categoryResponse.Headers.Location);
        Assert.NotNull(bookResponse.Headers.Location);
        Assert.NotNull(copyResponse.Headers.Location);

        var readAuthorResponse = await client.GetAsync(authorResponse.Headers.Location);
        var readCategoryResponse = await client.GetAsync(categoryResponse.Headers.Location);
        var readBookResponse = await client.GetAsync(bookResponse.Headers.Location);
        var readCopyResponse = await client.GetAsync(copyResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, readAuthorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readCategoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readBookResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readCopyResponse.StatusCode);

        var readAuthor = await readAuthorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var readCategory = await readCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        var readBook = await readBookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        var readCopy = await readCopyResponse.Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions);

        Assert.Equal(author.Id, readAuthor!.Id);
        Assert.Equal("Леся Українка", readAuthor.Name);
        Assert.Equal(category.Id, readCategory!.Id);
        Assert.Equal("Драма", readCategory.Name);
        Assert.Equal(book.Id, readBook!.Id);
        Assert.Equal("Лісова пісня", readBook.Title);
        Assert.Equal(category.Id, readBook.Category.Id);
        Assert.Equal(author.Id, Assert.Single(readBook.Authors).Id);
        Assert.Equal(copy.Id, readCopy!.Id);
        Assert.Equal(book.Id, readCopy.BookId);
        Assert.Equal("INV-001", readCopy.InventoryNumber);
        Assert.Equal(BookCopyStatus.Available, readCopy.Status);
    }

    [Fact]
    public async Task InvalidInput_Returns400ProblemDetails()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/authors", new { name = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MissingBook_Returns404()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/books/{Guid.NewGuid()}/copies",
            new { inventoryNumber = "INV-001" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateInventory_Returns409()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var authorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Леся Українка" });
        var author = await authorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new { name = "Драма" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        var bookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new { title = "Лісова пісня", categoryId = category!.Id, authorIds = new[] { author!.Id } });
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/books/{book!.Id}/copies",
            new { inventoryNumber = "INV-001" });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/books/{book.Id}/copies",
            new { inventoryNumber = "INV-001" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task InventoryNumbers_AreCaseSensitive()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var authorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Леся Українка" });
        var author = await authorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new { name = "Драма" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        var bookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new { title = "Лісова пісня", categoryId = category!.Id, authorIds = new[] { author!.Id } });
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);

        var upperResponse = await client.PostAsJsonAsync(
            $"/api/books/{book!.Id}/copies",
            new { inventoryNumber = "INV-001" });
        var lowerResponse = await client.PostAsJsonAsync(
            $"/api/books/{book.Id}/copies",
            new { inventoryNumber = "inv-001" });

        Assert.Equal(HttpStatusCode.Created, upperResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, lowerResponse.StatusCode);
    }

    [Fact]
    public async Task SearchBooks_ReturnsFilteredBooksAndAvailableOnlyResults()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var firstAuthorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Леся Українка" });
        var firstAuthor = await firstAuthorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var secondAuthorResponse = await client.PostAsJsonAsync("/api/authors", new { name = "Оксана Лущевська" });
        var secondAuthor = await secondAuthorResponse.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new { name = "Фентезі" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);

        var matchingBookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title = "Українська історія",
                categoryId = category!.Id,
                authorIds = new[] { firstAuthor!.Id, secondAuthor!.Id }
            });
        var matchingBook = await matchingBookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        var otherBookResponse = await client.PostAsJsonAsync(
            "/api/books",
            new
            {
                title = "Інша історія",
                categoryId = category.Id,
                authorIds = new[] { firstAuthor.Id }
            });
        var otherBook = await otherBookResponse.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);

        var copyResponse = await client.PostAsJsonAsync(
            $"/api/books/{matchingBook!.Id}/copies",
            new { inventoryNumber = "INV-SEARCH-001" });
        Assert.Equal(HttpStatusCode.Created, copyResponse.StatusCode);

        var authorFilter = Uri.EscapeDataString("  ЛУЩЕВСЬКА ");
        var filteredResponse = await client.GetAsync(
            $"/api/books?title={Uri.EscapeDataString(" українська ")}&author={authorFilter}&category={Uri.EscapeDataString(" ФЕНТ ")}&availableOnly=true");
        var filteredBooks = await filteredResponse.Content.ReadFromJsonAsync<List<BookResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        Assert.Equal(matchingBook.Id, Assert.Single(filteredBooks!).Id);

        var noMatchResponse = await client.GetAsync("/api/books?title=відсутня");
        var noMatchBooks = await noMatchResponse.Content.ReadFromJsonAsync<List<BookResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, noMatchResponse.StatusCode);
        Assert.Empty(noMatchBooks!);
        Assert.NotEqual(matchingBook.Id, otherBook!.Id);
    }

    [Fact]
    public async Task EfRepository_DuplicateSave_ThrowsDuplicateInventoryNumberException()
    {
        using var factory = new TestApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var repository = new EfCatalogRepository(dbContext);

        var author = new Author("Леся Українка");
        var category = new Category("Драма");
        var book = new Book("Лісова пісня", category, new[] { author });

        await repository.AddAuthorAsync(author);
        await repository.AddCategoryAsync(category);
        await repository.AddBookAsync(book);
        await repository.SaveChangesAsync();

        await repository.AddBookCopyAsync(new BookCopy(book, "INV-001"));
        await repository.SaveChangesAsync();

        await repository.AddBookCopyAsync(new BookCopy(book, "INV-001"));

        await Assert.ThrowsAsync<DuplicateInventoryNumberException>(() => repository.SaveChangesAsync());
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<LibraryDbContext>>();
                services.RemoveAll<DbContextOptions<LibraryDbContext>>();
                services.RemoveAll<LibraryDbContext>();
                services.AddSingleton(_connection);
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
}
