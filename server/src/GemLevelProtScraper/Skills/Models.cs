namespace GemLevelProtScraper.Skills;

public sealed class SkillGem
{
    public required string Name { get; init; }
    public required string RelativeUrl { get; init; }
    public required GemColor Color { get; init; }
    public required string? IconUrl { get; init; }
    public required string BaseType { get; init; }
    public required string? Discriminator { get; init; }
    public required double SumExperience { get; init; }
    public required double MaxLevel { get; init; }
}
