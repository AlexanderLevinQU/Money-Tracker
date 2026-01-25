namespace MoneyTracker.UI.Services.Interfaces;

public interface IDialogService
{
    Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}
