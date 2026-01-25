using System.Collections.Immutable;
using GemLevelProtScraper.Poe;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeNinja;

internal sealed record PoeNinjaList(DateTimeOffset Timestamp, PoeLeague League, string Url);

internal sealed record PoeNinjaListCompleted(DateTimeOffset Timestamp, PoeLeague League);

internal sealed record PoeNinjaApiSparkLine(ImmutableArray<double?> Data, double TotalChange);

internal sealed record PoeNinjaApiPrice(
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

internal sealed record PoeNinjaApiExchangeLine(
    string Id,
    double PrimaryValue,
    double VolumePrimaryValue,
    string MaxVolumeCurrency,
    double MaxVolumeRate,
    PoeNinjaApiSparkLine Sparkline
);

internal sealed record PoeNinjaApiExchangeItem(
    string Id,
    string Name,
    string Image,
    string Category,
    string DetailsId
);

internal sealed record PoeNinjaApiExchangeCore(string Primary, string Secondary, Dictionary<string, double> Rates);

internal sealed record PoeNinjaApiExchangeResponse(PoeNinjaApiExchangeCore Core, ImmutableArray<PoeNinjaApiExchangeLine> Lines, ImmutableArray<PoeNinjaApiExchangeItem> Items);

[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiGemPriceEnvelope(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiPrice Price);

[BsonIgnoreExtraElements]
internal sealed record PoeNinjaApiIncursionTemplePriceEnvelope(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiPrice Price);

internal sealed record PoeNinjaApiGemResponse(
    ImmutableArray<PoeNinjaApiPrice> Lines
);

internal sealed record PoeNinjaApiCurrencyPrice(
    string CurrencyTypeName,
    PoeNinjaApiCurrencyTrade? Pay,
    PoeNinjaApiCurrencyTrade? Receive,
    PoeNinjaApiSparkLine? PaySparkLine,
    PoeNinjaApiSparkLine? ReceiveSparkLine,
    double ChaosEquivalent,
    PoeNinjaApiSparkLine? LowConfidencePaySparkLine,
    PoeNinjaApiSparkLine? LowConfidenceReceiveSparkLine,
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
internal sealed record PoeNinjaApiCurrencyPriceEnvelope(LeagueMode League, DateTime UtcTimestamp, PoeNinjaApiCurrencyPrice Price);

internal sealed record PoeNinjaApiCurrencyResponse(
    ImmutableArray<PoeNinjaApiCurrencyPrice> Lines,
    ImmutableArray<PoeNinjaApiCurrencyDetails> CurrencyDetails
);

public sealed record PoeNinjaCurrencyExchangeRate(string CurrencyTypeName, double ChaosEquivalent);
