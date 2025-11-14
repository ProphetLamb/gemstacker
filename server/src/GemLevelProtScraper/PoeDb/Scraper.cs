using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using MongoDB.Migration;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeDb;

internal sealed class PoeDbScraper(IServiceScopeFactory serviceScopeFactory, ISystemClock clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var activePublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeDbActiveSkillList>>();
            var supportPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeDbSupportSkillList>>();
            var completionPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeDbListCompleted>>();
            await Task.WhenAll(
                activePublisher.PublishAsync(new(clock.UtcNow, "https://poedb.tw/us/Skill_Gems#SkillGemsGem"), stoppingToken).AsTask(),
                supportPublisher.PublishAsync(new(clock.UtcNow, "https://poedb.tw/us/Support_Gems#SupportGemsGem"), stoppingToken).AsTask()
            ).ConfigureAwait(false);
            await completionPublisher.PublishAsync(new(clock.UtcNow), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

internal sealed class PoeDbSkillNameSpider(IDataflowPublisher<PoeDbSkillName> activeSkillPublisher, IAngleSharpStaticPageLoader pageLoader, ILogger<PoeDbSkillSpider> logger) : IDataflowHandler<PoeDbActiveSkillList>, IDataflowHandler<PoeDbSupportSkillList>
{
    public async ValueTask HandleAsync(PoeDbActiveSkillList message, CancellationToken cancellationToken = default)
    {
        await ParseSkillListAsync(new(message.ActiveSkillUrl), "Skill Gems Gem", cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask HandleAsync(PoeDbSupportSkillList message, CancellationToken cancellationToken = default)
    {
        await ParseSkillListAsync(new(message.SupportSkillUrl), "Support Gems Gem", cancellationToken).ConfigureAwait(false);
    }
    private async ValueTask ParseSkillListAsync(Uri url, string headerText, CancellationToken cancellationToken)
    {
        var body = await pageLoader.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        var headers = body.QuerySelectorAll("div.card > h5.card-header");
        var card = headers
            .Where(h => PoeDbHtml.IsHeaderTextEqual(h, headerText))
            .SelectMany(h => h.NextElementSiblings(s => s.ClassList.Contains("card-body")))
            .FirstOrDefault();
        if (card is null)
        {
            logger.LogWarning("No card body found for {SkillGems}", url);
            return;
        }
        var items = card.QuerySelectorAll("a.gemitem")
            .Concat(card.QuerySelectorAll("a.gem_blue"))
            .Concat(card.QuerySelectorAll("a.gem_green"))
            .Concat(card.QuerySelectorAll("a.gem_red"))
            .OfType<IHtmlAnchorElement>()
            .SelectTruthy(PoeDbHtml.ParseSkillName);

        await activeSkillPublisher.PublishAllAsync(items, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed partial class PoeDbSkillSpider(IDataflowPublisher<PoeDbSkill> skillPublisher, IAngleSharpStaticPageLoader pageLoader, ILogger<PoeDbSkillSpider> logger) : IDataflowHandler<PoeDbSkillName>
{
    public async ValueTask HandleAsync(PoeDbSkillName message, CancellationToken cancellationToken = default)
    {
        var skillName = message.Id;
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
            logger.LogError(ex, "Failed to parse skill page {SkillName}", message.Id);
        }

        if (skill is { })
        {
            await skillPublisher.PublishAsync(skill, cancellationToken).ConfigureAwait(false);
        }

        PoeDbSkill ParseSkill()
        {
            var discriminator = body
                .QuerySelectorAll("ul.nav.nav-tabs a.nav-link > small")
                .SelectTruthy(ParseDiscriminator)
                .FirstOrDefault();

            var image = pane
                .QuerySelectorAll("div.itemboximage > img")
                .OfType<IHtmlImageElement>()
                .Select(img => img.Source)
                .FirstOrDefault();

            var headers = pane.QuerySelectorAll("div.card > h5.card-header");

            var qualities = PoeDbHtml.TryGetTableForHeader(headers, skillName, "Unusual Gems", logger) is { } qualitiesTable
                ? ParseQualitiesTable(qualitiesTable).ToImmutableArray()
                : [];

            var skillLevels = PoeDbHtml.TryGetTableForHeader(headers, skillName, "Level Effect", logger) is { } levelsTable
                ? ParseLevelsTable(levelsTable).ToImmutableArray()
                : [];

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
                image,
                discriminator,
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
                view["Level"].Single().SelectText(ParseDoubleCultured)
                .Zip(view["Requires Level"].Single().SelectText(ParseDoubleCultured))
                .Zip((view["Int"] | view["Intelligence"]).SingleOrDefault().SelectText(ParseDoubleCultured, null))
                .Zip((view["Dex"] | view["Dexterity"]).SingleOrDefault().SelectText(ParseDoubleCultured, null))
                .Zip((view["Str"] | view["Strength"]).SingleOrDefault().SelectText(ParseDoubleCultured, null))
                .Zip((view["Exp"] | view["Experience"]).SingleOrDefault().SelectText(ParseDoubleCultured, null))
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

        static double ParseDoubleCultured(string text) => double.Parse(text, CultureInfo.GetCultureInfo(1033));

        static IEnumerable<PoeDbSkillRelatedGem> ParseRelatedGemsTable(IHtmlTableElement relatedGemsTable)
        {
            var view = relatedGemsTable.ToView();
            foreach (var (name, description) in
                view["Name"].Single()
                .Zip(view["Show Full Descriptions"].Single().SelectText())
            )
            {
                if (name.QuerySelector("a.itemclass_gem") is IHtmlAnchorElement a && PoeDbHtml.ParseSkillName(a) is { } skillName)
                {
                    yield return new(skillName, description);
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
                ?? [];
            var references = titleView["Reference"].SingleOrDefault()?.First().Children.OfType<IHtmlAnchorElement>().Select(a => new PoeDbLink(a.TextContent, a.Href))
                ?? [];
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
                .SelectTruthy(PoeDbHtml.ParseSkillName)
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

    [GeneratedRegex(@"_(alt_\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MatchAltDiscriminatorRegex();
}

internal sealed class PoeDbCleanup(PoeDbRepository repository) : IDataflowHandler<PoeDbActiveSkillList>, IDataflowHandler<PoeDbListCompleted>
{
    private DateTime? _startTimestamp;

    public ValueTask HandleAsync(PoeDbActiveSkillList message, CancellationToken cancellationToken = default)
    {
        _startTimestamp ??= message.Timestamp.UtcDateTime;
        return default;
    }

    public async ValueTask HandleAsync(PoeDbListCompleted message, CancellationToken cancellationToken = default)
    {
        var endTs = message.Timestamp.UtcDateTime;
        if (_startTimestamp is not { } startTs || startTs >= endTs)
        {
            return;
        }
        _startTimestamp = null;

        _ = await repository.RemoveOlderThanAsync(startTs, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class PoeDbSink(PoeDbRepository repository) : IDataflowHandler<PoeDbSkill>
{
    public async ValueTask HandleAsync(PoeDbSkill newSkill, CancellationToken cancellationToken = default)
    {
        await repository.AddOrUpdateAsync(newSkill, cancellationToken).ConfigureAwait(false);
    }
}
