using System;
using System.Collections.Generic;
using System.Linq;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services.Paging;

public class SimplePagingService<T> : IPagingService<T>
{
    private List<T> _items = new();
    private int _pageSize = 1;
    private int _index = 0;

    public bool HasMore => _index < _items.Count;

    public void Reset(IEnumerable<T> items, int pageSize)
    {
        _items = (items ?? Enumerable.Empty<T>()).ToList();
        _pageSize = Math.Max(1, pageSize);
        _index = 0;
    }

    public List<T> GetNextPage()
    {
        if (!HasMore) return new List<T>();
        var page = _items.Skip(_index).Take(_pageSize).ToList();
        _index += page.Count;
        return page;
    }
}
