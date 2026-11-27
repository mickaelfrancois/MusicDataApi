namespace MusicData.Api.Endpoints;

internal static class Validation
{
    public const int MaxNameLength = 255;
    public const int MaxMbidLength = 36;

    public static IResult? RequireMaxLength(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
            return Results.BadRequest($"{fieldName} is required and must be at most {maxLength} characters.");

        return null;
    }

    /// <summary>
    /// MusicBrainz identifiers are canonical hyphenated GUIDs; anything else can
    /// never match and must not reach the cache or the external providers.
    /// </summary>
    public static IResult? RequireMbid(string? value, string fieldName)
    {
        if (value is null || value.Length != MaxMbidLength || !Guid.TryParseExact(value, "D", out _))
            return Results.BadRequest($"{fieldName} is required and must be a MusicBrainz identifier (GUID).");

        return null;
    }

    public static IResult? AllowMaxLength(string? value, int maxLength, string fieldName)
    {
        if (value is not null && value.Length > maxLength)
            return Results.BadRequest($"{fieldName} must be at most {maxLength} characters.");

        return null;
    }
}
