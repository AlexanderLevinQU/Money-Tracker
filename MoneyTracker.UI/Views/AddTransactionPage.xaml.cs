using MoneyTracker.UI.ViewModels;
using MoneyTracker.Models;

namespace MoneyTracker.UI.Views;

public partial class AddTransactionPage : ContentPage
{
    private readonly AddTransactionViewModel _viewModel;

    public AddTransactionPage(int profileId, string initialType = "Income")
    {
        InitializeComponent();
        _viewModel = new AddTransactionViewModel();
        if (Enum.TryParse<CategoryType>(initialType, true, out var parsed))
            _viewModel.SelectedType = parsed;
        else
            _viewModel.SelectedType = CategoryType.Income;
        BindingContext = _viewModel;

        // Load categories for the profile when the page loads
        Loaded += async (s, e) => await _viewModel.LoadCategoriesCommand.ExecuteAsync(profileId);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
