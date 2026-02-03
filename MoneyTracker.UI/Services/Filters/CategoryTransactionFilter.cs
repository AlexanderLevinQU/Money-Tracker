using MoneyTracker.UI.Models;
using System;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services.Filters;

public class CategoryTransactionFilter : ITransactionFilter
{
    private readonly string _category;
    public CategoryTransactionFilter(string category) => _category = category ?? string.Empty;

    public bool Matches(TransactionDto t)
    {
        if (t == null) return false;
        if (string.IsNullOrWhiteSpace(_category)) return true;
        return string.Equals(t.CategoryName, _category, StringComparison.OrdinalIgnoreCase);
    }
}
