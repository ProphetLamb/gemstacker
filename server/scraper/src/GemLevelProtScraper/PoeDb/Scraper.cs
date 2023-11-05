using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using MongoDB.Driver;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeDb;

internal sealed class PoeDbScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeDbRoot>>();
            await rootPublisher.PublishAsync(new("https://poedb.tw/api/ActiveSkills"), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(4), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

internal sealed class PoeDbSkillNameSpider(IDataflowPublisher<PoeDbSkillName> activeSkillPublisher, IStaticPageLoader pageLoader) : IDataflowHandler<PoeDbRoot>
{
    public async ValueTask HandleAsync(PoeDbRoot message, CancellationToken cancellationToken = default)
    {
        var response = await pageLoader.LoadAsync(new(message.ActiveSkillUrl), cancellationToken).ConfigureAwait(false);
        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        var skills = await response.ReadFromJsonAsync<PoeDbActiveSkillsResponse>(jsonOptions, cancellationToken).ConfigureAwait(false);
        var tasks = skills?.Data
            .Where(skill => skill.Name is not null)
            .Select(skill => activeSkillPublisher.PublishAsync(skill))
            .Where(task => !task.IsCompletedSuccessfully)
            .Select(task => task.AsTask())
            .ToArray() ?? [];
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

internal sealed partial class PoeDbSkillSpider(IDataflowPublisher<PoeDbSkill> skillPublisher, IAngleSharpStaticPageLoader pageLoader, ILogger<PoeDbSkillSpider> logger) : IDataflowHandler<PoeDbSkillName>
{
    public async ValueTask HandleAsync(PoeDbSkillName message, CancellationToken cancellationToken = default)
    {
        var skillName = message.Name;
        var skillPage = CreatePoeDbSkillPageUrlByName(skillName);
        var body = await pageLoader.LoadAsync(skillPage, cancellationToken).ConfigureAwait(false);
        var pane = body.QuerySelector("div.tab-pane.fade.show.active");
        if (pane is null)
        {
            logger.LogWarning("Missing tab-pane in skill html body for {Skill}", skillName);
            return;
        }

        PoeDbSkill? skill = null;
        try
        {
            skill = ParseSkill();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse skill page");
        }

        if (skill is { })
        {
            await skillPublisher.PublishAsync(skill, cancellationToken).ConfigureAwait(false);
        }

        PoeDbSkill ParseSkill()
        {
            var headers = pane.QuerySelectorAll("div.card > h5.card-header");
            var qualities = TryGetTableForHeader(headers, skillName, "Unusual Gems", logger) is { } qualitiesTable
                ? ParseQualitiesTable(qualitiesTable).ToImmutableArray()
                : ImmutableArray<PoeDbGemQuality>.Empty;
            var skillLevels = TryGetTableForHeader(headers, skillName, "Level Effect", logger) is { } levelsTable
                ? ParseLevelsTable(levelsTable).ToImmutableArray()
                : ImmutableArray<PoeDbSkillLevel>.Empty;
            var skillDescription = TryGetHeader(headers, skillName, skillName) is { } header
                && TryGetTableForHeader(headers, skillName, skillName) is { } descriptionTable
                ? new PoeDbSkillDescription(header.ParentElement!.Text(), ParseRelatedGemsTable(descriptionTable).ToImmutableArray())
                : null;
            var skillStats = pane.QuerySelector(":scope > table.table") is IHtmlTableElement infoTable
                ? ParseInfoTable(infoTable)
                : throw new InvalidOperationException("Missing skill stats table");

            return new(
                new(skillName),
                skillStats,
                skillDescription,
                qualities,
                skillLevels
            );
        }

        static IEnumerable<PoeDbGemQuality> ParseQualitiesTable(IHtmlTableElement qualitiesTable)
        {
            var view = qualitiesTable.ToView();
            foreach (var type in view["Type"].Single().Select(cell => cell.TextContent))
            {
                yield return new(type);
            }
        }

        static IEnumerable<PoeDbSkillLevel> ParseLevelsTable(IHtmlTableElement levelEffectsTable)
        {
            var view = levelEffectsTable.ToView();
            foreach (var (((((level, requiresLevel), intelligence), dexterity), strength), experience) in
                view["Level"].Single().SelectText(ParseDecimalCultured)
                .Zip(view["Requires Level"].Single().SelectText(ParseDecimalCultured))
                .Zip(view["Intelligence"].SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip(view["Dexterity"].SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip(view["Strength"].SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip(view["Experience"].SingleOrDefault().SelectText(ParseDecimalCultured, null))
            )
            {
                yield return new(
                    level,
                    requiresLevel,
                    new(intelligence, dexterity, strength),
                    experience
                );
            }
        }

        static decimal ParseDecimalCultured(string text) => decimal.Parse(text, CultureInfo.GetCultureInfo(1033));

        static IEnumerable<PoeDbSkillRelatedGem> ParseRelatedGemsTable(IHtmlTableElement relatedGemsTable)
        {
            var view = relatedGemsTable.ToView();
            foreach (var (name, description) in
                view["Name"].Single().SelectText()
                .Zip(view["Show Full Descriptions"].Single().SelectText())
            )
            {
                yield return new(new(name), description);
            }
        }

        static PoeDbSkillStats ParseInfoTable(IHtmlTableElement infoTable)
        {
            var titleView = infoTable.ToRowsView(0);
            var baseType = titleView.Match("BaseType*").Single().First().TextContent;
            var class_ = titleView.Match("Class*").Single().First().Children.OfType<IHtmlAnchorElement>().Select(a => new PoeDbLink(a.TextContent, a.Href)).Single();
            var metadata = titleView["Metadata"].Single().First().TextContent;
            var acronyms = titleView["Acronym"].SingleOrDefault()?.First().Children.OfType<IHtmlAnchorElement>().Select(a => new PoeDbLink(a.TextContent, a.Href))
                ?? Enumerable.Empty<PoeDbLink>();
            var references = titleView["Reference"].SingleOrDefault()?.First().Children.OfType<IHtmlAnchorElement>().Select(a => new PoeDbLink(a.TextContent, a.Href))
                ?? Enumerable.Empty<PoeDbLink>();
            return new(
                baseType,
                class_,
                acronyms.ToImmutableArray(),
                metadata,
                references.ToImmutableArray()
            );
        }
    }

    private static IHtmlHeadingElement? TryGetHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
    {

        if (headers.OfType<IHtmlHeadingElement>().FirstOrDefault(header => IsHeaderTextEqual(header, headerName)) is not { } header)
        {
            logger?.LogWarning("Failed to find table for header: Missing card with card-header {Header} for {Skill}", headerName, skillName);
            return null;
        }
        return header;
    }

    private static IHtmlTableElement? TryGetTableForHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
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

    private static IHtmlTableElement GetTableForHeader(IHtmlCollection<IElement> headers, string skillName, string headerName, ILogger? logger = null)
    {
        return TryGetTableForHeader(headers, skillName, headerName, logger) ?? throw new InvalidOperationException("Failed to find table for header");
    }

    [GeneratedRegex(@"^\s*(.*?)\s*\/\s*\d+")]
    private static partial Regex GetHeaderTextValueRegex();

    private static bool IsHeaderTextEqual(IElement header, ReadOnlySpan<char> expectedText)
    {
        return GetHeaderTextValueRegex().Match(header.TextContent ?? "") is { Success: true } match
            && match.Groups[1].ValueSpan.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase);
    }

    private static Uri CreatePoeDbSkillPageUrlByName(string name)
    {
        var normalizedName = name.Replace(' ', '_');
        return new($"https://poedb.tw/us/{Uri.EscapeDataString(normalizedName)}");
    }
}

internal sealed class PoeDbSink(PoeDbRepository repository) : IDataflowHandler<PoeDbSkill>
{
    public async ValueTask HandleAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(newSkill, cancellationToken).ConfigureAwait(false);
    }
}
