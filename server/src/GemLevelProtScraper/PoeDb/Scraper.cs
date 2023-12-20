using System.Collections.Immutable;
using System.Globalization;
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
            await rootPublisher.PublishAsync(new("https://poedb.tw/us/Gem#SkillGemsGem"), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

internal sealed class PoeDbSkillNameSpider(IDataflowPublisher<PoeDbSkillName> activeSkillPublisher, IAngleSharpStaticPageLoader pageLoader, ILogger<PoeDbSkillSpider> logger) : IDataflowHandler<PoeDbRoot>
{
    public async ValueTask HandleAsync(PoeDbRoot message, CancellationToken cancellationToken = default)
    {
        var body = await pageLoader.LoadAsync(new(message.ActiveSkillUrl), cancellationToken).ConfigureAwait(false);
        var headers = body.QuerySelectorAll("div.card > h5.card-header");
        var card = headers
            .Where(h => PoeDbHtml.IsHeaderTextEqual(h, "Skill Gems Gem"))
            .SelectMany(h => h.NextElementSiblings(s => s.ClassList.Contains("card-body")))
            .FirstOrDefault();
        if (card is null)
        {
            logger.LogWarning("No card body found for {SkillGems}", message.ActiveSkillUrl);
            return;
        }
        var items = card.QuerySelectorAll("a.gemitem")
            .Concat(card.QuerySelectorAll("a.gem_blue"))
            .Concat(card.QuerySelectorAll("a.gem_green"))
            .Concat(card.QuerySelectorAll("a.gem_red"))
            .OfType<IHtmlAnchorElement>()
            .Select(a => new PoeDbSkillName(a.Text, a.Href))
            .SelectTruthy(ParseSkillNameFromRelative);
        var tasks = items
            .Select(s => activeSkillPublisher.PublishAsync(s, cancellationToken))
            .SelectTruthy(t => t.IsCompletedSuccessfully ? null : t.AsTask());
        await Task.WhenAll(tasks).ConfigureAwait(false);


        static PoeDbSkillName? ParseSkillNameFromRelative(PoeDbSkillName absoluteSkill)
        {
            if (string.IsNullOrWhiteSpace(absoluteSkill.Name))
            {
                return null;
            }
            if (!Uri.TryCreate(absoluteSkill.RelativeUrl, UriKind.RelativeOrAbsolute, out var uri))
            {
                return null;
            }
            return absoluteSkill with { RelativeUrl = uri.PathAndQuery };
        }
    }
}

internal sealed partial class PoeDbSkillSpider(IDataflowPublisher<PoeDbSkill> skillPublisher, IAngleSharpStaticPageLoader pageLoader, ILogger<PoeDbSkillSpider> logger) : IDataflowHandler<PoeDbSkillName>
{
    public async ValueTask HandleAsync(PoeDbSkillName message, CancellationToken cancellationToken = default)
    {
        var skillName = message.Name;
        Url skillPage = new(message.RelativeUrl, "https://poedb.tw/");
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
            logger.LogError(ex, "Failed to parse skill page {SkillName}", message.Name);
        }

        if (skill is { })
        {
            await skillPublisher.PublishAsync(skill, cancellationToken).ConfigureAwait(false);
        }

        PoeDbSkill ParseSkill()
        {
            var image = pane
                .QuerySelectorAll("div.itemboximage > img")
                .OfType<IHtmlImageElement>()
                .Select(img => img.Source)
                .FirstOrDefault();

            var discriminator = pane
                .QuerySelectorAll("ul.nav.nav-tabs a.nav-link > small")
                .Select(ParseDiscriminator)
                .FirstOrDefault();

            var headers = pane.QuerySelectorAll("div.card > h5.card-header");

            var qualities = PoeDbHtml.TryGetTableForHeader(headers, skillName, "Unusual Gems", logger) is { } qualitiesTable
                ? ParseQualitiesTable(qualitiesTable).ToImmutableArray()
                : ImmutableArray<PoeDbGemQuality>.Empty;

            var skillLevels = PoeDbHtml.TryGetTableForHeader(headers, skillName, "Level Effect", logger) is { } levelsTable
                ? ParseLevelsTable(levelsTable).ToImmutableArray()
                : ImmutableArray<PoeDbSkillLevel>.Empty;

            var skillDescription = PoeDbHtml.TryGetHeader(headers, skillName, skillName) is { } header
                && PoeDbHtml.TryGetTableForHeader(headers, skillName, skillName) is { } descriptionTable
                ? new PoeDbSkillDescription(header.ParentElement!.Text(), ParseRelatedGemsTable(descriptionTable).ToImmutableArray())
                : null;

            var skillStats = PoeDbHtml.TryGetTableForHeader(headers, skillName, "Attribute", logger) is { } attributeTable
                ? ParseInfoTable(attributeTable)
                : pane.QuerySelector(":scope > table.table") is IHtmlTableElement infoTable
                ? ParseInfoTable(infoTable)
                : throw new InvalidOperationException("Missing skill stats table");

            var genus = PoeDbHtml.TryGetHeader(headers, skillName, "Genus", logger) is { } genusHeader
                ? ParseGenusList(genusHeader)
                : null;

            return new(
                message,
                discriminator,
                image,
                skillStats,
                skillDescription,
                qualities,
                skillLevels,
                genus
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
                .Zip((view["Int"] | view["Intelligence"]).SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip((view["Dex"] | view["Dexterity"]).SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip((view["Str"] | view["Strength"]).SingleOrDefault().SelectText(ParseDecimalCultured, null))
                .Zip((view["Exp"] | view["Experience"]).SingleOrDefault().SelectText(ParseDecimalCultured, null))
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
                view["Name"].Single()
                .Zip(view["Show Full Descriptions"].Single().SelectText())
            )
            {
                if (name.QuerySelector("a.itemclass_gem") is IHtmlAnchorElement a)
                {
                    yield return new(new(a.TextContent, a.Href), description);
                }
            }
        }

        static PoeDbSkillStats ParseInfoTable(IHtmlTableElement infoTable)
        {
            var titleView = infoTable.ToRowsView(0);
            var baseType = titleView.Match("BaseType*").Single().First().TextContent;
            var class_ = titleView.Match("Class*").Single().First().Children.OfType<IHtmlAnchorElement>().Select(a => new PoeDbLink(a.TextContent, a.Href)).Single();
            var metadata = titleView["ItemType"].Single().First().TextContent;
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

        static PoeDbGenus? ParseGenusList(IHtmlHeadingElement genusHeader)
        {
            if (genusHeader.NextElementSibling is not IHtmlDivElement genusBody || !genusBody.ClassList.Contains("card-body"))
            {
                return null;
            }

            var skills = genusBody
                .QuerySelectorAll(":scope > a")
                .OfType<IHtmlAnchorElement>()
                .Select(a => new PoeDbSkillName(a.TextContent, a.Href))
                .ToImmutableArray();
            return new(
                skills
            );
        }

        static string? ParseDiscriminator(IElement e)
        {
            var match = MatchAltDiscriminatorRegex().Match(e.TextContent);
            if (!match.Success || match.Groups.Count <= 1)
            {
                return null;
            }

            if (match.Groups[1] is not { Success: true } group)
            {
                return null;
            }
            return group.Value;
        }
    }

    [GeneratedRegex("_(alt_[xyz])")]
    private static partial Regex MatchAltDiscriminatorRegex();
}

internal sealed class PoeDbSink(PoeDbRepository repository) : IDataflowHandler<PoeDbSkill>
{
    public async ValueTask HandleAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(newSkill, cancellationToken).ConfigureAwait(false);
    }
}
