using System.Collections.Immutable;
using GemLevelProtScraper.Poe;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeNinja;

internal sealed record PoeNinjaList(DateTimeOffset Timestamp, PoeLeague League, string ApiUrl);

internal sealed record PoeNinjaListCompleted(DateTimeOffset Timestamp, PoeLeague League);

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
internal sealed record PoeNinjaApiLeagueGemPrice(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiGemPrice Price);

internal sealed record PoeNinjaApiGemPricesEnvelope(
    ImmutableArray<PoeNinjaApiGemPrice> Lines
);
