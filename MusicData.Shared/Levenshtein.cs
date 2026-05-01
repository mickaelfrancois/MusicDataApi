namespace MusicData.Shared;

public static class Levenshtein
{
    /// <summary>
    /// Compute the similarity of two strings using the Levenshtein distance.
    /// </summary>
    /// <param name="s">The first string.</param>
    /// <param name="t">The second string.</param>
    /// <param name="ignoreCase">If true (default), strings are lowercased before comparing.</param>
    /// <returns>A floating point value between 0.0 and 1.0.</returns>
    public static float Similarity(string s, string t, bool ignoreCase = true)
    {
        int maxLen = Math.Max(s.Length, t.Length);
        if (maxLen == 0)
            return 1.0f;

        if (ignoreCase)
        {
            s = s.ToLowerInvariant();
            t = t.ToLowerInvariant();
        }

        return 1.0f - (Distance(s, t) / (float)maxLen);
    }

    private static int Distance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = t[j - 1] == s[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
