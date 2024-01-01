using System.Diagnostics.CodeAnalysis;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeNinja;

namespace GemLevelProtScraper;

internal sealed class ExchangeRateProvider(PoeNinjaCurrencyRepository currencyRepository, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly PoeNinjaCurrencyRepository _currencyRepository = currencyRepository;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private readonly object _exchangeRatesLock = new();
    private Dictionary<Key, PoeNinjaCurrencyExchangeRate> _exchangeRates = new();
    private Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>>? _exchangeRatesTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var signal = scope.ServiceProvider.GetRequiredService<DataflowSignal<PoeNinjaListCompleted>>();
        _exchangeRatesTask = InitializeAsync(stoppingToken);
        await foreach (var completion in signal.ToEnumerableAsync(stoppingToken).ConfigureAwait(false))
        {
            var task = Interlocked.Exchange(ref _exchangeRatesTask, RefreshAsync(completion.League.Mode, stoppingToken));
            if (task is not null)
            {
                _ = await task.ConfigureAwait(false);
            }
            stoppingToken.ThrowIfCancellationRequested();
        }
    }

    public ValueTask<ExchangeRateCollection> GetExchangeRatesAsync(CancellationToken cancellationToken = default)
    {
        var task = _exchangeRatesTask;
        if (task is null || task.IsCompletedSuccessfully)
        {
            lock (_exchangeRatesLock)
            {
                return new(new ExchangeRateCollection(_exchangeRates));
            }
        }

        return GetExchangeRatesTask(task.WaitAsync(cancellationToken));

        static async ValueTask<ExchangeRateCollection> GetExchangeRatesTask(Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>> exchangeRatesTask)
        {
            var exchangeRates = await exchangeRatesTask.ConfigureAwait(false);
            return new(exchangeRates);
        }
    }

    private async Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>> InitializeAsync(CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(LeagueModeHelper.WellknownLeagues.Select(league => GetExchangeRatesAsync(league, cancellationToken))).ConfigureAwait(false);
        Dictionary<Key, PoeNinjaCurrencyExchangeRate> exchangeRates = new(results.Sum(r => r.ExchangeRates.Count));
        foreach (var (league, rates) in results)
        {
            foreach (var rate in rates)
            {
                exchangeRates.Add(new(league, rate.CurrencyTypeName), rate);
            }
        }
        AmendAndReplaceExchangeRates(null, exchangeRates);
        return exchangeRates;

        async Task<(LeagueMode League, List<PoeNinjaCurrencyExchangeRate> ExchangeRates)> GetExchangeRatesAsync(LeagueMode league, CancellationToken cancellationToken)
        {
            var rates = await _currencyRepository.GetExchangeRatesAsync(league, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
            return (league, rates);
        }
    }

    private async Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>> RefreshAsync(LeagueMode league, CancellationToken cancellationToken)
    {
        var newExchangeRates = _currencyRepository.GetExchangeRatesAsync(league, cancellationToken);
        Dictionary<Key, PoeNinjaCurrencyExchangeRate> exchangeRates = new(_exchangeRates.Count);
        await foreach (var rate in newExchangeRates.ConfigureAwait(false))
        {
            exchangeRates.Add(new(league, rate.CurrencyTypeName), rate);
            cancellationToken.ThrowIfCancellationRequested();
        }
        AmendAndReplaceExchangeRates(league, exchangeRates);
        return exchangeRates;
    }

    private void AmendAndReplaceExchangeRates(LeagueMode? amendExceptleague, Dictionary<Key, PoeNinjaCurrencyExchangeRate> newExchangeRates)
    {
        lock (_exchangeRatesLock)
        {
            if (amendExceptleague is { } l)
            {
                foreach (var (key, value) in _exchangeRates.Where(kvp => kvp.Key.Mode != l))
                {
                    _ = newExchangeRates.TryAdd(key, value);
                }
            }
            _exchangeRates = newExchangeRates;
        }
    }

    internal readonly struct Key(LeagueMode mode, string name) : IEquatable<Key>
    {
        public LeagueMode Mode => mode;
        public string Name => name;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Key other && Equals(other);
        }

        public bool Equals(Key other)
        {
            return other.Mode == mode && other.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(mode, name);
        }
    }
}

internal readonly struct ExchangeRateCollection(Dictionary<ExchangeRateProvider.Key, PoeNinjaCurrencyExchangeRate> exchangeRates)
{
    public bool TryGetValue(LeagueMode mode, string currencyTypeName, [MaybeNullWhen(false)] out PoeNinjaCurrencyExchangeRate rates)
    {
        return exchangeRates.TryGetValue(new(mode, currencyTypeName), out rates);
    }
}
