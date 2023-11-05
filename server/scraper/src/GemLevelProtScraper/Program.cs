using GemLevelProtScraper;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

var apiKey = builder.Configuration["Authentication:ApiKey"];

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddMigrations()
    .AddTransient<ProfitService>()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
    )
    .AddHttpContextAccessor();

builder.Services.AddControllers();

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", async (
        HttpContext context,
        [FromServices] ProfitService profitService,
        [FromServices] Pager<ProfitResponse> pager,
        [FromQuery] ProfitRequest request,
        [FromQuery(Name = "pager_id")] Guid? maybePagerId = null,
        [FromQuery(Name = "page_number")] int? maybePageNumber = null,
        [FromQuery(Name = "page_size")] int pageSize = 25,
        CancellationToken cancellationToken = default
    ) =>
    {
        if (maybePagerId is { } pagerId && maybePageNumber is { } pageNumger)
        {
            return pager.Get(pagerId, pageNumger);
        }
        var response = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        return pager.First(response, pageSize, context.Request, "pager_id", "page_number");
    });
app.Run();

