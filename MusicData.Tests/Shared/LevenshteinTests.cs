using MusicData.Shared;

namespace MusicData.Tests.Shared;

public class LevenshteinTests
{
    [Fact]
    public void Similarity_IdenticalStrings_ReturnsOne()
    {
        Assert.Equal(1.0f, Levenshtein.Similarity("foo", "foo"));
    }

    [Fact]
    public void Similarity_BothEmpty_ReturnsOne()
    {
        Assert.Equal(1.0f, Levenshtein.Similarity("", ""));
    }

    [Fact]
    public void Similarity_OneEmpty_ReturnsZero()
    {
        Assert.Equal(0.0f, Levenshtein.Similarity("", "foo"));
        Assert.Equal(0.0f, Levenshtein.Similarity("foo", ""));
    }

    [Fact]
    public void Similarity_DifferentCase_IgnoredByDefault()
    {
        Assert.Equal(1.0f, Levenshtein.Similarity("Foo Fighters", "foo fighters"));
    }

    [Fact]
    public void Similarity_DifferentCase_RespectedWhenIgnoreCaseFalse()
    {
        // 12 chars total, 2 differ (F vs f, F vs f) -> 1 - 2/12 = 0.8333...
        float similarity = Levenshtein.Similarity("Foo Fighters", "foo fighters", ignoreCase: false);
        Assert.InRange(similarity, 0.83f, 0.84f);
    }

    [Fact]
    public void Similarity_OneEditAway_ReturnsExpectedRatio()
    {
        // "kitten" -> "sitten" is 1 edit; max length 6 -> 1 - 1/6 = 0.8333...
        float similarity = Levenshtein.Similarity("kitten", "sitten");
        Assert.InRange(similarity, 0.83f, 0.84f);
    }

    [Fact]
    public void Similarity_KittenSitting_KnownDistance()
    {
        // Classic example: distance = 3, max length = 7 -> 1 - 3/7 ~= 0.5714
        float similarity = Levenshtein.Similarity("kitten", "sitting");
        Assert.InRange(similarity, 0.57f, 0.58f);
    }
}
