using System;

namespace GemLevelProtScraper;

public static class StringExtensions
{
    public static bool ContainsGlobChars(this ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (IsGlobChar(c))
            {
                return true;
            }
        }
        return false;

        static bool IsGlobChar(char c) => c is '*' or '?' or '!' or '[' or ']';
    }

    public static bool ContainsGlobChars(this string text)
    {
        return ContainsGlobChars(text.AsSpan());
    }
}
