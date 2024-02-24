using System.Text;
using System.Text.Json.Serialization;
using GemLevelProtScraper;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using GemLevelProtScraper.Skills;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Sentry;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var apiKey = builder.Configuration["Authentication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

// Encoding
var provider = CodePagesEncodingProvider.Instance;
Encoding.RegisterProvider(provider);

// BSON Serialization
BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());
// Sentry
using var flush = SentrySdk.Init("https://bcf0ff9fab08594e449c0638263a731f@o4505884379250688.ingest.sentry.io/4505884389670912");
AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
{
    if (args.Exception is not Polly.ExecutionRejectedException and not OperationCanceledException and not ObjectDisposedException)
    {
        SentrySdk.CaptureException(args.Exception);
    }
};

builder.Services
    .Configure<PoeDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDatabaseSettings"))
    .Configure<ProfitServiceOptions>(builder.Configuration.GetSection("ProfitService"))
    .AddMigrations()
    .AddSystemClock()
    .AddSingleton<SignalCompletionStorage>()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaGemRepository>()
    .AddTransient<PoeNinjaCurrencyRepository>()
    .AddTransient<PoeRepository>()
    .AddTransient<SkillGemRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<ExchangeRateProvider>()
    .AddSingleton(sf => sf.GetServices<IHostedService>().OfType<ExchangeRateProvider>().First())
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddHostedService<PoeScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .WithLongLivingServiceLifetime(ServiceLifetime.Scoped)
        .UseWebShareProxyProvider(o =>
        {
            o.ApiKey = webShareKey;
            o.CacheExpiration = TimeSpan.FromMinutes(30);
        })
        .AddDataflow<DataflowSignal<PoeDbListCompleted>>()
        .AddDataflow<DataflowSignal<PoeLeagueListCompleted>>()
        .AddDataflow<DataflowSignal<PoeNinjaListCompleted>>()
    )
    .AddMemoryCache()
    .AddResponseCaching()
    .AddHttpContextAccessor()
    .AddOutputCache(o => o.AddBasePolicy(b => b.Cache().Expire(TimeSpan.FromSeconds(60))));

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.AllowTrailingCommas = true;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicies.SnakeCaseLower));
});

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app.UseResponseCaching();

app
    .MapGet("gem-profit", async (
        [FromServices] ProfitService profitService,
        [FromServices] PoeRepository poeRepository,
        [FromQuery(Name = "league")] string? league = null,
        [FromQuery(Name = "gem_name")] string? gemNameWildcard = null,
        [FromQuery(Name = "added_quality")] long addedQuality = 0,
        [FromQuery(Name = "min_sell_price_chaos")] double? minSellPriceChaos = null,
        [FromQuery(Name = "max_buy_price_chaos")] double? maxBuyPriceChaos = null,
        [FromQuery(Name = "min_experience_delta")] double? minExperienceDelta = null,
        [FromQuery(Name = "min_listing_count")] long minListingCount = 8,
        [FromQuery(Name = "items_count")] int itemsCount = 10,
        CancellationToken cancellationToken = default
    ) =>
    {
        var baseLeague = await poeRepository.GetByModeAndRealmAsync(LeagueMode.League | LeagueMode.Softcore, Realm.Pc, cancellationToken).ConfigureAwait(false);
        if (baseLeague is null)
        {
            return Results.BadRequest(new
            {
                Error = "Invalid parameter value `league`",
                Message = $"Failed to parse the league value `{league}`"
            });
        }

        if (!LeagueModeHelper.TryParse(league, baseLeague.Name, out var leagueMode))
        {
            return Results.BadRequest(new
            {
                Error = "Invalid parameter value `league`",
                Message = $"Failed to parse the league value `{league}`"
            });
        }

        ProfitRequest request = new()
        {
            League = leagueMode,
            GemNameWildcard = gemNameWildcard,
            AddedQuality = addedQuality,
            MinSellPriceChaos = minSellPriceChaos,
            MaxBuyPriceChaos = maxBuyPriceChaos,
            MinExperienceDelta = minExperienceDelta,
            MinimumListingCount = minListingCount
        };
        var result = profitService.GetProfitAsync(request, cancellationToken);

        return Results.Ok(itemsCount is <= 0 or >= 20000 ? result : result.Take(itemsCount));
    }).CacheOutput(b => b
        .Cache()
        .Expire(TimeSpan.FromMinutes(30))
        .SetVaryByQuery("league", "gem_name", "added_quality", "min_sell_price_chaos", "max_buy_price_chaos", "min_experience_delta", "items_count")
    );
app.Run();
