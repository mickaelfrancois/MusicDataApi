namespace MusicData.Shared;

/// <summary>
/// Rules for the queried-name aliases stored on cached artists/albums. They
/// are client-driven, so they are normalized (trivial variants collapse) and
/// bounded in length and number (a row cannot be inflated without limit).
/// </summary>
public static class NameAliases
{
    public const int MaxCount = 20;
    public const int MaxLength = 255;

    /// <summary>Trimmed, lower-cased, inner whitespace collapsed to one space.</summary>
    public static string Normalize(string? name) =>
        string.Join(' ', (name ?? string.Empty).ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The alias to store for <paramref name="queriedName"/>, or null when it
    /// is just the canonical name, empty or too long to be worth keeping.
    /// </summary>
    public static string? ForQuery(string? queriedName, string? canonicalName)
    {
        string alias = Normalize(queriedName);

        return alias.Length == 0 || alias.Length > MaxLength || alias == Normalize(canonicalName)
            ? null
            : alias;
    }

    /// <summary>
    /// <paramref name="existing"/> followed by <paramref name="incoming"/>
    /// (most recent last), de-duplicated, keeping only the
    /// <see cref="MaxCount"/> most recent valid aliases.
    /// </summary>
    public static List<string> Merge(IEnumerable<string>? existing, IEnumerable<string>? incoming)
    {
        List<string> merged = [];

        foreach (string alias in (existing ?? []).Concat(incoming ?? []).Select(Normalize))
        {
            if (alias.Length == 0 || alias.Length > MaxLength)
                continue;

            merged.Remove(alias);
            merged.Add(alias);
        }

        return merged.Count > MaxCount ? merged.GetRange(merged.Count - MaxCount, MaxCount) : merged;
    }
}
