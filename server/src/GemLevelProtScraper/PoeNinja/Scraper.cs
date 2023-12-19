using System.Text.Json;
using GemLevelProtScraper.Poe;
using ScrapeAAS;

namespace GemLevelProtScraper.PoeNinja;

internal sealed class PoeNinjaScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaList>>();
            var completedPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeNinjaListCompleted>>();
            var league = await GetCurrentPcLeauge(scope, LeaugeMode.Softcore, stoppingToken).ConfigureAwait(false);
            await rootPublisher.PublishAsync(new(LeaugeMode.Softcore, $"https://poe.ninja/api/data/itemoverview?league={league.Name}&type=SkillGem&language=en"), stoppingToken).ConfigureAwait(false);
            await completedPublisher.PublishAsync(new(league.Mode), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }

        static async Task<PoeLeauge> GetCurrentPcLeauge(AsyncServiceScope scope, LeaugeMode league, CancellationToken stoppingToken)
        {
            var poeRepository = scope.ServiceProvider.GetRequiredService<PoeRepository>();
            var poeLeagueListInitialCompletedSignal = scope.ServiceProvider.GetRequiredService<DataflowSignal<PoeLeagueListCompleted>>();
            await poeLeagueListInitialCompletedSignal.WaitAsync(stoppingToken).ConfigureAwait(false);
            var currentSoftcoreTradePcLeauge = await poeRepository.GetByModeAndRealmAsync(league, Realm.Pc, stoppingToken).ConfigureAwait(false);
            return currentSoftcoreTradePcLeauge ?? throw new InvalidOperationException($"No leauge for mode '{league}' found");
        }
    }
}

internal sealed class PoeNinjaSpider(IHttpClientFactory httpClientFactory, IDataflowPublisher<PoeNinjaApiGemPrice> gemPublisher) : IDataflowHandler<PoeNinjaList>
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask HandleAsync(PoeNinjaList root, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(root.GemPriceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var envelope = await JsonSerializer.DeserializeAsync<PoeNinjaApiGemPricesEnvelope>(content, _jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The poe.ninja API response is no PoeNinjaApiGemPricesEnvelope");
        await Task.WhenAll(
            envelope.Lines
                .Select(gemPrice => gemPublisher.PublishAsync(gemPrice))
                .SelectTruthy(task => task.IsCompletedSuccessfully ? null : task.AsTask())
        ).ConfigureAwait(false);
    }
}

internal sealed class PoeNinjaSink(PoeNinjaRepository repository) : IDataflowHandler<PoeNinjaApiGemPrice>
{
    public async ValueTask HandleAsync(PoeNinjaApiGemPrice newGemPrice, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(newGemPrice, cancellationToken).ConfigureAwait(false);
    }
}
