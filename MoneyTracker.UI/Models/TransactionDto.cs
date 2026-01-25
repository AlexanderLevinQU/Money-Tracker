using System;
using MoneyTracker.Models;

namespace MoneyTracker.UI.Models;

public class TransactionDto
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public CategoryType Type { get; set; } = CategoryType.Income;
    public string CategoryName { get; set; } = string.Empty;
}
