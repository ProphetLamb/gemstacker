using System.Runtime.CompilerServices;
using GemLevelProtScraper.Profit.Recipes;
using GemLevelProtScraper.Skills;
using Microsoft.Extensions.Options;

namespace GemLevelProtScraper.Profit;

public sealed class ProfitServiceOptions : IOptions<ProfitServiceOptions>
{
    public Dictionary<string, double>? SpecialExperienceFactorPerQualityGams
    {
        get;
        init;
    }

    public List<string>? VendorRestrictedGems
    {
        get;
        init;
    }

    ProfitServiceOptions IOptions<ProfitServiceOptions>.Value => this;
}

public sealed class ProfitService(
    SkillGemRepository repository,
    ExchangeRateProvider exchangeRateProvider,
    IOptions<ProfitServiceOptions> options,
    IEnumerable<IProfitRecipe> profitRecipes,
    ILoggerFactory loggerFactory
)
{
    private List<IProfitRecipe> ProfitRecipes
    {
        get;
    } = profitRecipes.ToList();

    public async ValueTask<IReadOnlyList<ProfitResponse>> GetProfitAsync(
        ProfitRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var allowedRecipes = request.DisallowedRecipes is null
            ? ProfitRecipes
            : ProfitRecipes.Where(x => !request.DisallowedRecipes.Contains(x.Name)).ToList();
        var exchangeRates = await exchangeRateProvider.GetExchangeRatesAsync(cancellationToken).ConfigureAwait(false);
        var calc = new ProfitMarginCalculator(
            request,
            exchangeRates,
            options.Value.SpecialExperienceFactorPerQualityGams ?? [],
            options.Value.VendorRestrictedGems?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [],
            allowedRecipes,
            loggerFactory.CreateLogger<ProfitMarginCalculator>()
        );
        var pricedGems = await repository
            .GetPricedGemsAsync(request.League, request.GemNameWildcard, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var eligiblePricedGems = pricedGems
            .AsParallel()
            .SelectTruthy(g => calc.ComputeProfitMargin(g.Skill, g.Prices) is { } delta
                ? new ProfitResponse
                {
                    Name = g.Skill.Name,
                    Color = g.Skill.Color,
                    Discriminator = g.Skill.Discriminator,
                    Type = g.Skill.BaseType,
                    ForeignInfoUrl = $"https://poedb.tw{g.Skill.RelativeUrl}",
                    PreferredRecipe = delta.PreferredRecipe,
                    Recipes = delta.ProfitMargins,
                    GainMargin = delta.MaxGainMargin,
                    Icon = g.Skill.IconUrl,
                    Max = delta.Max.Data,
                    Min = delta.Min.Data,
                }
                : null
            )
            .OrderByDescending(x => x.GainMargin)
            .ToList();
        return eligiblePricedGems;
    }
}

internal sealed class ProfitMarginCalculator(
    ProfitRequest request,
    ExchangeRateCollection exchangeRates,
    IReadOnlyDictionary<string, double> experienceFactorAddQualityByName,
    IReadOnlySet<string> vendorRestrictedGems,
    IReadOnlyCollection<IProfitRecipe> recipes,
    ILogger<ProfitMarginCalculator> logger
)
{
    public PriceDelta? ComputeProfitMargin(SkillGem skill, IEnumerable<SkillGemPrice> prices)
    {
        var profitRequest = request;
        var pricesOrdered = prices
            .Where(p => (profitRequest.MaxBuyPriceChaos is not { } maxBuy || p.ChaosValue <= maxBuy)
                        && (profitRequest.MinSellPriceChaos is not { } minSell || p.ChaosValue >= minSell)
                        && p.ListingCount >= profitRequest.MinimumListingCount
            )
            .ToArray();

        // order by ChaosValue descending
        pricesOrdered.AsSpan().Sort((lhs, rhs) => rhs.ChaosValue.CompareTo(lhs.ChaosValue));

        SkillProfitCalculationContext context = new(
            profitRequest,
            skill,
            exchangeRates,
            experienceFactorAddQualityByName,
            vendorRestrictedGems,
            pricesOrdered
        );

        Dictionary<string, ProfitMargin> profitMargins = [];
        foreach (var recipe in recipes)
        {
            try
            {
                if (recipe.Execute(context) is { } margin)
                {
                    profitMargins[recipe.Name] = margin;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to evaluate recipe {Recipe} for skill {Skill}",
                    recipe.Name,
                    skill.Name
                );
            }
        }

        foreach (var (key, _) in profitMargins.Where(ProfitMarginViolatesRequestConstraint).ToList())
        {
            profitMargins.Remove(key);
        }

        if (profitMargins.Count == 0)
        {
            return null;
        }

        var (recipeName, bestProfit) = profitMargins.MaxBy(kvp => kvp.Value.GainMargin);

        return new(
            profitMargins,
            new(bestProfit.Buy, 0),
            new(bestProfit.Sell, bestProfit.ExperienceDelta),
            recipeName,
            bestProfit.GainMargin,
            skill
        );

        bool ProfitMarginViolatesRequestConstraint(KeyValuePair<string, ProfitMargin> kvp)
        {
            var (_, margin) = kvp;
            if (margin.ExperienceDelta < profitRequest.MinExperienceDelta)
            {
                return true;
            }

            if (profitRequest.MaxBuyPriceChaos is { } max && margin.Buy.Price > max)
            {
                return true;
            }

            if (profitRequest.MinSellPriceChaos is { } min && margin.Sell.Price < min)
            {
                return true;
            }

            return false;
        }
    }
}
