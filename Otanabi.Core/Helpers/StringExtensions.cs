using System.Text;
using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string SubstringBetween(this string str, string start, string end)
    {
        var startIndex = str.IndexOf(start);
        if (startIndex == -1)
            return "";

        startIndex += start.Length;

        var endIndex = str.IndexOf(end, startIndex);
        if (endIndex == -1)
            return "";

        return str.Substring(startIndex, endIndex - startIndex);
    }

    public static int? ToIntOrNull(this string? value) => int.TryParse(value, out var i) ? i : null;

    public static int? ToIntOrNull(this string? value, int radix)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var length = value?.Length;
        if (length == 0)
            return null;

        return Convert.ToByte(value, radix);
    }

    public static string Reverse(this string value)
    {
        var charArray = value.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public static string SubstringAfter(this string str, string delimiter)
    {
        if (str is null || delimiter is null)
            return str;

        var index = str.IndexOf(delimiter);
        return index < 0 ? str : str[(index + delimiter.Length)..];
    }

    public static string SubstringBefore(this string str, string delimiter)
    {
        if (str is null || string.IsNullOrEmpty(delimiter))
            return str;

        var index = str.IndexOf(delimiter);
        return index < 0 ? str : str[..index];
    }

    public static string DecodeBase64(this string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    public static byte[] DecodeBase64ToBytes(this string value) => Convert.FromBase64String(value);

    private static readonly Regex _whitespace = new(@"\s+");

    public static string ReplaceWhitespaces(this string input, string replacement) => _whitespace.Replace(input, replacement);

    public static string RemoveWhitespaces(this string input) => input.ReplaceWhitespaces(string.Empty);

    public static string RemovePrefix(this string input, string prefix)
    {
        if (input.StartsWith(prefix))
            return input.Remove(0, prefix.Length);

        return input;
    }

    public static string RemovePrefix(this string input, int prefixLen)
    {
        if (input.Length < prefixLen)
            return string.Empty;

        return input.Remove(0, prefixLen);
    }

    public static string NormalizeSTR(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remove special characters (keep letters, numbers, and spaces)
        var cleaned = Regex.Replace(input, @"[^\w\s]", ""); // \w = [a-zA-Z0-9_], \s = whitespace
        return cleaned.ToLowerInvariant();
    }

    public static string TrimAll(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var cleaned = input.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
        return cleaned.Trim();
    }
}
