using System;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Migration;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeDb;

internal sealed record PoeDbRoot(string ActiveSkillUrl);

internal sealed record PoeDbActiveSkillsResponse(ImmutableArray<PoeDbSkillName> Data);

internal sealed record PoeDbSkillName(string Name);

internal sealed record PoeDbGemQuality(string Type);

internal sealed record PoeDbSkillRelatedGem(PoeDbSkillName Name, string Text);

internal sealed record PoeDbSkillDescription(string Text, ImmutableArray<PoeDbSkillRelatedGem> RelatedGems);

internal sealed record PoeDbStatRequirements(double? Intelligence, double? Dexterity, double? Strenght);

internal sealed record PoeDbSkillLevel(double Level, double RequiresLevel, PoeDbStatRequirements Requirements, double? Experience);

internal sealed record PoeDbLink(string Label, string Link);

internal sealed record PoeDbSkillStats(string BaseType, PoeDbLink Class, ImmutableArray<PoeDbLink> Acronyms, string Metadata, ImmutableArray<PoeDbLink> ReferenceUrls);

[BsonIgnoreExtraElements]
internal sealed record PoeDbSkill(PoeDbSkillName Name, PoeDbSkillStats Stats, PoeDbSkillDescription? Description, ImmutableArray<PoeDbGemQuality> Qualities, ImmutableArray<PoeDbSkillLevel> LevelEffects);

