using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.Skills;

[BsonIgnoreExtraElements]
public record SkillGem
(
    string Name,
    string RelativeUrl,
    GemColor Color,
    string? IconUrl,
    string BaseType,
    string? Discriminator,
    double SumExperience,
    double MaxLevel
);

public sealed record SkillGemPrice
(
    string Icon,
    bool Corrupted,
    long GemLevel,
    long GemQuality,
    double ChaosValue,
    double DivineValue,
    long ListingCount
);

[BsonIgnoreExtraElements]
public sealed record SkillGemPriced
(
    SkillGem Skill,
    ImmutableArray<SkillGemPrice> Prices
);
