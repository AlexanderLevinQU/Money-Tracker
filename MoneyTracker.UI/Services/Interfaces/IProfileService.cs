using MoneyTracker.Models;

namespace MoneyTracker.UI.Services.Interfaces;

public interface IProfileService
{
	Task<List<Profile>> GetProfilesAsync();
	Task<Profile?> GetProfileAsync(int id);
	Task<Profile?> CreateProfileAsync(Profile profile);
	Task<bool> DeleteProfileAsync(int id);
}
