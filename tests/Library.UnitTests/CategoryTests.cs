using Library.Core;

namespace Library.UnitTests;

public class CategoryTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateWithEmptyOrWhitespaceName_ShouldThrowArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new Category(name));
    }
}
