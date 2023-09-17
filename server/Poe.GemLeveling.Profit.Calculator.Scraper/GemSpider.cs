using System.Collections.Immutable;
using DotnetSpider;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Parser;
using DotnetSpider.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotnetSpider.Selector;
using DotnetSpider.DataFlow.Storage;

namespace Poe.GemLeveling.Profit.Calculator.Scraper;

public sealed class PoedbOptions : IOptions<PoedbOptions>
{
    public string PoedbApiUrl { get; set; } = "https://poedb.tw/";
    public string PoedbItemUrlRegex { get; set; } = @"https://poedb\.tw/us/[\w\d_]+";

    PoedbOptions IOptions<PoedbOptions>.Value => this;
}

public enum GemQualityType
{
    None,
    Superior,
    Anomalous,
    Divergent,
    Phantasmal
}

public readonly record struct GemQuality(GemQualityType Type, string Stats, long Weight);

public readonly record struct GemLevel(long Level, long RequiredLevel, long Experience, long Intelligence, long Dexterity, long Strength);

public sealed record GemLevelEffects(ImmutableArray<GemLevel> Levels);

public sealed record GemQualityEffects(ImmutableArray<GemQuality> Quality);

[Schema("poe-gemleveling-profit-calculator", "gem-details")]
public sealed class GemDetails : EntityBase<GemDetails>
{
    public GemDetails()
    {
    }

    public GemDetails(GemDescriptor descriptor, GemQualityEffects qualityEffects, GemLevelEffects levelEffects)
    {
        Descriptor = descriptor;
        LevelEffects = levelEffects;
        QualityEffects = qualityEffects;
    }

    public GemDescriptor? Descriptor { get; set; }
    public GemQualityEffects? QualityEffects { get; set; }
    public GemLevelEffects? LevelEffects { get; set; }
}

public sealed class GemSpider : Spider
{
    private readonly GemDescriptorService _gemDescriptorService;
    private readonly PoedbOptions _poedbOptions;

    public GemSpider(
        IOptions<SpiderOptions> spiderOptions,
        DependenceServices services,
        ILogger<Spider> logger,
        GemDescriptorService gemDescriptorService,
        IOptions<PoedbOptions> poedbOptions
        ) : base(spiderOptions, services, logger)
    {
        _gemDescriptorService = gemDescriptorService;
        _poedbOptions = poedbOptions.Value;
    }

    protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
    {
        var gems = await _gemDescriptorService.GetDescriptors(stoppingToken);
        var requests = gems.Descriptors.Select(gem => new Request(
            "GET",
            new Uri(gem.Key),
            new() { [nameof(GemDescriptor)] = gem.Value }
        )).ToArray();
        AddDataFlow(new GemDetailsDataParser(_poedbOptions));
        await AddRequestsAsync(requests);
    }

    private sealed class GemDetailsDataParser : DataParser
    {
        private PoedbOptions _options;

        public GemDetailsDataParser(IOptions<PoedbOptions> options)
        {
            _options = options.Value;
        }

        public override Task InitializeAsync()
        {
            AddRequiredValidator(_options.PoedbItemUrlRegex);
            return Task.CompletedTask;
        }

        protected override Task ParseAsync(DataFlowContext context)
        {
            var url = context.Request.RequestUri;
            var cardHeaders = context.Selectable.XPath(@".//div[@class='tab-pane']/div[@class='card']/h5[@class='card-header']").Nodes();
            var qualityEffectsHeader = cardHeaders.FirstOrDefault(header => header.XPath(@".//text()").Value.AsSpan().Trim().StartsWith("Unusual Gems"));
            var qualityEffectsTable = qualityEffectsHeader?.XPath(@"./following-sibling::div[@class='table-responsive']/table");
            var gemQualityEffects = qualityEffectsTable is null ? null : ParseGemQualityEffects(qualityEffectsTable, url);

            var levelEffectsHeader = cardHeaders.FirstOrDefault(header => header.XPath(@".//text()").Value.AsSpan().Trim().StartsWith("Level Efect"));
            var levelEffectsTable = levelEffectsHeader?.XPath(@"./following-sibling::div[@class='table-responsive']/table");
            var gemLevelEffects = levelEffectsTable is null ? null : ParseGemLevelEffects(levelEffectsTable, url);

            if (context.Request.Properties?[nameof(GemDescriptor)] is not GemDescriptor descriptor)
            {
                Logger.LogWarning("GemDescriptor is null");
                return Task.CompletedTask;
            }
            if (gemLevelEffects is null)
            {
                Logger.LogWarning("Gem {Gem} has no level effects: {Url}", descriptor.Name, url);
                return Task.CompletedTask;
            }

            var gemDetails = new GemDetails(
                descriptor,
                gemQualityEffects ?? new(ImmutableArray<GemQuality>.Empty),
                gemLevelEffects ?? new(ImmutableArray<GemLevel>.Empty)
            );

            return Task.CompletedTask;
        }

        private GemQualityEffects? ParseGemQualityEffects(ISelectable qualityEffectsTable, Uri url)
        {
            var qualityEffects = ImmutableArray.CreateBuilder<GemQuality>();
            foreach (var tr in GetTableEntries(qualityEffectsTable))
            {
                var type = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Type")).Value;
                if (!Enum.TryParse<GemQualityType>(type, out var typeValue))
                {
                    Logger.LogWarning("GemQualityEffects table row type {Type} is invalid: {Url}", type, url);
                    continue;
                }
                var weight = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Weight")).Value;
                if (!long.TryParse(weight, out var weightValue))
                {
                    Logger.LogWarning("GemQualityEffects table row weight {Weight} is invalid: {Url}", weight, url);
                    continue;
                }
                var stats = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Stats")).Value;

                qualityEffects.Add(new(typeValue, stats, weightValue));
            }
            return new(qualityEffects.ToImmutable());
        }

        private GemLevelEffects? ParseGemLevelEffects(ISelectable gemLevelEffectsTable, Uri uri)
        {
            var levelEffects = ImmutableArray.CreateBuilder<GemLevel>();
            foreach (var tr in GetTableEntries(gemLevelEffectsTable))
            {
                var level = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Level")).Value;
                if (!long.TryParse(level, out var levelValue))
                {
                    Logger.LogWarning("GemLevelEffects table row level {Level} is invalid: {Url}", level, uri);
                    continue;
                }
                var requiredLevel = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Required Level")).Value;
                long.TryParse(requiredLevel, out var requiredLevelValue);
                var experience = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Experience")).Value;
                long.TryParse(experience, out var experienceValue);
                var intelligence = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Intelligence")).Value;
                long.TryParse(intelligence, out var intelligenceValue);
                var dexterity = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Dexterity")).Value;
                long.TryParse(dexterity, out var dexterityValue);
                var strength = tr.FirstOrDefault(t => t.Header.AsSpan().Trim().StartsWith("Strength")).Value;
                long.TryParse(strength, out var strengthValue);
                levelEffects.Add(new(levelValue, requiredLevelValue, experienceValue, intelligenceValue, dexterityValue, strengthValue));
            }
            return new(levelEffects.ToImmutable());
        }

        private IEnumerable<ImmutableArray<(string Header, string Value)>> GetTableEntries(ISelectable selector)
        {
            var th = selector.XPath("./thead/tr/th//text()").Nodes().Select(v => v.Value).ToImmutableArray();
            foreach (var tr in selector.XPath("./tbody/tr").Nodes())
            {
                yield return GetRowEntries(th, tr);
            }

            static ImmutableArray<(string Header, string Value)> GetRowEntries(ImmutableArray<string> th, ISelectable tr)
            {
                IEnumerable<ISelectable> td = tr.XPath("./td//text()").Nodes();
                var builder = ImmutableArray.CreateBuilder<(string, string)>(td.TryGetNonEnumeratedCount(out int count) ? count : 0);
                foreach (var (header, value) in th.Zip(td, (h, v) => (h, v.Value)))
                {
                    builder.Add((header, value));
                }
                return builder.Capacity == builder.Count
                    ? builder.MoveToImmutable()
                    : builder.ToImmutable();
            }
        }
    }
}