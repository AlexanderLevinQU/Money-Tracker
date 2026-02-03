
using MoneyTracker.Models;

namespace MoneyTracker.UI.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserAsync(int id);
        Task<User?> CreateUserAsync(User User);
        Task<bool> DeleteUserAsync(int id);
    }
}
