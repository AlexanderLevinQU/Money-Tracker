using System;

namespace MoneyTracker.UI.Models;

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
