using MoneyTracker.UI.Enums;

namespace MoneyTracker.UI.Services.Interfaces;

public interface ITransactionFilterFactory
{
    ITransactionFilter Create(TransactionFilter filter, string category);
}
