using MoneyTracker.Models;
using MoneyTracker.UI.Models;

namespace MoneyTracker.UI.Services.Interfaces;

public interface ITransactionService
{
	Task<List<TransactionDto>> GetTransactionsAsync();
	Task<TransactionDto?> GetTransactionAsync(int id);
	Task<TransactionDto?> CreateTransactionAsync(Transaction transaction);
	Task<bool> DeleteTransactionAsync(int id);
}
