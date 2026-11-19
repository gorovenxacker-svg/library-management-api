using Library.Core;

namespace Library.UnitTests;

public class BookTests
{
    [Fact]
    public void CreateWithEmptyTitle_ShouldThrowArgumentException()
    {
        var title = "";
        var category = new Category("Поезія");
        var authors = new[] { new Author("Тарас Шевченко") };

        Action action = () => new Book(title, category, authors);

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateWithWhitespaceTitle_ShouldThrowArgumentException(string title)
    {
        var category = new Category("Поезія");
        var authors = new[] { new Author("Тарас Шевченко") };

        Assert.Throws<ArgumentException>(() => new Book(title, category, authors));
    }

    [Fact]
    public void CreateWithTitle_ShouldStoreExactTitle()
    {
        var title = "Лісова пісня";
        var category = new Category("Драма");
        var author = new Author("Леся Українка");

        var book = new Book(title, category, new[] { author });

        Assert.Equal(title, book.Title);
    }

    [Fact]
    public void CreateWithNullCategory_ShouldThrowArgumentNullException()
    {
        var authors = new[] { new Author("Леся Українка") };

        Assert.Throws<ArgumentNullException>(() => new Book("Лісова пісня", null!, authors));
    }

    [Fact]
    public void CreateWithNullAuthors_ShouldThrowArgumentNullException()
    {
        var category = new Category("Драма");

        Assert.Throws<ArgumentNullException>(() => new Book("Лісова пісня", category, null!));
    }

    [Fact]
    public void CreateWithEmptyAuthors_ShouldThrowArgumentException()
    {
        var category = new Category("Драма");

        Assert.Throws<ArgumentException>(() => new Book("Лісова пісня", category, Array.Empty<Author>()));
    }

    [Fact]
    public void CreateWithNullAuthor_ShouldThrowArgumentException()
    {
        var category = new Category("Драма");
        var authors = new Author[] { null! };

        Assert.Throws<ArgumentException>(() => new Book("Лісова пісня", category, authors));
    }

    [Fact]
    public void Authors_ShouldBeReadOnly()
    {
        var category = new Category("Драма");
        var author = new Author("Леся Українка");
        var extraAuthor = new Author("Тарас Шевченко");
        var book = new Book("Лісова пісня", category, new[] { author });

        var authors = Assert.IsAssignableFrom<ICollection<Author>>(book.Authors);

        Assert.Throws<NotSupportedException>(() => authors.Add(extraAuthor));
        Assert.Single(book.Authors);
    }

    [Fact]
    public void CreateWithTwoAuthors_ShouldStoreBookData()
    {
        var title = "Лісова пісня";
        var category = new Category("Драма");
        var lesya = new Author("Леся Українка");
        var taras = new Author("Тарас Шевченко");

        var book = new Book(title, category, new[] { lesya, taras });

        Assert.Equal(title, book.Title);
        Assert.Same(category, book.Category);
        Assert.Equal(2, book.Authors.Count);
        Assert.Same(lesya, book.Authors.ElementAt(0));
        Assert.Same(taras, book.Authors.ElementAt(1));
    }
}
