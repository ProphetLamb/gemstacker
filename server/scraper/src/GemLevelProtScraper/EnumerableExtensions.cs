using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace GemLevelProtScraper;

public static class EnumerableExtensions
{
    public static IReadOnlyDictionary<TKey, ImmutableArray<T>> ToImmutableMap<T, TKey>(
        this IEnumerable<T> sequence,
        Func<T, TKey> keySelector)
    where TKey : notnull
    {
        return ToImmutableMap(sequence, keySelector, null, static item => item);
    }

    public static IReadOnlyDictionary<TKey, ImmutableArray<TValue>> ToImmutableMap<T, TKey, TValue>(
        this IEnumerable<T> sequence,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        return ToImmutableMap(sequence, keySelector, null, valueSelector);
    }

    public static IReadOnlyDictionary<TKey, ImmutableArray<TValue>> ToImmutableMap<T, TKey, TValue>(
        this IEnumerable<T> sequence,
        Func<T, TKey> keySelector,
        EqualityComparer<TKey>? keyComparer,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        // immutable dictionary is optimized for many keys.
        // we assume many values few keys here.
        // use a array dictionary instead.
        Dictionary<TKey, TValue[]> dict = new(keyComparer);
        foreach (var item in sequence)
        {
            var key = keySelector(item);
            var value = valueSelector(item);

            ref var items = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
            var insertionIndex = exists ? items!.Length : 0;
            Array.Resize(ref items, insertionIndex + 1);
            items[insertionIndex] = value;
        }
        var dictAsImmutable = Unsafe.As<Dictionary<TKey, ImmutableArray<TValue>>>(dict);
        return dictAsImmutable.AsReadOnly();
    }

    public static IEnumerable<TIn> SelectTruthy<TIn, TOut>(this IEnumerable<TIn> sequence, Func<TIn, TIn?> filterPredicate)
        where TIn : class
    {
        foreach (var item in sequence)
        {
            if (filterPredicate(item) is { } result)
            {
                yield return result;
            }
        }
    }
    public static IEnumerable<TOut> SelectTruthy<TIn, TOut>(this IEnumerable<TIn> sequence, Func<TIn, TOut?> filterPredicate)
        where TOut : struct
    {
        foreach (var item in sequence)
        {
            if (filterPredicate(item) is { } result)
            {
                yield return result;
            }
        }
    }

    public static IEnumerable<TIn> SelectTruthy<TIn>(this IEnumerable<TIn> sequence, Func<TIn, TIn?> filterPredicate)
    where TIn : class
    {
        foreach (var item in sequence)
        {
            if (filterPredicate(item) is { } result)
            {
                yield return result;
            }
        }
    }
    public static IEnumerable<TIn> SelectTruthy<TIn>(this IEnumerable<TIn> sequence, Func<TIn, TIn?> filterPredicate)
        where TIn : struct
    {
        foreach (var item in sequence)
        {
            if (filterPredicate(item) is { } result)
            {
                yield return result;
            }
        }
    }
}
