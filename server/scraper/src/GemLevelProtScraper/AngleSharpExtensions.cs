using System.Collections;
using System.Collections.Immutable;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace GemLevelProtScraper;

public static class AngleSharpExtensions
{
    public static IEnumerable<IElement> NextElementSiblings(this IElement? element, Func<IElement, bool> siblingSelector)
    {
        while ((element = element?.NextElementSibling) is not null)
        {
            if (siblingSelector(element))
            {
                yield return element;
            }
        }
    }
    public static IEnumerable<IElement> PreviousElementSiblings(this IElement? element, Func<IElement, bool> siblingSelector)
    {
        while ((element = element?.PreviousElementSibling) is not null)
        {
            if (siblingSelector(element))
            {
                yield return element;
            }
        }
    }

    public static TableHeaders? GetHeadersWithIndexOrDefault(this IHtmlTableElement table)
    {
        var headers = table.Head?.Rows
            .SelectMany(tr => tr.Cells.Select((cell, index) => (cell: cell as IHtmlTableHeaderCellElement, index)))
            .Where(tuple => tuple.cell is not null)
            .ToImmutableMap(tuple => tuple.index, tuple => tuple.cell!);
        if (headers is not null)
        {
            return new(headers);
        }
        return null;
    }

    public static TableView ToView(this IHtmlTableElement table)
    {
        var headers = table.GetHeadersWithIndexOrDefault() ?? throw new InvalidOperationException("No thead found");
        return new(headers, table);
    }

    public static RowsView ToRowsView(this IHtmlTableElement table, int rowIndex)
    {
        return new(table, rowIndex);
    }
}

public sealed class TableHeaders
{
    private readonly IReadOnlyDictionary<int, ImmutableArray<IHtmlTableHeaderCellElement>> _cells;

    internal TableHeaders(IReadOnlyDictionary<int, ImmutableArray<IHtmlTableHeaderCellElement>> headers)
    {
        _cells = headers;
    }

    public IReadOnlyDictionary<int, ImmutableArray<IHtmlTableHeaderCellElement>> Cells => _cells;

    public IEnumerable<(int Index, IHtmlTableHeaderCellElement Cell)> GetIndices(Func<IHtmlTableHeaderCellElement, bool> headerSelector)
    {
        return _cells.SelectTruthy(headers => ((int, IHtmlTableHeaderCellElement)?)(headers.Value.FirstOrDefault(headerSelector) is { } header ? (headers.Key, header) : null));
    }

    public IEnumerable<(int Index, IHtmlTableHeaderCellElement Cell)> GetIndices(ReadOnlyMemory<char> headerName, StringComparison? stringComparison = null)
    {
        return GetIndices(header => header.TextContent.AsSpan().Equals(headerName.Span, stringComparison ?? StringComparison.CurrentCultureIgnoreCase));
    }
    public IEnumerable<(int Index, IHtmlTableHeaderCellElement Cell)> GetIndices(string headerName, StringComparison? stringComparison = null)
    {
        return GetIndices(headerName.AsMemory(), stringComparison);
    }
}

public sealed class TableView(TableHeaders tableHeaders, IHtmlTableElement tableElement)
{
    public TableHeaders Headers => tableHeaders;
    public TableColumns Columns => new(tableHeaders.GetIndices(static _ => true).ToImmutableArray(), tableElement);

    public TableColumns this[Func<TableHeaders, IEnumerable<(int, IHtmlTableHeaderCellElement)>> filterHeaders] => new(filterHeaders(tableHeaders).ToImmutableArray(), tableElement);
    public TableColumns this[string headerName, StringComparison? stringComparison = null] => this[col => col.GetIndices(headerName, stringComparison)];
    public TableColumns this[ReadOnlyMemory<char> headerName, StringComparison? stringComparison = null] => this[col => col.GetIndices(headerName, stringComparison)];
}

public sealed class TableColumns(ImmutableArray<(int Index, IHtmlTableHeaderCellElement Cell)> cellsWithIndex, IHtmlTableElement tableElement) : IEnumerable<IEnumerable<IHtmlTableCellElement>>
{
    public IEnumerator<IEnumerable<IHtmlTableCellElement>> GetEnumerator()
    {
        return tableElement.Bodies
            .SelectMany(body => body.Rows)
            .Select(row => row.Cells
                .Where((cell, index) => cellsWithIndex
                    .Any(tuple => index == tuple.Index)
                )
            )
            .GetEnumerator();
    }

    public IEnumerable<IEnumerable<string>> SelectText()
    {
        return this.Select(cells => cells.Select(cell => cell.TextContent));
    }

    public IEnumerable<IEnumerable<T>> SelectText<T>(Func<string, T> textSelector)
    {
        return this.Select(cells => cells.Select(cell => textSelector(cell.TextContent)));
    }

#pragma warning disable CA1720
    public TableColumn Single()
#pragma warning restore CA1720
    {
        return new(this, cells => cells.Single());
    }

    public TableColumn First()
    {
        return new(this, cells => cells.First());
    }


    public MaybeTableColumn SingleOrDefault()
    {
        return new(this, cells => cells.FirstOrDefault());
    }

    public MaybeTableColumn FirstOrDefault()
    {
        return new(this, cells => cells.FirstOrDefault());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class TableColumn(TableColumns filter, Func<IEnumerable<IHtmlTableCellElement>, IHtmlTableCellElement> columnPredicate) : IEnumerable<IHtmlTableCellElement>
{
    public IEnumerable<string> SelectText()
    {
        return this.Select(cell => cell.TextContent);
    }

    public IEnumerable<T> SelectText<T>(Func<string, T> textSelector)
    {
        return SelectText().Select(textSelector);
    }

    public IEnumerator<IHtmlTableCellElement> GetEnumerator()
    {
        return filter.Select(columnPredicate).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class MaybeTableColumn(TableColumns filter, Func<IEnumerable<IHtmlTableCellElement>, IHtmlTableCellElement?> columnPredicate) : IEnumerable<IHtmlTableCellElement?>
{
    public IEnumerable<string?> SelectText()
    {
        return this.Select(cell => cell?.TextContent);
    }

    public IEnumerable<T?> SelectText<T>(Func<string, T> textSelector, T? defaultValue)
        where T : class
    {
        return this.Select(cell => cell is null || string.IsNullOrEmpty(cell.TextContent) ? defaultValue : textSelector(cell.TextContent));
    }
    public IEnumerable<T?> SelectText<T>(Func<string, T> textSelector, T? defaultValue)
        where T : struct
    {
        return this.Select(cell => cell is null || string.IsNullOrEmpty(cell.TextContent) ? defaultValue : textSelector(cell.TextContent));
    }
    public IEnumerator<IHtmlTableCellElement?> GetEnumerator()
    {
        return filter.Select(columnPredicate).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class RowsView(IHtmlTableElement table, int columnIndex)
{
    public IEnumerable<IHtmlTableCellElement> Headers => table.Rows.Select(row => row.Cells[columnIndex]);

    public TableRows this[Func<IHtmlTableCellElement, bool> cellPredicate] => new(table.Rows.Select((row, index) => (index, cell: row.Cells[columnIndex])).Where(tuple => cellPredicate(tuple.cell)).ToImmutableArray(), table, columnIndex);
    public TableRows this[ReadOnlyMemory<char> textContent, StringComparison? stringComparison = null] => this[cell => cell.TextContent.AsSpan().Equals(textContent.Span, stringComparison ?? StringComparison.CurrentCultureIgnoreCase)];
    public TableRows this[string textContent, StringComparison? stringComparison = null] => this[textContent.AsMemory(), stringComparison];
}

public sealed class TableRows(ImmutableArray<(int Index, IHtmlTableCellElement Cell)> cellsWithIndex, IHtmlTableElement tableElement, int columnIndex) : IEnumerable<IEnumerable<IHtmlTableCellElement>>
{
    public IEnumerator<IEnumerable<IHtmlTableCellElement>> GetEnumerator()
    {
        return tableElement.Rows
            .Select((row, index) => (index, row))
            .Where(tuple => cellsWithIndex.Any(t2 => t2.Index == tuple.index))
            .Select(tuple => tuple.row)
            .Select(row => row.Cells
                .Select((cell, index) => (index, cell))
                .Where(tuple => tuple.index != columnIndex)
                .Select(tuple => tuple.cell)
            ).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
