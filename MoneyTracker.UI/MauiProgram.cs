using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Models;
using MoneyTracker.UI.Services;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configure DI services
		ConfigureServices(builder);

		var app = builder.Build();

		// Always attempt to apply EF migrations on startup so new installs get the schema
		try
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "moneytracker.db");
			Logger.Log($"MoneyTracker DB path: {dbPath}");
			Logger.Log($"MoneyTracker DB exists: {File.Exists(dbPath)}");
			
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<MoneyTrackerContext>();
				db.Database.Migrate();
			}
		}
		catch (Exception ex)
		{
			Logger.LogException(ex, "Failed to ensure/create the SQLite database");
		}

		return app;
	}

	static void ConfigureServices(MauiAppBuilder builder)
	{
		// Configure SQLite DB in app data directory
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "moneytracker.db");
		builder.Services.AddDbContext<MoneyTrackerContext>(options =>
		{
			options.UseSqlite($"Data Source={dbPath}");
		});

		// Register EF-backed data service for the unified IApiService
		builder.Services.AddScoped<IApiService, EfDataService>();
	}
}
