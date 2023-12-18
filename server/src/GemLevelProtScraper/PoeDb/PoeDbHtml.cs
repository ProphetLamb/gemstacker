using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace GemLevelProtScraper.PoeDb;

public static partial class PoeDbHtml
{

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
        return GetHeaderTextValueRegex().Match(header.TextContent ?? "") is { Success: true } match
            && match.Groups[1].ValueSpan.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase);
    }
}
