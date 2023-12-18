using System.Collections.Immutable;
using MongoDB.Bson.Serialization.Attributes;

namespace GemLevelProtScraper.PoeDb;

internal sealed record PoeDbRoot(string ActiveSkillUrl);

internal sealed record PoeDbActiveSkillsResponse(ImmutableArray<PoeDbSkillName> Data);

internal sealed record PoeDbSkillName(string Name, string RelativeUrl);

internal sealed record PoeDbGemQuality(string Type);

internal sealed record PoeDbSkillRelatedGem(PoeDbSkillName Name, string Text);

internal sealed record PoeDbSkillDescription(string Text, ImmutableArray<PoeDbSkillRelatedGem> RelatedGems);

internal sealed record PoeDbStatRequirements(decimal? Intelligence, decimal? Dexterity, decimal? Strenght);

internal sealed record PoeDbSkillLevel(decimal Level, decimal RequiresLevel, PoeDbStatRequirements Requirements, decimal? Experience);

internal sealed record PoeDbLink(string Label, string Link);

internal sealed record PoeDbSkillStats(string BaseType, PoeDbLink Class, ImmutableArray<PoeDbLink> Acronyms, string Metadata, ImmutableArray<PoeDbLink> ReferenceUrls);

[BsonIgnoreExtraElements]
internal sealed record PoeDbSkill(PoeDbSkillName Name, PoeDbSkillStats Stats, PoeDbSkillDescription? Description, ImmutableArray<PoeDbGemQuality> Qualities, ImmutableArray<PoeDbSkillLevel> LevelEffects);

