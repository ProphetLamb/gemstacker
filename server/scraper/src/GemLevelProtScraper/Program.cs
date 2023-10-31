using GemLevelProtScraper;
using ScrapeAAS;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
    );

var app = builder.Build();
app.Run();
