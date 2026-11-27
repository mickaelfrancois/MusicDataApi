using MusicData.Shared;

namespace MusicData.Tests.Shared;

public class NameAliasesTests
{
    [Theory]
    [InlineData("  The   BEATLES ", "the beatles")]
    [InlineData("the\tbeatles\n", "the beatles")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_Trims_LowerCases_AndCollapsesWhitespace(string? input, string expected)
    {
        Assert.Equal(expected, NameAliases.Normalize(input));
    }

    [Fact]
    public void ForQuery_ReturnsNull_ForCanonicalEmptyOrTooLongNames()
    {
        Assert.Null(NameAliases.ForQuery(" the  beatles", "The Beatles"));
        Assert.Null(NameAliases.ForQuery("  ", "The Beatles"));
        Assert.Null(NameAliases.ForQuery(new string('a', NameAliases.MaxLength + 1), "The Beatles"));
        Assert.Equal("beatles", NameAliases.ForQuery(" Beatles ", "The Beatles"));
        Assert.NotNull(NameAliases.ForQuery(new string('a', NameAliases.MaxLength), "The Beatles"));
    }

    [Fact]
    public void Merge_Deduplicates_DropsInvalid_AndKeepsTheMostRecent()
    {
        IEnumerable<string> existing = Enumerable.Range(0, 30).Select(i => $"old {i}");
        string[] incoming = ["OLD  3", new string('x', 300), "", "new"];

        List<string> merged = NameAliases.Merge(existing, incoming);

        Assert.Equal(NameAliases.MaxCount, merged.Count);
        Assert.Equal(["old 3", "new"], merged.TakeLast(2));
        Assert.DoesNotContain("old 0", merged);
        Assert.Equal(merged.Count, merged.Distinct().Count());
        Assert.All(merged, a => Assert.InRange(a.Length, 1, NameAliases.MaxLength));
    }
}
