using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Migration;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeNinja;

internal sealed class PoeNinjaScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaRoot>>();
            await rootPublisher.PublishAsync(new("https://poe.ninja/api/data/itemoverview?league=Crucible&type=SkillGem&language=en"), stoppingToken).ConfigureAwait(false);

            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

internal sealed class PoeNinjaSpider(IHttpClientFactory httpClientFactory, IDataflowPublisher<PoeNinjaApiGemPrice> gemPublisher) : IDataflowHandler<PoeNinjaRoot>
{
    public async ValueTask HandleAsync(PoeNinjaRoot root, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(root.GemPriceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemPricesEnvelope>(content, jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPricesEnvelope");
        await Task.WhenAll(
            envelope.Lines
                .Select(gemPrice => gemPublisher.PublishAsync(gemPrice))
                .SelectTruthy(task => !task.IsCompletedSuccessfully ? task.AsTask() : null)
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink(IOptions<PoeNinjaDatabaseSettings> settings, IMongoMigrationCompletion migrationCompletion) : IDataflowHandler<PoeNinjaApiGemPrice>
{
    private readonly IMongoCollection<PoeNinjaApiGemPrice> _gemPriceCollection = new MongoClient(settings.Value.ConnectionString)
        .GetDatabase(settings.Value.DatabaseName)
        .GetCollection<PoeNinjaApiGemPrice>(settings.Value.GemPriceCollectionName);

    public async ValueTask HandleAsync(PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        // _ = await migrationCompletion.WaitAsync(settings.Value, cancellationToken).ConfigureAwait(false);
        _ = await _gemPriceCollection.FindOneAndReplaceAsync(
            gemPrice
                => gemPrice.GemLevel == newGemPrice.GemLevel
                && gemPrice.GemQuality == newGemPrice.GemQuality
                && gemPrice.Name == newGemPrice.Name,
            newGemPrice,
            new() { IsUpsert = true },
            cancellationToken
        ).ConfigureAwait(false);
    }
}
