using MusicData.Api.Endpoints;

namespace MusicData.Tests.Api;

public class ValidationTests
{
    [Fact]
    public void RequireMaxLength_NullOrEmpty_ReturnsBadRequest()
    {
        Assert.NotNull(Validation.RequireMaxLength(null, 10, "X"));
        Assert.NotNull(Validation.RequireMaxLength("", 10, "X"));
    }

    [Fact]
    public void RequireMaxLength_OverLimit_ReturnsBadRequest()
    {
        Assert.NotNull(Validation.RequireMaxLength(new string('a', 11), 10, "X"));
    }

    [Fact]
    public void RequireMaxLength_AtOrUnderLimit_ReturnsNull()
    {
        Assert.Null(Validation.RequireMaxLength("a", 10, "X"));
        Assert.Null(Validation.RequireMaxLength(new string('a', 10), 10, "X"));
    }

    [Theory]
    [InlineData("a8d77515-f7b7-4d71-9646-8446a3b2a341")]
    [InlineData("A8D77515-F7B7-4D71-9646-8446A3B2A341")]
    public void RequireMbid_Guid_ReturnsNull(string value)
    {
        Assert.Null(Validation.RequireMbid(value, "X"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mbid-foo")]
    [InlineData("a8d77515f7b74d7196468446a3b2a341")]
    [InlineData("{a8d77515-f7b7-4d71-9646-8446a3b2a341}")]
    [InlineData("a8d77515-f7b7-4d71-9646-8446a3b2a34z")]
    public void RequireMbid_NotAGuid_ReturnsBadRequest(string? value)
    {
        Assert.NotNull(Validation.RequireMbid(value, "X"));
    }

    [Fact]
    public void AllowMaxLength_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(Validation.AllowMaxLength(null, 10, "X"));
        Assert.Null(Validation.AllowMaxLength("", 10, "X"));
    }

    [Fact]
    public void AllowMaxLength_OverLimit_ReturnsBadRequest()
    {
        Assert.NotNull(Validation.AllowMaxLength(new string('a', 11), 10, "X"));
    }

    [Fact]
    public void AllowMaxLength_UnderLimit_ReturnsNull()
    {
        Assert.Null(Validation.AllowMaxLength("a", 10, "X"));
    }
}
