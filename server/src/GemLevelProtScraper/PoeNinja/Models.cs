using System.Collections.Immutable;
using GemLevelProtScraper.Poe;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeNinja;

internal sealed record PoeNinjaList(LeaugeMode League, string GemPriceUrl);

internal sealed record PoeNinjaListCompleted(LeaugeMode League);

internal sealed record PoeNinjaApiSparkLine(ImmutableArray<decimal?> Data, decimal TotalChange);

internal sealed record PoeNinjaApiGemPrice(
    string Name,
    string Icon,
    PoeNinjaApiSparkLine SparkLine,
    PoeNinjaApiSparkLine LowConfidenceSparkLine,
    bool Corrupted,
    long GemLevel,
    long GemQuality,
    decimal ChaosValue,
    decimal DivineValue,
    long ListingCount
);

[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiLeaugeGemPrice(LeaugeMode Leauge, DateTime UtcTimestamp, PoeNinjaApiGemPrice Price);

internal sealed record PoeNinjaApiGemPricesEnvelope(
    ImmutableArray<PoeNinjaApiGemPrice> Lines
);
