using Library.Core;

namespace Library.UnitTests;

public class BookCopyTests
{
    [Fact]
    public void CreateWithNullBook_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BookCopy(null!, "INV-001"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateWithEmptyOrWhitespaceInventoryNumber_ShouldThrowArgumentException(string inventoryNumber)
    {
        var book = CreateBook();

        Assert.Throws<ArgumentException>(() => new BookCopy(book, inventoryNumber));
    }

    [Fact]
    public void CreateWithValidData_ShouldStoreBookAndInventoryNumberAndAvailableStatus()
    {
        var book = CreateBook();

        var copy = new BookCopy(book, "INV-001");

        Assert.Same(book, copy.Book);
        Assert.Equal("INV-001", copy.InventoryNumber);
        Assert.Equal(BookCopyStatus.Available, copy.Status);
    }

    [Fact]
    public void TwoCopiesOfSameBook_ShouldHaveDifferentInventoryNumbers()
    {
        var book = CreateBook();

        var firstCopy = new BookCopy(book, "INV-001");
        var secondCopy = new BookCopy(book, "INV-002");

        Assert.NotEqual(firstCopy.InventoryNumber, secondCopy.InventoryNumber);
    }

    private static Book CreateBook()
    {
        var category = new Category("Драма");
        var author = new Author("Леся Українка");

        return new Book("Лісова пісня", category, new[] { author });
    }
}
