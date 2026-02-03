using MoneyTracker.UI.Models;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services.Filters;

public class AllTransactionFilter : ITransactionFilter
{
    public bool Matches(TransactionDto t) => true;
}
