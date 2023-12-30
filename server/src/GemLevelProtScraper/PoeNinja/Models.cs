using System.Collections.Immutable;
using GemLevelProtScraper.Poe;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeNinja;

internal sealed record PoeNinjaList(DateTimeOffset Timestamp, PoeLeague League, string ApiUrl);

internal sealed record PoeNinjaListCompleted(DateTimeOffset Timestamp, PoeLeague League);

internal sealed record PoeNinjaApiSparkLine(ImmutableArray<double?> Data, double TotalChange);

internal sealed record PoeNinjaApiGemPrice(
    string Name,
    string Icon,
    PoeNinjaApiSparkLine SparkLine,
    PoeNinjaApiSparkLine LowConfidenceSparkLine,
    bool Corrupted,
    long GemLevel,
    long GemQuality,
    double ChaosValue,
    double DivineValue,
    long ListingCount
);


[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiGemPriceEnvalope(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiGemPrice Price);

internal sealed record PoeNinjaApiGemResponse(
    ImmutableArray<PoeNinjaApiGemPrice> Lines
);

internal sealed record PoeNinjaApiCurrencyPrice(
    string CurrencyTypeName,
    PoeNinjaApiCurrencyTrade Pay,
    PoeNinjaApiCurrencyTrade Receive,
    PoeNinjaApiSparkLine PaySparkLine,
    PoeNinjaApiSparkLine ReceiveSparkLine,
    double ChaosEquivalent,
    PoeNinjaApiSparkLine LowConfidencePaySparkLine,
    PoeNinjaApiSparkLine LowConfidenceReceiveSparkLine,
    string DetailsId
);

internal sealed record PoeNinjaApiCurrencyTrade(
    long Id,
    long LeagueId,
    long PayCurrencyId,
    long GetCurrencyId,
    DateTimeOffset SampleTimeUrc,
    long Count,
    double Value,
    long DataPointCount,
    bool IncludesSecondary,
    long ListingCount
);

internal sealed record PoeNinjaApiCurrencyDetails(
    long Id,
    string Icon,
    string Name,
    string TradeId
);

[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiCurrencyPriceEnvalope(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiCurrencyPrice Price);

internal sealed record PoeNinjaApiCurrencyResponse(
    ImmutableArray<PoeNinjaApiCurrencyPrice> Lines,
    ImmutableArray<PoeNinjaApiCurrencyDetails> CurrencyDetails
);
