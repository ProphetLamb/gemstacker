using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace GemLevelProtScraper;

public sealed record Page<TValue>
{
    public required string CurrentUrl { get; init; }
    public string? PreviousUrl { get; init; }
    public string? NextUrl { get; init; }
    public required int PageCount { get; init; }
    public required int PageSize { get; init; }
    public required int PageIndex { get; init; }
    public required int ItemsCount { get; init; }
    public required IEnumerable<TValue> Items { get; init; }
}

public sealed class PaginatorOptions
{
    public int PageSize { get; init; } = 64;
    public MemoryCacheEntryOptions MemoryCacheEntryOptions { get; init; } = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };
    public string QueryIdName { get; init; } = "pageId";
    public string QueryIndexName { get; init; } = "pageIndex";
}

public sealed class HttpRequestPaginator<TValue>(IMemoryCache cache, HttpRequest request, PaginatorOptions? options = null)
{
    private readonly HttpRequestPaginator<int, TValue> _paginator = new(cache, request, options);

    public bool TryGetPage([MaybeNullWhen(false)] out Page<TValue> page)
    {
        return _paginator.TryGetPage(0, out page);
    }


    public Page<TValue> CreatePage(ImmutableArray<TValue> data)
    {
        return _paginator.CreatePage(0, data);
    }
}

public sealed class HttpRequestPaginator<TKey, TValue>(IMemoryCache cache, HttpRequest request, PaginatorOptions? options = null)
    where TKey : IEquatable<TKey>
{
    private readonly PaginatorOptions _options = options ?? new();
    private readonly HttpRequestPaginatable _paginatable = new(request, options ?? new());

    public bool TryGetPage(TKey key, [MaybeNullWhen(false)] out Page<TValue> page)
    {
        if (!_paginatable.TryGetId(out var id) || !_paginatable.TryGetIndex(out var pageIndex))
        {
            page = null;
            return false;
        }

        if (cache.TryGetValue(new PaginatorId(id, key), out PageinatorContainer? container))
        {
            var data = container!.Data;
            page = _paginatable.CreatePage(data, id, pageIndex);
            return true;
        }

        page = null;
        return false;
    }

    public Page<TValue> CreatePage(TKey key, ImmutableArray<TValue> data)
    {
        if (!_paginatable.TryGetIndex(out var pageIndex))
        {
            pageIndex = 0;
        }
        PaginatorId pageId = new(Guid.NewGuid(), key);
        cache.Set(pageId, new PageinatorContainer(data), _options.MemoryCacheEntryOptions);
        return _paginatable.CreatePage(data, pageId.Value, pageIndex);
    }

    private sealed class PaginatorId(Guid value, TKey key) : IEquatable<PaginatorId>
    {
        public Guid Value { get; } = value;
        public TKey Key { get; } = key;

        public bool Equals(PaginatorId? other)
        {
            return other is { } paginatable && paginatable.Value.Equals(Value) && paginatable.Key.Equals(Key);
        }

        public override bool Equals(object? obj)
        {
            return obj is PaginatorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    private sealed class PageinatorContainer(ImmutableArray<TValue> data)
    {
        public ImmutableArray<TValue> Data { get; } = data;
    }
}

public sealed class HttpRequestPaginatable(HttpRequest request, PaginatorOptions options)
{
    public PaginatorOptions Options { get; } = options;

    public HttpRequest Request { get; } = request;

    public bool TryGetId(out Guid id)
    {
        if (Request.Query.TryGetValue(Options.QueryIdName, out var idValues) && Guid.TryParse(idValues.FirstOrDefault(), out id))
        {
            return true;
        }
        id = default;
        return false;
    }
    public bool TryGetIndex(out int index)
    {
        if (Request.Query.TryGetValue(Options.QueryIndexName, out var indexValues) && int.TryParse(indexValues.FirstOrDefault(), out index))
        {
            return true;
        }
        index = default;
        return false;
    }

    private string CreatePageUrl(Guid id, int pageIndex)
    {
        var currentUrl = Request.GetEncodedPathAndQuery();
        // trim the query string from the url
        var currentQueryLength = Request.QueryString.Value?.Length ?? 0;
        var currentUrlWithoutQuery = currentUrl.AsSpan(0, currentUrl.Length - currentQueryLength);
        // overwrite query id & index parameters
        var query = Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        query[Options.QueryIdName] = id.ToString();
        query[Options.QueryIndexName] = pageIndex.ToString(CultureInfo.InvariantCulture);
        var queryString = QueryString.Create(query).Value ?? "";
        // combine the url and query
        var urlLength = currentUrlWithoutQuery.Length + 1 + queryString.Length;
        StringBuilder urlBuilder = new(urlLength);
        urlBuilder.Append(currentUrlWithoutQuery);
        urlBuilder.Append(queryString);
        return urlBuilder.ToString();
    }

    public Page<TValue> CreatePage<TValue>(ImmutableArray<TValue> data, Guid dataId, int pageIndex)
    {
        var previousPageIndex = pageIndex - 1;
        var nextPageIndex = pageIndex + 1;
        var pageSize = Options.PageSize;
        var pageCount = (Math.Max(0, data.Length - 1) / pageSize) + 1;
        var pageItemsOffset = pageIndex * pageSize;
        var pageItemsCount = Math.Min(pageSize, data.Length - pageItemsOffset);
        if (pageCount == 0 || pageItemsOffset >= data.Length)
        {
            return new()
            {
                CurrentUrl = CreatePageUrl(dataId, pageIndex),
                PreviousUrl = previousPageIndex < 0 ? null : CreatePageUrl(dataId, previousPageIndex),
                PageIndex = pageIndex,
                PageSize = pageSize,
                PageCount = pageCount,
                ItemsCount = data.Length,
                Items = Array.Empty<TValue>(),
            };
        }
        Debug.Assert(pageItemsCount > 0);
        ArraySegment<TValue> slice = new(Unsafe.As<ImmutableArray<TValue>, TValue[]>(ref data), pageItemsOffset, pageItemsCount);
        return new()
        {
            CurrentUrl = CreatePageUrl(dataId, pageIndex),
            PreviousUrl = previousPageIndex < 0 ? null : CreatePageUrl(dataId, previousPageIndex),
            NextUrl = nextPageIndex >= pageCount ? null : CreatePageUrl(dataId, nextPageIndex),
            PageIndex = pageIndex,
            PageSize = pageSize,
            PageCount = pageCount,
            ItemsCount = data.Length,
            Items = slice.AsReadOnly()
        };
    }
}
