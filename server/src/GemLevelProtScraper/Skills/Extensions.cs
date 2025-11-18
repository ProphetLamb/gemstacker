namespace GemLevelProtScraper.Skills;

public static class SkillsExtensions
{
    public static bool IsVaalSkillGem(this SkillGem gem)
    {
        return gem.Name.StartsWith("vaal ", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAwakenedGem(this SkillGem gem)
    {
        return gem.Name.StartsWith("awakened ", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSupportGem(this SkillGem gem)
    {
        return gem.Name.EndsWith(" support", StringComparison.OrdinalIgnoreCase);
    }

    public static SkillGemPrice ToVendorFreePrice(this SkillGemPrice price)
    {
        return price with
        {
            GemLevel = 1,
            GemQuality = 0,
            ListingCount = 9999,
            ChaosValue = 0,
            DivineValue = 0
        };
    }
}
