using GemLevelProtScraper.Skills;

namespace GemLevelProtScraper.Profit.Recipes;

public class LevelCorruptAddLevelDropFailureSell : IProfitRecipe
{
    public string Name => "level_corrupt_add_level_drop_failure_sell";

    public ProfitMargin? Execute(SkillProfitCalculationContext ctx)
    {
        if ((ctx.MinLevel ?? ctx.MinLevel20Quality) is not { } min
            || (ctx.CorruptedAddLevel20Quality ?? ctx.CorruptedAddLevel) is not { } corruptAddLevel
            || (ctx.CorruptedMaxLevel20Quality ?? ctx.CorruptedMaxLevel) is not { } corruptFailure
            || (ctx.CorruptedMaxLevel23Quality ?? corruptFailure) is not { } corruptAddQuality
            || (ctx.CorruptedMaxLevel ?? corruptFailure) is not { } corruptRemQuality
            || (ctx.CorruptedMinLevel20Quality ?? ctx.CorruptedMinLevel ?? corruptFailure.Min(min)) is not
            {
            } minCorrupted)
        {
            return null;
        }

        if (ctx.CanBuyFromVendor())
        {
            return null;
        }

        return ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min,
            minCorrupted
        );
    }

    public static ProfitMargin ProfitMarginUnchecked(
        SkillProfitCalculationContext ctx,
        SkillGemPrice corruptAddLevel,
        SkillGemPrice corruptAddQuality,
        SkillGemPrice corruptRemQuality,
        SkillGemPrice corruptFailure,
        SkillGemPrice min,
        SkillGemPrice minCorrupted
    )
    {
        var result = LevelCorruptAddLevelSell.ProfitMarginUnchecked(
            ctx,
            corruptAddLevel,
            corruptAddQuality,
            corruptRemQuality,
            corruptFailure,
            min
        );
        var probabilistic = result.Probabilistic!.ToList();
        // add chance to remove level and destroy the gem
        // sell the gem for the min level again
        probabilistic.Add(
            new() { Earnings = minCorrupted.ChaosValue - min.ChaosValue, Chance = 1 / 8.0, Label = "corrupt_rem_level" }
        );
        // reduce chance for no effect to by the chance to destroy the gem
        var noChange = probabilistic.First(x => x.Label == "no_change");
        _ = probabilistic.Remove(noChange);
        probabilistic.Add(noChange with { Chance = noChange.Chance - 1 / 8.0 });
        // recreate keyfigures
        var levelEarning = ctx.ProbabilisticEarnings(probabilistic) - ctx.RecipeCost(result.RecipeCost!);
        var experienceDelta = ctx.Skill.SumExperience * ctx.ExperienceFactor(ctx.GemQuality(min));
        return result with
        {
            GainMargin = ctx.GainMargin(levelEarning, experienceDelta),
            ExperienceDelta = experienceDelta,
            AdjustedEarnings = levelEarning,
            Probabilistic = probabilistic,
            MinAttemptsToProfit = ctx.AttemptsToProfit(probabilistic),
        };
    }
}
