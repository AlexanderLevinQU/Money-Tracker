using MoneyTracker.UI.ViewModels;
using MoneyTracker.Models;

namespace MoneyTracker.UI;

public partial class MainPage : ContentPage
{
	private MainViewModel _viewModel => BindingContext as MainViewModel;

	public MainPage()
	{
		InitializeComponent();
		// Use the app-wide MainViewModel instance so selected profile is preserved
		// when navigating between pages.
		BindingContext = (Application.Current as App)?.MainViewModel ?? new MainViewModel();

		if (_viewModel != null)
		{
			_viewModel.OnAddTransactionRequest += async () =>
			{
				var profileId = _viewModel.CurrentProfileId;
				await Navigation.PushAsync(new Views.AddTransactionPage(profileId));
			};
		}
		Loaded += MainPage_Loaded;
	}

	private async void MainPage_Loaded(object? sender, EventArgs e)
	{
		if (_viewModel != null)
		{
			await _viewModel.LoadProfilesCommand.ExecuteAsync(null);
		}
	}

	private void UpdateButtonStyles(Button clickedButton)
	{
	}
}

