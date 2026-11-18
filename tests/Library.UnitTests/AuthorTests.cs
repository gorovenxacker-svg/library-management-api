using Library.Core;

namespace Library.UnitTests;

public class AuthorTests
{
    [Fact]
    public void CreateWithEmptyName_ShouldThrowArgumentException()
    {
        var name = "";

        Action action = () => new Author(name);

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateWithWhitespaceName_ShouldThrowArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new Author(name));
    }

    [Fact]
    public void CreateWithName_ShouldStoreExactName()
    {
        var name = "Леся Українка";

        var author = new Author(name);

        Assert.Equal(name, author.Name);
    }
}
