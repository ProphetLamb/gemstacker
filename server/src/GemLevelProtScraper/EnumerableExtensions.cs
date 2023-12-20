using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public static ImmutableDictionary<TKey, ImmutableArray<TValue>> ToImmutableMap<T, TKey, TValue>(
        this IEnumerable<T> sequence,
        Func<T, TKey> keySelector,
        EqualityComparer<TKey>? keyComparer,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        // immutable dictionary is optimized for many keys.
        // we assume many values few keys here.
        // use a array dictionary instead.
        var dict = ImmutableDictionary.CreateBuilder<TKey, ImmutableArray<TValue>>(keyComparer);
        foreach (var item in sequence)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            if (dict.TryGetValue(key, out var items))
            {
                var insertionIndex = items.Length;
                ref var itemsArray = ref Unsafe.As<ImmutableArray<TValue>, TValue[]>(ref items);
                Array.Resize(ref itemsArray, insertionIndex + 1);
                itemsArray[insertionIndex] = value;
                // we dont have a pointer to the items bucket, so we have to manually update the value.
                dict[key] = items;
            }
            else
            {
                var itemsArray = new[] { value };
                dict[key] = Unsafe.As<TValue[], ImmutableArray<TValue>>(ref itemsArray);
            }
        }

        return dict.ToImmutable();
    }

    public static IEnumerable<TOut> SelectTruthy<TIn, TOut>(this IEnumerable<TIn> sequence, Func<TIn, TOut?> filterPredicate)
        where TOut : class
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

    public static void ForAll<T>(this IEnumerable<T> sequence, Action<T> action)
    {
        foreach (var item in sequence)
        {
            action(item);
        }
    }

    public static bool TryGetFirstAndLast<T>(
        this IEnumerable<T> sequence,
        [MaybeNullWhen(false)] out T first,
        [MaybeNullWhen(false)] out T last
    )
    {
        if (sequence.TryGetNonEnumeratedCount(out var count) && count == 0)
        {
            first = default;
            last = default;
            return false;
        }

        if (sequence is IReadOnlyList<T> roList && roList.Count > 0)
        {
            first = roList[0];
            last = roList[roList.Count - 1];
            return true;
        }

        if (sequence is IList<T> list && list.Count > 0)
        {
            first = list[0];
            last = list[list.Count - 1];
            return true;
        }

        using var en = sequence.GetEnumerator();
        if (!en.MoveNext())
        {
            first = default;
            last = default;
            return false;
        }
        first = en.Current;
        last = first;
        while (en.MoveNext())
        {
            last = en.Current;
        }
        return true;
    }
}
