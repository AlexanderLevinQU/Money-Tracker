using MoneyTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTracker.UI.Services.Interfaces;

public interface IApiService : IProfileService, ICategoryService, ITransactionService
{
    
}
