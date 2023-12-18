using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace GemLevelProtScraper;

public static class PagerExtensions
{
    public static IServiceCollection AddPager(this IServiceCollection services)
    {
        services.Add(new ServiceDescriptor(typeof(Pager<>), typeof(Pager<>), ServiceLifetime.Transient));
        return services;
    }
}

public sealed class Pager<TItem>(IMemoryCache pageCollectionCache)
{
    public Page<TItem> Get(Guid pagerId, int pageNumber)
    {
        return pageCollectionCache.Get<PageCollection<TItem>>(pagerId) is { } pager
            ? pager[pageNumber - 1] ?? pager.Empty()
            : Page<TItem>.Empty(0);
    }

    public Page<TItem> First(IEnumerable<TItem> sequence, int pageSize, IPaginatable paginatable, MemoryCacheEntryOptions? memoryCacheEntryOptions = null)
    {
        var pagerId = Guid.NewGuid();
        var pager = pageCollectionCache.Set(
            pagerId,
            PageCollection<TItem>.Create(pagerId, sequence, pageSize, paginatable),
            memoryCacheEntryOptions ?? new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            }
        );

        return pager[0] ?? pager.Empty();
    }

    public Page<TItem> First(IEnumerable<TItem> sequence, int pageSize, HttpRequest request)
    {
        return First(sequence, pageSize, new RequestQueryPaginatable(request));
    }

    public Page<TItem> First(IEnumerable<TItem> sequence, int pageSize, HttpRequest request, string pagerIdQueryName, string pageNumberQueryName)
    {
        return First(sequence, pageSize, new RequestQueryPaginatable(request, pagerIdQueryName, pageNumberQueryName));
    }
}

internal sealed class PageCollection<TItem>(
    Guid pagerId,
    ImmutableArray<TItem> items,
    int pageSize,
    ImmutableArray<Uri> pages
) : IEnumerable<Page<TItem>>
{

    public Page<TItem>? this[int pageIndex] => (uint)pageIndex < (uint)pages.Length
        ? GetPage(pageIndex)
        : null;

    public Guid PagerId => pagerId;

    public Page<TItem> Empty()
    {
        return Page<TItem>.Empty(pageSize) with
        {
            FirstPage = pages.FirstOrDefault(),
            LastPage = pages.LastOrDefault()
        };
    }

    public static PageCollection<TItem> Create(Guid pagerId, IEnumerable<TItem> sequence, int pageSize, IPaginatable paginatable)
    {
        if (sequence is not ImmutableArray<TItem> items)
        {
            items = sequence.ToImmutableArray();
        }

        if (items.IsDefaultOrEmpty)
        {
            return new(pagerId, items, pageSize, ImmutableArray<Uri>.Empty);
        }
        var totalPages = ((items.Length - 1) / pageSize) + 1;
        var pages = Enumerable.Range(0, totalPages)
            .Select(p => paginatable.CreatePageUrl(pagerId, p))
            .ToImmutableArray();
        return new(pagerId, items, pageSize, pages);
    }

    public IEnumerator<Page<TItem>> GetEnumerator()
    {
        for (var pageIndex = 0; pageIndex < pages.Length; pageIndex += 1)
        {
            yield return GetPage(pageIndex);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private Page<TItem> GetPage(int pageIndex)
    {
        var offset = Math.Clamp(pageIndex * pageSize, 0, items.Length - 1);
        var length = Math.Clamp(items.Length - offset, 0, pageSize - 1);
        int? nextIndex = pageIndex + 1 < pages.Length ? pageIndex + 1 : null;
        int? previousIndex = pageIndex - 1 >= 0 ? pageIndex - 1 : null;
        return new()
        {
            PageNumber = pageIndex + 1,
            PageSize = pageSize,
            TotalPages = pages.Length,
            TotalRecords = items.Length,
            FirstPage = pages.First(),
            LastPage = pages.Last(),
            NextPage = nextIndex is { } n ? pages[n] : null,
            PreviousPage = previousIndex is { } p ? pages[p] : null,
            Data = new ArraySegment<TItem>
            (
                Unsafe.As<ImmutableArray<TItem>, TItem[]>(ref items),
                offset,
                length
            )
        };
    }
}

public interface IPaginatable
{
    Uri CreatePageUrl(Guid pagerId, int pageIndex);
}

public sealed class RequestQueryPaginatable(Uri requestBaseUrl, string pagerIdQueryName, string pageNumberQueryName) : IPaginatable
{
    public const string DefaultPagerIdQueryName = "pager_id";
    public const string DefaultPageNumberQueryName = "page_index";

    public RequestQueryPaginatable(Uri requestBaseUri)
        : this(requestBaseUri, DefaultPagerIdQueryName, DefaultPageNumberQueryName)
    {
    }

    public RequestQueryPaginatable(HttpRequest request)
        : this(request, DefaultPagerIdQueryName, DefaultPageNumberQueryName)
    {
    }

    public RequestQueryPaginatable(HttpRequest request, string pagerIdQueryName, string pageNumberQueryName)
        : this(new Uri(request.GetDisplayUrl()), pagerIdQueryName, pageNumberQueryName)
    {
    }

    public Uri CreatePageUrl(Guid pagerId, int pageIndex)
    {
        var pageNumber = pageIndex + 1;
        UriBuilder builder = new(requestBaseUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);
        query.Add(pagerIdQueryName, pagerId.ToString());
        query.Add(pageNumberQueryName, pageNumber.ToString(CultureInfo.InvariantCulture));
        builder.Query = query.ToString();
        return builder.Uri;
    }
}

public sealed record Page<TItem>
{
    public required long PageNumber { get; init; }
    public required long PageSize { get; init; }
    public required long TotalPages { get; init; }
    public required long TotalRecords { get; init; }
    public Uri? FirstPage { get; set; }
    public Uri? LastPage { get; set; }
    public Uri? NextPage { get; set; }
    public Uri? PreviousPage { get; set; }
    public required IEnumerable<TItem> Data { get; init; }

    public static Page<TItem> Empty(int pageSize)
    {
        return new()
        {
            PageNumber = 0,
            PageSize = pageSize,
            TotalPages = 0,
            TotalRecords = 0,
            Data = Enumerable.Empty<TItem>()
        };
    }
}
