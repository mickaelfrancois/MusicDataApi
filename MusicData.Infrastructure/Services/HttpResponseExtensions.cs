namespace MusicData.Infrastructure.Services;

internal static class HttpResponseExtensions
{
    /// <summary>
    /// Returns true for a 404 (the provider genuinely has nothing for this
    /// key) and throws for any other non-success status, so a transient
    /// provider failure is never mistaken for "not found" and cached.
    /// </summary>
    public static bool IsNotFoundOrThrow(this HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return true;

        response.EnsureSuccessStatusCode();
        return false;
    }
}
