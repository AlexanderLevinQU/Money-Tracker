using Microsoft.Maui.Controls;

namespace MoneyTracker.UI.Services;

public class DialogService : IDialogService
{
    public async Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        var page = Application.Current?.MainPage;
        if (page == null) return null;
        return await page.DisplayPromptAsync(title, message, accept, cancel);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        var page = Application.Current?.MainPage;
        if (page == null) return false;
        return await page.DisplayAlert(title, message, accept, cancel);
    }
}
