using MoneyTracker.Models;
using MoneyTracker.UI.Models;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services.Filters;

public class ExpensesTransactionFilter : ITransactionFilter
{
    public bool Matches(TransactionDto t) => t?.Type == CategoryType.Expense;
}
