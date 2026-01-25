using MoneyTracker.UI.Services;

namespace MoneyTracker.UI;

public partial class App : Application
{
	public App()
	{
		// Initialize centralized logger as early as possible
		try
		{
			Logger.Init();
			Logger.Log("App constructor called - Logger initialized");
		}
		catch
		{
			// Swallow exceptions here to avoid crashing startup; MainPage already has a secondary fallback
		}

		InitializeComponent();
		// Create a single MainViewModel for the application so selected profile
		// and dashboard state persist across navigation and page reloads.
		var dialogService = new Services.DialogService();
		MainViewModel = new ViewModels.MainViewModel(dialogService);
	}

	public ViewModels.MainViewModel MainViewModel { get; }

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new MainPage()));
	}
}