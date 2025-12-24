using System.Collections;
using System.Reflection;
using System.Text;

namespace MusicData.Infrastructure.Utilities;

internal static class ObjectDumper
{
    public static void DumpToConsole(object? obj, string? name = null, int maxDepth = 5, int indentSize = 2)
    {
        HashSet<object> visited = new(new ReferenceEqualityComparer());
        DumpInternal(obj, name ?? "root", 0, maxDepth, indentSize, visited, Console.Out);
    }

    public static void DumpToConsole<T>(this T? obj, string? name = null, int maxDepth = 5, int indentSize = 2)
        => DumpToConsole(obj as object, name, maxDepth, indentSize);

    public static void DumpToFile(object? obj, string filePath, bool append = false, string? name = null, int maxDepth = 5, int indentSize = 2)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath must be a non-empty string.", nameof(filePath));

        string? dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using StreamWriter writer = new(filePath, append, Encoding.UTF8);
        HashSet<object> visited = new(new ReferenceEqualityComparer());
        DumpInternal(obj, name ?? "root", 0, maxDepth, indentSize, visited, writer);
    }

    public static void DumpToFile<T>(this T? obj, string filePath, bool append = false, string? name = null, int maxDepth = 5, int indentSize = 2)
        => DumpToFile(obj as object, filePath, append, name, maxDepth, indentSize);

    private static void DumpInternal(object? obj, string name, int depth, int maxDepth, int indentSize, HashSet<object> visited, TextWriter writer)
    {
        string indent = new(' ', depth * indentSize);

        if (obj is null)
        {
            writer.WriteLine("{0}{1}: null", indent, name);
            return;
        }

        Type type = obj.GetType();

        if (IsSimple(type))
        {
            writer.WriteLine("{0}{1}: {2} ({3})", indent, name, obj, type.Name);
            return;
        }

        if (depth >= maxDepth)
        {
            writer.WriteLine("{0}{1}: {2} (Max depth reached)", indent, name, type.Name);
            return;
        }

        if (!visited.Add(obj))
        {
            writer.WriteLine("{0}{1}: {2} (Already visited)", indent, name, type.Name);
            return;
        }

        if (obj is IEnumerable enumerable && !(obj is string))
        {
            writer.WriteLine("{0}{1}: {2} (IEnumerable)", indent, name, type.Name);
            int i = 0;
            foreach (object? item in enumerable)
            {
                DumpInternal(item, $"[{i}]", depth + 1, maxDepth, indentSize, visited, writer);
                i++;
            }
            return;
        }

        writer.WriteLine("{0}{1}: {2}", indent, name, type.Name);

        // Dump public readable properties
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.GetIndexParameters().Length > 0)
                continue; // skip indexers

            if (!prop.CanRead)
                continue;

            object? value;
            try
            {
                value = prop.GetValue(obj);
            }
            catch (TargetInvocationException)
            {
                writer.WriteLine("{0}{1}{2}: <error invoking>", indent, new string(' ', indentSize), prop.Name);
                continue;
            }
            catch
            {
                writer.WriteLine("{0}{1}{2}: <error>", indent, new string(' ', indentSize), prop.Name);
                continue;
            }

            DumpInternal(value, prop.Name, depth + 1, maxDepth, indentSize, visited, writer);
        }

        // Optionally dump fields (uncomment if needed)
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            object? value;
            try
            {
                value = field.GetValue(obj);
            }
            catch
            {
                writer.WriteLine("{0}{1}{2}: <error>", indent, new string(' ', indentSize), field.Name);
                continue;
            }

            DumpInternal(value, field.Name, depth + 1, maxDepth, indentSize, visited, writer);
        }
    }

    private static bool IsSimple(Type type)
    {
        if (type.IsPrimitive || type.IsEnum)
            return true;

        if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) ||
            type == typeof(Uri))
            return true;

        // Nullable<T> where T is simple
        if (Nullable.GetUnderlyingType(type) is Type underlying)
            return IsSimple(underlying);

        return false;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}