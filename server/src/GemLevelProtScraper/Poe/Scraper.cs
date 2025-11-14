using System.Text.Json;
using ScrapeAAS;

namespace GemLevelProtScraper.Poe;

internal sealed class PoeScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeLeagueList>>();
            var completedPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<PoeLeagueListCompleted>>();
            await rootPublisher.PublishAsync(new("https://www.pathofexile.com/api"), stoppingToken).ConfigureAwait(false);
            await completedPublisher.PublishAsync(new(), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

public sealed class PoeLeaguesSpider(IDataflowPublisher<PoeLeague> publisher, IHttpClientFactory httpClientFactory) : IDataflowHandler<PoeLeagueList>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    public async ValueTask HandleAsync(PoeLeagueList message, CancellationToken cancellationToken = default)
    {
        Uri apiUrl = new($"{message.ApiUrl}/trade/data/leagues");
        HttpRequestMessage req = new(HttpMethod.Get, apiUrl);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new("application/json"));
        req.Headers.UserAgent.Add(new("OAuth-poe-gemleveling-profit-calculator", "0.1"));
        using var client = httpClientFactory.CreateClient();
        var rsp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        _ = rsp.EnsureSuccessStatusCode();
        var content = rsp.Content;

        // var content = await pageLoader.LoadAsync(apiUrl, cancellationToken).ConfigureAwait(false);
        var response = await content.ReadFromJsonAsync<PoeLeagueListRepsonse>(_jsonSerializerOptions, cancellationToken).ConfigureAwait(false);

        var league = response.Result.First().Id;

        var items = response.Result
            .Select(item => new PoeLeague
                (
                    item.Id,
                    item.Text,
                    RealmHelper.Parse(item.Realm),
                    LeagueModeHelper.Parse(item.Id, league)
                )
            );

        await publisher.PublishAllAsync(items, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class PoeSink(PoeRepository repository) : IDataflowHandler<PoeLeague>
{
    public async ValueTask HandleAsync(PoeLeague message, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
