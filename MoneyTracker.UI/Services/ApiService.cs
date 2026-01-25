using MoneyTracker.Models;
using MoneyTracker.UI.Models;
using MoneyTracker.UI.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace MoneyTracker.UI.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5027/api";
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public ApiService()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        _httpClient = new HttpClient(handler);
    }

    public async Task<List<Profile>> GetProfilesAsync()
    {
        try
        {
            Services.Logger.Log($"GetProfilesAsync: Making request to {_baseUrl}/profiles");
            var response = await _httpClient.GetAsync($"{_baseUrl}/profiles");
            Services.Logger.Log($"GetProfilesAsync: Response status: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Services.Logger.Log($"GetProfilesAsync: Response JSON: {json}");
            var result = JsonSerializer.Deserialize<List<Profile>>(json, _jsonOptions) ?? new();
            Services.Logger.Log($"GetProfilesAsync: Deserialized {result.Count} profiles");
            return result;
        }
        catch (Exception ex)
        {
            Services.Logger.Log($"GetProfilesAsync Error: {ex.Message}");
            Services.Logger.LogException(ex, "GetProfilesAsync Exception");
            return new();
        }
    }

    public async Task<Profile?> GetProfileAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Profile>($"{_baseUrl}/profiles/{id}");
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error getting profile");
            return null;
        }
    }

    public async Task<Profile?> CreateProfileAsync(Profile profile)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/profiles", profile);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error creating profile");
            return null;
        }
    }

    // Categories
    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/categories");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Category>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error getting categories");
            return new();
        }
    }

    public async Task<Category?> CreateCategoryAsync(Category category)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/categories", category);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Category>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error creating category");
            return null;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/categories/{id}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error deleting category");
            return false;
        }
    }

    // Transactions
    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/transactions");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TransactionDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error getting transactions");
            return new();
        }
    }

    public async Task<TransactionDto?> GetTransactionAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TransactionDto>($"{_baseUrl}/transactions/{id}");
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error getting transaction");
            return null;
        }
    }

    public async Task<TransactionDto?> CreateTransactionAsync(Transaction transaction)
    {
        try
        {
            Services.Logger.Log($"Creating transaction via API: {transaction.Type} - ${transaction.Amount}");
                // Post a DTO to the API to avoid sending navigation properties
                var dto = new TransactionDto
                {
                    ProfileId = transaction.ProfileId,
                    CategoryId = transaction.CategoryId,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    Date = transaction.Date,
                    Type = transaction.Type
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/transactions", dto);

            var json = await response.Content.ReadAsStringAsync();
            Services.Logger.Log($"API Response Status: {response.StatusCode}");
            Services.Logger.Log($"API Response Body: {json}");

            if (!response.IsSuccessStatusCode)
            {
                Services.Logger.Log($"API Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var result = JsonSerializer.Deserialize<TransactionDto>(json, _jsonOptions);
            Services.Logger.Log($"Deserialized result: Id={result?.Id}");
            return result;
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Exception in CreateTransactionAsync");
            return null;
        }
    }

    public async Task<bool> DeleteTransactionAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/transactions/{id}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error deleting transaction");
            return false;
        }
    }

    public async Task<bool> DeleteProfileAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/profiles/{id}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Services.Logger.LogException(ex, "Error deleting profile");
            return false;
        }
    }
}

