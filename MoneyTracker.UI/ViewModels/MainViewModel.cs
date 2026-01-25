using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyTracker.Models;
using MoneyTracker.UI.Services;
using MoneyTracker.UI.Enums;

namespace MoneyTracker.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly IDialogService _dialogService;

    public DashboardViewModel Dashboard { get; }

    [ObservableProperty]
    private List<Profile> profiles = new();

    [ObservableProperty]
    private Profile? selectedProfile;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int currentProfileId;

    [ObservableProperty]
    private PeriodType currentPeriod = PeriodType.Month;

    public Action? OnAddTransactionRequest;

    public MainViewModel(IDialogService? dialogService = null)
    {
        _apiService = new ApiService();
        _dialogService = dialogService ?? new DialogService();
        Dashboard = new DashboardViewModel();
    }

    [RelayCommand]
    public async Task LoadProfiles()
    {
        IsLoading = true;
        MoneyTracker.UI.Services.Logger.Log("LoadProfiles: Starting API call...");
        try
        {
            // remember previous selection to avoid resetting when navigating back
            var previousSelectedId = SelectedProfile?.Id;

            Profiles = await _apiService.GetProfilesAsync();
            MoneyTracker.UI.Services.Logger.Log($"LoadProfiles: API returned {Profiles.Count} profiles");
            if (Profiles.Count > 0)
            {
                if (previousSelectedId != null && Profiles.Any(p => p.Id == previousSelectedId.Value))
                {
                    SelectedProfile = Profiles.First(p => p.Id == previousSelectedId.Value);
                }
                else
                {
                    SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles[0];
                }
                StatusMessage = $"Loaded {Profiles.Count} profile(s)";
                MoneyTracker.UI.Services.Logger.Log($"LoadProfiles: SelectedProfile set to {SelectedProfile?.Name}");
            }
            else
            {
                StatusMessage = "No profiles found";
                MoneyTracker.UI.Services.Logger.Log("LoadProfiles: No profiles returned from API");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Profile name cannot be empty";
            return;
        }

        IsLoading = true;
        try
        {
            var newProfile = new Profile
            {
                Name = name,
                Description = $"Profile created on {DateTime.Now:g}",
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdProfile = await _apiService.CreateProfileAsync(newProfile);
            if (createdProfile != null)
            {
                Profiles.Add(createdProfile);
                StatusMessage = $"Profile '{name}' created successfully";
            }
            else
            {
                StatusMessage = "Failed to create profile";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void AddTransaction()
    {
        if (SelectedProfile != null)
        {
            CurrentProfileId = SelectedProfile.Id;
            OnAddTransactionRequest?.Invoke();
        }
    }

    [RelayCommand]
    public async Task NewProfile()
    {
        var name = await _dialogService.PromptAsync("New Profile", "Enter profile name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        await CreateProfileCommand.ExecuteAsync(name);
        await LoadProfilesCommand.ExecuteAsync(null);
    }
    [RelayCommand]
    public async Task RemoveProfile()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "No profile selected";
            return;
        }

        var confirm = await _dialogService.ConfirmAsync("Delete Profile", $"Delete profile '{SelectedProfile.Name}' and all its data?");
        if (!confirm) return;

        IsLoading = true;
        try
        {
            var id = SelectedProfile.Id;
            var ok = await _apiService.DeleteProfileAsync(id);
            if (!ok)
            {
                StatusMessage = "Failed to delete profile";
                return;
            }

            Profiles.RemoveAll(p => p.Id == id);
            StatusMessage = "Profile deleted";
            SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles.FirstOrDefault();

            if (SelectedProfile != null)
            {
                CurrentProfileId = SelectedProfile.Id;
                _ = Dashboard.LoadDashboardCommand.ExecuteAsync(SelectedProfile.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // When SelectedProfile changes (generated by ObservableProperty) this partial is invoked.
    // Use it to update CurrentProfileId and load the dashboard for the selected profile.
    partial void OnSelectedProfileChanged(Profile? value)
    {
        if (value != null)
        {
            CurrentProfileId = value.Id;
            _ = Dashboard.LoadDashboardCommand.ExecuteAsync(value.Id);
        }
    }

    [RelayCommand]
    public void ChangePeriod(string period)
    {
        if (!Enum.TryParse<PeriodType>(period, ignoreCase: true, out var parsed))
        {
            parsed = PeriodType.Month;
        }
        CurrentPeriod = parsed;
    }
}
