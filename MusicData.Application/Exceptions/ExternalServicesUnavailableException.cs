namespace MusicData.Application.Exceptions;

/// <summary>
/// Raised when external providers failed (timeout, 5xx, network) and none
/// produced a result, so "not found" cannot be concluded. Must not be cached.
/// </summary>
public sealed class ExternalServicesUnavailableException(string entityKind)
    : Exception($"External services failed to answer for {entityKind}.")
{
}
