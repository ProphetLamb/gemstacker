using System.Collections;
using System.Diagnostics.CodeAnalysis;
using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeNinja;

namespace GemLevelProtScraper;

public sealed class ExchangeRateProvider(
    PoeNinjaCurrencyRepository currencyRepository,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private readonly TaskCompletionSource _serviceStartCompletion = new();
    private readonly Lock _exchangeRatesLock = new();
    private Dictionary<Key, PoeNinjaCurrencyExchangeRate> _exchangeRates = [];
    private Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>>? _exchangeRatesTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var signal = scope.ServiceProvider.GetRequiredService<DataflowSignal<PoeNinjaListCompleted>>();
        await Task
            .WhenAll(RefreshUsingSignal(signal, stoppingToken), RefreshPeriodically(stoppingToken))
            .ConfigureAwait(false);
    }

    private async Task RefreshUsingSignal(
        DataflowSignal<PoeNinjaListCompleted> signal,
        CancellationToken cancellationToken
    )
    {
        _exchangeRatesTask = InitializeAsync(cancellationToken);
        await foreach (var completion in signal.ToEnumerableAsync(cancellationToken).ConfigureAwait(false))
        {
            var task = Interlocked.Exchange(
                ref _exchangeRatesTask,
                RefreshAsync(completion.League.Mode, cancellationToken)
            );
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (task is not null)
            {
                _ = await task.ConfigureAwait(false);
            }

            // notify listeners that the service has started
            _ = _serviceStartCompletion.TrySetResult();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task RefreshPeriodically(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
            var task = Interlocked.Exchange(ref _exchangeRatesTask, InitializeAsync(cancellationToken));
            if (task is not null)
            {
                _ = await task.ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public ValueTask<ExchangeRateCollection> GetExchangeRatesAsync(CancellationToken cancellationToken = default)
    {
        var task = _exchangeRatesTask;
        if (task is not null && task.IsCompletedSuccessfully)
        {
            return new(GetCurrentExchangeRates());
        }

        return GetExchangeRatesTask(task, cancellationToken);

        async ValueTask<ExchangeRateCollection> GetExchangeRatesTask(
            Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>>? exchangeRatesTask,
            CancellationToken cancellationToken
        )
        {
            if (exchangeRatesTask is null)
            {
                await _serviceStartCompletion.Task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return GetCurrentExchangeRates();
            }

            var exchangeRates = await exchangeRatesTask.ConfigureAwait(false);
            return new(exchangeRates);
        }

        ExchangeRateCollection GetCurrentExchangeRates()
        {
            lock (_exchangeRatesLock)
            {
                return new(_exchangeRates);
            }
        }
    }

    private async Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>> InitializeAsync(
        CancellationToken cancellationToken
    )
    {
        var results = await currencyRepository
            .GetAllExchangeRatesAsync(cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Key, PoeNinjaCurrencyExchangeRate> exchangeRates = new(results.Count);
        foreach (var (league, rate) in results)
        {
            exchangeRates.Add(new(league, rate.CurrencyTypeName), rate);
        }

        AmendAndReplaceExchangeRates(null, exchangeRates);
        return exchangeRates;
    }

    private async Task<Dictionary<Key, PoeNinjaCurrencyExchangeRate>> RefreshAsync(
        LeagueMode league,
        CancellationToken cancellationToken = default
    )
    {
        var newExchangeRates = currencyRepository.GetExchangeRatesAsync(league, cancellationToken);
        int count;
        lock (_exchangeRatesLock)
        {
            count = _exchangeRates.Count;
        }

        Dictionary<Key, PoeNinjaCurrencyExchangeRate> exchangeRates = new(count);
        await foreach (var rate in newExchangeRates.ConfigureAwait(false))
        {
            exchangeRates.Add(new(league, rate.CurrencyTypeName), rate);
            cancellationToken.ThrowIfCancellationRequested();
        }

        AmendAndReplaceExchangeRates(league, exchangeRates);
        return exchangeRates;
    }

    private void AmendAndReplaceExchangeRates(
        LeagueMode? amendExceptLeague,
        Dictionary<Key, PoeNinjaCurrencyExchangeRate> newExchangeRates
    )
    {
        lock (_exchangeRatesLock)
        {
            if (amendExceptLeague is { } l)
            {
                foreach (var (key, value) in _exchangeRates.Where(kvp => kvp.Key.Mode != l))
                {
                    _ = newExchangeRates.TryAdd(key, value);
                }
            }

            _exchangeRates = newExchangeRates;
        }
    }

    public readonly struct Key(LeagueMode mode, string name) : IEquatable<Key>
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

        public static bool operator ==(Key left, Key right) => left.Equals(right);

        public static bool operator !=(Key left, Key right) => !(left == right);
    }
}

public readonly struct ExchangeRateCollection(
    Dictionary<ExchangeRateProvider.Key, PoeNinjaCurrencyExchangeRate> exchangeRates
) : IReadOnlyCollection<PoeNinjaCurrencyExchangeRate>
{
    public int Count => exchangeRates.Count;

    public bool TryGetValue(
        LeagueMode mode,
        CurrencyTypeName name,
        [MaybeNullWhen(false)] out PoeNinjaCurrencyExchangeRate rates
    )
    {
        return exchangeRates.TryGetValue(new(mode, name.Value), out rates);
    }

    public IEnumerator<PoeNinjaCurrencyExchangeRate> GetEnumerator()
    {
        return exchangeRates.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return exchangeRates.GetEnumerator();
    }
}

public readonly struct CurrencyTypeName(string value) : IEquatable<CurrencyTypeName>, IParsable<CurrencyTypeName>
{
    public CurrencyTypeName(CurrencyTypeName existing)
        : this(existing.Value)
    {
    }

    public string Value
    {
        get;
    } = value;

    public static CurrencyTypeName DivineOrb => new("Divine Orb");
    public static CurrencyTypeName CartographersChisel => new("Cartographer's Chisel");
    public static CurrencyTypeName GemcuttersPrism => new("Gemcutter's Prism");
    public static CurrencyTypeName ChaosOrb => new("Chaos Orb");
    public static CurrencyTypeName VaalOrb => new("Vaal Orb");


    public bool Equals(CurrencyTypeName other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is CurrencyTypeName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(CurrencyTypeName left, CurrencyTypeName right) => left.Equals(right);

    public static bool operator !=(CurrencyTypeName left, CurrencyTypeName right) => !(left == right);

    public static CurrencyTypeName Parse(string? value)
    {
        return Parse(value, null);
    }

    public static CurrencyTypeName Parse(string? s, IFormatProvider? provider)
    {
        return TryParse(s, provider, out var c) ? c : throw new FormatException("Currency must not be null or empty");
    }

    public static bool TryParse([NotNullWhen(true)] string? s, out CurrencyTypeName result)
    {
        return TryParse(s, null, out result);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CurrencyTypeName result)
    {
        if (!string.IsNullOrEmpty(s))
        {
            result = new(s);
            return true;
        }

        result = default;
        return false;
    }
}
