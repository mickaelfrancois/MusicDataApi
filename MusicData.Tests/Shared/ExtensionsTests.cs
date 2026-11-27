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
    public void Quote_SingleWord_IsStillQuoted_SoLuceneOperatorsStayLiteral()
    {
        Assert.Equal("\"foo\"", "foo".Quote());
        Assert.Equal("\"AC/DC\"", "AC/DC".Quote());
    }

    [Fact]
    public void Quote_EscapesQuotesAndBackslashes()
    {
        Assert.Equal("\"say \\\"hi\\\" \\\\ ok\"", "say \"hi\" \\ ok".Quote());
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
