using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using GemLevelProtScraper;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Sentry;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var apiKey = builder.Configuration["Authen1tication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

// Encoding
var provider = CodePagesEncodingProvider.Instance;
Encoding.RegisterProvider(provider);

// BSON Serialization
BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());
// Sentry
var flush = SentrySdk.Init("https://bcf0ff9fab08594e449c0638263a731f@o4505884379250688.ingest.sentry.io/4505884389670912");
AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
{
    if (args.Exception is not Polly.ExecutionRejectedException and not OperationCanceledException and not ObjectDisposedException)
    {
        SentrySdk.CaptureException(args.Exception);
    }
};

builder.Services
    .Configure<PoeDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDatabaseSettings"))
    .AddMigrations()
    .AddSystemClock()
    .AddSingleton<SignalCompletionStorage>()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddTransient<PoeRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddHostedService<PoeScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .WithLongLivingServiceLifetime(ServiceLifetime.Scoped)
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaCleanup>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbCleanup>()
        .AddDataFlow<PoeDbSink>()
        .AddDataFlow<DataflowSignal<PoeDbListCompleted>>()
        .AddDataFlow<PoeLeaguesSpider>()
        .AddDataFlow<PoeSink>()
        .AddDataFlow<DataflowSignal<PoeLeagueListCompleted>>()
        .Use(ScrapeAASRole.ProxyProvider, s => s.AddWebShareProxyProvider(o =>
        {
            o.ApiKey = webShareKey;
            o.CacheExpiration = TimeSpan.FromMinutes(30);
        }))
    )
    .AddMemoryCache()
    .AddResponseCaching()
    .AddHttpContextAccessor()
    .AddOutputCache(o =>
    {
        o.AddBasePolicy(b => b.Cache().Expire(TimeSpan.FromSeconds(60)));
        o.AddPolicy("gem-profit", b => b
            .Cache()
            .Expire(TimeSpan.FromMinutes(30))
            .SetVaryByQuery("league", "gem_name", "min_sell_price_chaos", "max_buy_price_chaos", "min_experience_delta", "items_count")
        );
    });

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
        [FromQuery(Name = "league")] string league = "",
        [FromQuery(Name = "gem_name")] string gemNameWindcard = "*",
        [FromQuery(Name = "min_sell_price_chaos")] double? minSellPriceChaos = null,
        [FromQuery(Name = "max_buy_price_chaos")] double? maxBuyPriceChaos = null,
        [FromQuery(Name = "min_experience_delta")] double? minExperienceDelta = null,
        [FromQuery(Name = "min_listing_count")] int minListingCount = 4,
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
            GemNameWindcard = gemNameWindcard,
            MinSellPriceChaos = minSellPriceChaos,
            MaxBuyPriceChaos = maxBuyPriceChaos,
            MinExperienceDelta = minExperienceDelta,
            MinimumListingCount = minListingCount
        };
        var data = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        var count = Math.Min(data.Length, itemsCount);
        if (count > 0)
        {
            return Results.Ok(new ArraySegment<ProfitResponse>(Unsafe.As<ImmutableArray<ProfitResponse>, ProfitResponse[]>(ref data), 0, count));
        }

        return Results.Ok(data);
    }).CacheOutput("gem-profit");
app.Run();

