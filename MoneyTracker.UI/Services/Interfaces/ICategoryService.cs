using MoneyTracker.Models;

namespace MoneyTracker.UI.Services.Interfaces;

public interface ICategoryService
{
	Task<List<Category>> GetCategoriesAsync();
	Task<Category?> CreateCategoryAsync(Category category);
	Task<bool> DeleteCategoryAsync(int id);
}
