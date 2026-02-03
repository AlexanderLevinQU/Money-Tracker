using MoneyTracker.UI.Enums;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services.Filters;

public class TransactionFilterFactory : ITransactionFilterFactory
{
    public ITransactionFilter Create(TransactionFilter filter, string category)
    {
        switch (filter)
        {
            case TransactionFilter.Expenses:
                return new ExpensesTransactionFilter();
            case TransactionFilter.Income:
                return new IncomeTransactionFilter();
            case TransactionFilter.Category:
                return new CategoryTransactionFilter(category);
            default:
                return new AllTransactionFilter();
        }
    }
}
