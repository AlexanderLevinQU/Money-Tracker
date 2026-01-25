using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyTracker.Models;
// using MoneyTracker.UI.Enums; replaced by CategoryType from MoneyTracker.Models
using MoneyTracker.UI.Services;

namespace MoneyTracker.UI.ViewModels;

public partial class AddTransactionViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public IAsyncRelayCommand SaveTransactionCommand { get; }

    [ObservableProperty]
    private int profileId;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private DateTime transactionDate = DateTime.Now;

    [ObservableProperty]
    private CategoryType selectedType = CategoryType.Income;

    [ObservableProperty]
    private int selectedCategoryId;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private List<Category> categories = new();

    [ObservableProperty]
    private List<Category> filteredCategories = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isSuccess;

    public IAsyncRelayCommand AddCategoryCommand { get; }
    public IAsyncRelayCommand RemoveCategoryCommand { get; }

    private IDialogService _dialogService = new DialogService();

    public AddTransactionViewModel()
    {
        _apiService = new ApiService();
        SaveTransactionCommand = new AsyncRelayCommand(SaveTransaction);
        AddCategoryCommand = new AsyncRelayCommand(AddCategory);
        RemoveCategoryCommand = new AsyncRelayCommand(RemoveCategory);
    }
    private async Task AddCategory()
    {
        var name = await _dialogService.PromptAsync("Add Category", "Enter category name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var type = await _dialogService.PromptAsync("Category Type", "Enter type (Income/Expense):");
        if (!Enum.TryParse<CategoryType>(type, true, out var parsedType))
        {
            StatusMessage = "Invalid type. Must be Income or Expense.";
            return;
        }

        var category = new Category
        {
            Name = name,
            Type = parsedType
        };

        IsLoading = true;
        try
        {
            var created = await _apiService.CreateCategoryAsync(category);
            if (created != null)
            {
                Categories.Add(created);
                StatusMessage = $"Category '{name}' added.";
                UpdateFilteredCategories();
            }
            else
            {
                StatusMessage = "Failed to add category.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding category: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveCategory()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "No category selected.";
            return;
        }
        var confirm = await _dialogService.ConfirmAsync("Remove Category", $"Delete category '{SelectedCategory.Name}'?");
        if (!confirm) return;

        IsLoading = true;
        try
        {
            var ok = await _apiService.DeleteCategoryAsync(SelectedCategory.Id);
            if (ok)
            {
                Categories.Remove(SelectedCategory);
                StatusMessage = $"Category '{SelectedCategory.Name}' removed.";
                UpdateFilteredCategories();
                SelectedCategory = FilteredCategories.FirstOrDefault();
                SelectedCategoryId = SelectedCategory?.Id ?? 0;
            }
            else
            {
                StatusMessage = "Failed to remove category.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing category: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadCategories(int profileId)
    {
        ProfileId = profileId;
        IsLoading = true;
        try
        {
            var allCategories = await _apiService.GetCategoriesAsync();
            Categories = allCategories;
            // set a sensible default selected category if available
            if (Categories.Count > 0)
            {
                // Update filtered view based on current SelectedType
                UpdateFilteredCategories();
                SelectedCategory = FilteredCategories.FirstOrDefault() ?? Categories.First();
                SelectedCategoryId = SelectedCategory?.Id ?? 0;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectType(string type)
    {
        if (Enum.TryParse<CategoryType>(type, true, out var parsed))
            SelectedType = parsed;
        else
            SelectedType = CategoryType.Income;
        UpdateFilteredCategories();
    }

    partial void OnSelectedTypeChanged(CategoryType value)
    {
        UpdateFilteredCategories();
    }

    partial void OnCategoriesChanged(List<Category> value)
    {
        UpdateFilteredCategories();
    }

    private void UpdateFilteredCategories()
    {
        try
        {
            if (Categories == null) Categories = new List<Category>();
            var filtered = Categories
                .Where(c => c.Type == SelectedType)
                .ToList();

            // Do not fall back to showing all categories; keep filtered list strictly by Type

            FilteredCategories = filtered;

            // Ensure SelectedCategory is valid for the current filtered list
            if (FilteredCategories.Count > 0)
            {
                if (SelectedCategory == null || !FilteredCategories.Any(c => c.Id == SelectedCategory.Id))
                {
                    SelectedCategory = FilteredCategories.First();
                    SelectedCategoryId = SelectedCategory.Id;
                }
            }
            else
            {
                SelectedCategory = null;
                SelectedCategoryId = 0;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error filtering categories: {ex.Message}";
        }
    }

    public async Task SaveTransaction()
    {
        // parse amount from bound text if provided
        if (!string.IsNullOrWhiteSpace(AmountText) && decimal.TryParse(AmountText, out var parsed))
        {
            Amount = parsed;
        }

        if (Amount <= 0)
        {
            StatusMessage = "Amount must be greater than 0";
            IsSuccess = false;
            return;
        }

        if (SelectedCategory == null && SelectedCategoryId == 0)
        {
            StatusMessage = "Please select a category";
            IsSuccess = false;
            return;
        }

        IsLoading = true;
        try
        {
            Logger.Log($"SaveTransaction: ProfileId={ProfileId}, CategoryId={SelectedCategoryId}, Amount={Amount}, Type={SelectedType}");

            // Ensure the transaction Type matches the selected category's Type
            var resolvedCategory = SelectedCategory ?? Categories.FirstOrDefault(c => c.Id == SelectedCategoryId);
            if (resolvedCategory != null)
            {
                SelectedCategoryId = resolvedCategory.Id;
                // resolvedCategory.Type is CategoryType already
                SelectedType = resolvedCategory.Type;
                Logger.Log($"SaveTransaction: resolved Type from Category -> {SelectedType}");
            }

            var transaction = new Transaction
            {
                ProfileId = ProfileId,
                CategoryId = SelectedCategoryId,
                Amount = Amount,
                Description = Description,
                Date = TransactionDate,
                Type = SelectedType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _apiService.CreateTransactionAsync(transaction);
            if (result != null)
            {
                Logger.Log($"SaveTransaction: success Id={result.Id} Amount={result.Amount}");
                StatusMessage = "Transaction saved successfully!";
                IsSuccess = true;
                ResetForm();
            }
            else
            {
                Logger.Log("SaveTransaction: success but response null");
                StatusMessage = "Transaction saved but could not load response details";
                IsSuccess = true;
                ResetForm();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Exception in SaveTransaction");
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetForm()
    {
        Amount = 0;
        Description = string.Empty;
        TransactionDate = DateTime.Now;
    }
}
