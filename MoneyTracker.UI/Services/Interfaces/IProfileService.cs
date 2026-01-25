using MoneyTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTracker.UI.Services.Interfaces;

public interface IProfileService
{
	Task<List<Profile>> GetProfilesAsync();
	Task<Profile?> GetProfileAsync(int id);
	Task<Profile?> CreateProfileAsync(Profile profile);
	Task<bool> DeleteProfileAsync(int id);
}
