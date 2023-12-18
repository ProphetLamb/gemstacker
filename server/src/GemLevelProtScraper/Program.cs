using GemLevelProtScraper;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

var apiKey = builder.Configuration["Authentication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddMigrations()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
        .Use(ScrapeAASRole.ProxyProvider, s => s.AddWebShareProxyProvider(o => o.ApiKey = webShareKey))
    )
    .AddHttpContextAccessor()
    .AddMemoryCache();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
        o.JsonSerializerOptions.AllowTrailingCommas = true;
    });

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", async (
        HttpContext context,
        [FromServices] ProfitService profitService,
        [FromServices] IMemoryCache memoryCache,
        [AsParameters] ProfitRequest request,
        [FromQuery(Name = "page_size")] int pageSize = 25,
        CancellationToken cancellationToken = default
    ) =>
    {
        HttpRequestPaginator<ProfitResponse> paginator = new(memoryCache, context.Request, new() { QueryIdName = "pager_id", QueryIndexName = "pager_index", PageSize = pageSize });
        if (paginator.TryGetPage(out var page))
        {
            return page;
        }
        var data = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        return paginator.CreatePage(data);
    });
app.Run();

