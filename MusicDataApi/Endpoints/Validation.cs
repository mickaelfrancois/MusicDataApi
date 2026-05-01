namespace MusicData.Api.Endpoints;

internal static class Validation
{
    public static IResult? RequireMaxLength(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
            return Results.BadRequest($"{fieldName} is required and must be less than {maxLength} characters.");

        return null;
    }

    public static IResult? AllowMaxLength(string? value, int maxLength, string fieldName)
    {
        if (value is not null && value.Length > maxLength)
            return Results.BadRequest($"{fieldName} must be less than {maxLength} characters.");

        return null;
    }
}
