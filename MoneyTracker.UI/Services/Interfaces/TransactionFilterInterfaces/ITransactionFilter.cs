using MoneyTracker.UI.Models;

namespace MoneyTracker.UI.Services.Interfaces;

public interface ITransactionFilter
{
    bool Matches(TransactionDto t);
}
