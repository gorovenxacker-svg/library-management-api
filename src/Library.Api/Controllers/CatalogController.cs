using Library.Api.Contracts;
using Library.Core;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogController(
    CatalogService catalogService,
    ICatalogRepository repository) : ControllerBase
{
    [HttpPost("authors")]
    public async Task<ActionResult<AuthorResponse>> AddAuthor(CreateAuthorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationError(nameof(request.Name), "Author name cannot be empty.");
        }

        try
        {
            var author = await catalogService.AddAuthorAsync(request.Name);
            return CreatedAtAction(nameof(GetAuthor), new { id = author.Id }, ToResponse(author));
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.ParamName ?? nameof(request.Name), exception.Message);
        }
    }

    [HttpGet("authors")]
    public async Task<ActionResult<IReadOnlyList<AuthorResponse>>> GetAuthors()
    {
        var authors = await repository.GetAuthorsAsync();
        return Ok(authors.Select(ToResponse).ToList());
    }

    [HttpGet("authors/{id:guid}")]
    public async Task<ActionResult<AuthorResponse>> GetAuthor(Guid id)
    {
        var author = await repository.GetAuthorAsync(id);
        return author is null ? NotFound() : Ok(ToResponse(author));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> AddCategory(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationError(nameof(request.Name), "Category name cannot be empty.");
        }

        try
        {
            var category = await catalogService.AddCategoryAsync(request.Name);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, ToResponse(category));
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.ParamName ?? nameof(request.Name), exception.Message);
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories()
    {
        var categories = await repository.GetCategoriesAsync();
        return Ok(categories.Select(ToResponse).ToList());
    }

    [HttpGet("categories/{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid id)
    {
        var category = await repository.GetCategoryAsync(id);
        return category is null ? NotFound() : Ok(ToResponse(category));
    }

    [HttpPost("books")]
    public async Task<ActionResult<BookResponse>> AddBook(CreateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ValidationError(nameof(request.Title), "Book title cannot be empty.");
        }

        if (request.AuthorIds is null || request.AuthorIds.Count == 0)
        {
            return ValidationError(nameof(request.AuthorIds), "At least one author is required.");
        }

        try
        {
            var book = await catalogService.AddBookAsync(request.Title, request.CategoryId, request.AuthorIds);
            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, ToResponse(book));
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.ParamName ?? nameof(request), exception.Message);
        }
    }

    [HttpGet("books")]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> GetBooks()
    {
        var books = await repository.GetBooksAsync();
        return Ok(books.Select(ToResponse).ToList());
    }

    [HttpGet("books/{id:guid}")]
    public async Task<ActionResult<BookResponse>> GetBook(Guid id)
    {
        var book = await repository.GetBookAsync(id);
        return book is null ? NotFound() : Ok(ToResponse(book));
    }

    [HttpPost("books/{bookId:guid}/copies")]
    public async Task<ActionResult<BookCopyResponse>> AddCopy(Guid bookId, CreateBookCopyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InventoryNumber))
        {
            return ValidationError(nameof(request.InventoryNumber), "Inventory number cannot be empty.");
        }

        try
        {
            var copy = await catalogService.AddCopyAsync(bookId, request.InventoryNumber);
            return CreatedAtAction(nameof(GetCopy), new { id = copy.Id }, ToResponse(copy));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DuplicateInventoryNumberException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate inventory number",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.ParamName ?? nameof(request.InventoryNumber), exception.Message);
        }
    }

    [HttpGet("copies")]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> GetCopies()
    {
        var copies = await repository.GetBookCopiesAsync();
        return Ok(copies.Select(ToResponse).ToList());
    }

    [HttpGet("copies/{id:guid}")]
    public async Task<ActionResult<BookCopyResponse>> GetCopy(Guid id)
    {
        var copy = await repository.GetBookCopyAsync(id);
        return copy is null ? NotFound() : Ok(ToResponse(copy));
    }

    private ActionResult ValidationError(string field, string detail)
    {
        ModelState.AddModelError(field, detail);
        return ValidationProblem(ModelState);
    }

    private static AuthorResponse ToResponse(Author author) => new(author.Id, author.Name);

    private static CategoryResponse ToResponse(Category category) => new(category.Id, category.Name);

    private static BookResponse ToResponse(Book book) => new(
        book.Id,
        book.Title,
        ToResponse(book.Category),
        book.Authors.Select(ToResponse).ToList());

    private static BookCopyResponse ToResponse(BookCopy copy) => new(
        copy.Id,
        copy.Book.Id,
        copy.InventoryNumber,
        copy.Status);
}
