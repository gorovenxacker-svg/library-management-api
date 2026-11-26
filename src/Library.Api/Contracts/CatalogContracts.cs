using System.ComponentModel.DataAnnotations;
using Library.Core;

namespace Library.Api.Contracts;

public sealed class CreateAuthorRequest
{
    [Required]
    public string? Name { get; init; }
}

public sealed class CreateCategoryRequest
{
    [Required]
    public string? Name { get; init; }
}

public sealed class CreateBookRequest
{
    [Required]
    public string? Title { get; init; }

    public Guid CategoryId { get; init; }

    [Required]
    public IReadOnlyCollection<Guid>? AuthorIds { get; init; }
}

public sealed class CreateBookCopyRequest
{
    [Required]
    public string? InventoryNumber { get; init; }
}

public sealed record AuthorResponse(Guid Id, string Name);

public sealed record CategoryResponse(Guid Id, string Name);

public sealed record BookResponse(
    Guid Id,
    string Title,
    CategoryResponse Category,
    IReadOnlyCollection<AuthorResponse> Authors);

public sealed record BookCopyResponse(
    Guid Id,
    Guid BookId,
    string InventoryNumber,
    BookCopyStatus Status);
