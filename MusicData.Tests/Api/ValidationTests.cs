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
