namespace MusicData.Shared;

public static class Extensions
{
    /// <summary>
    /// Turns a user string into a Lucene phrase: always wrapped in double
    /// quotes, with backslash and quote escaped. Quoting only when the string
    /// contained a space left single words like "AC/DC" (regex syntax) or
    /// "foo~" interpreted as operators, and a stray quote broke the query.
    /// </summary>
    public static string Quote(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "";
        }

        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static string ToShortDate(this string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 4)
        {
            return "----";
        }

        return s.Substring(0, 4);
    }
}