using MusicData.Shared;

namespace MusicData.Tests.Shared;

public class ExtensionsTests
{
    [Fact]
    public void Quote_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal("", "".Quote());
    }

    [Fact]
    public void Quote_NoSpace_ReturnsUnchanged()
    {
        Assert.Equal("foo", "foo".Quote());
    }

    [Fact]
    public void Quote_WithSpace_WrapsInDoubleQuotes()
    {
        Assert.Equal("\"foo bar\"", "foo bar".Quote());
    }

    [Fact]
    public void ToShortDate_TooShort_ReturnsPlaceholder()
    {
        Assert.Equal("----", "abc".ToShortDate());
        Assert.Equal("----", "".ToShortDate());
    }

    [Fact]
    public void ToShortDate_FourOrMoreChars_ReturnsFirstFour()
    {
        Assert.Equal("2026", "2026-05-01".ToShortDate());
        Assert.Equal("1999", "1999".ToShortDate());
    }
}
