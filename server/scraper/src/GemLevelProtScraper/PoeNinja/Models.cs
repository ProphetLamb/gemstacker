using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeNinja;

internal sealed record PoeNinjaRoot(string GemPriceUrl);

internal sealed record PoeNinjaApiSparkLine(ImmutableArray<decimal?> Data, decimal TotalChange);

[BsonIgnoreExtraElements]
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

internal sealed record PoeNinjaApiGemPricesEnvelope(
    ImmutableArray<PoeNinjaApiGemPrice> Lines
);
