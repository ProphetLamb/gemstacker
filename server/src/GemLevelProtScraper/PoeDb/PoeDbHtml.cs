using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace GemLevelProtScraper.PoeDb;

public static partial class PoeDbHtml
{
    internal static PoeDbSkillName? ParseSkillName(IHtmlAnchorElement a)
    {
        if (a.TextContent is not { } text || string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (!a.ClassList
            .SelectTruthy(c => TryParseColour(c, out var color) ? color : default(GemColor?))
            .TryGetFirst(out var color)
        )
        {
            return null;
        }

        if (NormalizeRelativeUrl(a.Href) is not { } uri || string.IsNullOrEmpty(uri))
        {
            return null;
        }

        return new(text, uri, color);
    }

    private static bool TryParseColour(string className, out GemColor color)
    {
        if (className.Equals("gem_red", StringComparison.OrdinalIgnoreCase))
        {
            color = GemColor.Red;
            return true;
        }
        if (className.Equals("gem_green", StringComparison.OrdinalIgnoreCase))
        {
            color = GemColor.Green;
            return true;
        }
        if (className.Equals("gem_blue", StringComparison.OrdinalIgnoreCase))
        {
            color = GemColor.Blue;
            return true;
        }
        if (className.Equals("gemitem", StringComparison.OrdinalIgnoreCase))
        {
            color = GemColor.White;
            return true;
        }

        color = default;
        return false;
    }

    private static string? NormalizeRelativeUrl(string absoluteUrl)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }
        return uri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.Unescaped);
    }

    public static IHtmlHeadingElement? TryGetHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
    {

        if (headers.OfType<IHtmlHeadingElement>().FirstOrDefault(header => IsHeaderTextEqual(header, headerName)) is not { } header)
        {
            logger?.LogWarning("Failed to find table for header: Missing card with card-header {Header} for {Skill}", headerName, skillName);
            return null;
        }
        return header;
    }

    public static IHtmlTableElement? TryGetTableForHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
    {
        if (TryGetHeader(headers, skillName, headerName, logger) is not { } header)
        {
            return null;
        }

        if (header.NextElementSiblings(sibling => sibling.ClassList.Contains("table-responsive")).FirstOrDefault() is not { } tableEnvelope)
        {
            logger?.LogWarning("Failed to find table for header: Missing table-responsive envelope sibling to the card-header {Header} for {Skill}", headerName, skillName);
            return null;
        }

        if (tableEnvelope.Children.OfType<IHtmlTableElement>().FirstOrDefault() is not { } table || !table.ClassList.Contains("table"))
        {
            logger?.LogWarning("Failed to find table for header: Missing table child in table-responsive envelope sibling to card-header {Header} for {Skill}", headerName, skillName);
            return null;
        }

        return table;
    }

    public static IHtmlTableElement GetTableForHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
    {
        return TryGetTableForHeader(headers, skillName, headerName, logger) ?? throw new InvalidOperationException("Failed to find table for header");
    }

    [GeneratedRegex(@"^\s*(.*?)\s*\/\s*\d+")]
    public static partial Regex GetHeaderTextValueRegex();

    public static bool IsHeaderTextEqual(IElement header, ReadOnlySpan<char> expectedText)
    {
        var input = header.TextContent ?? "";
        if (input.AsSpan().Equals(expectedText, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return GetHeaderTextValueRegex().Match(input) is { Success: true } match
            && match.Groups[1].ValueSpan.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase);
    }
}
