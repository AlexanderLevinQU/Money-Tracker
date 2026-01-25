using Microsoft.EntityFrameworkCore;
using MoneyTracker.Models;
using MoneyTracker.UI.Models;
using MoneyTracker.UI.Services.Interfaces;

namespace MoneyTracker.UI.Services;

public class EfDataService : IApiService
{
    private readonly MoneyTrackerContext _db;

    public EfDataService(MoneyTrackerContext db)
    {
        _db = db;
    }

    // Profiles
    public async Task<List<Profile>> GetProfilesAsync()
    {
        return await _db.Profiles.AsNoTracking().ToListAsync();
    }

    public async Task<Profile?> GetProfileAsync(int id)
    {
        return await _db.Profiles.FindAsync(id);
    }

    public async Task<Profile?> CreateProfileAsync(Profile profile)
    {
        _db.Profiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(int id)
    {
        var p = await _db.Profiles.FindAsync(id);
        if (p == null) return false;
        _db.Profiles.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    // Categories
    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _db.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category?> CreateCategoryAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return false;
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    // Transactions
    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        var txs = await _db.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .ToListAsync();

        return txs.Select(t => new TransactionDto
        {
            Id = t.Id,
            ProfileId = t.ProfileId,
            CategoryId = t.CategoryId,
            Amount = t.Amount,
            Description = t.Description,
            Date = t.Date,
            Type = t.Type,
            CategoryName = t.Category?.Name ?? string.Empty
        }).ToList();
    }

    public async Task<TransactionDto?> GetTransactionAsync(int id)
    {
        var t = await _db.Transactions.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return null;
        return new TransactionDto
        {
            Id = t.Id,
            ProfileId = t.ProfileId,
            CategoryId = t.CategoryId,
            Amount = t.Amount,
            Description = t.Description,
            Date = t.Date,
            Type = t.Type,
            CategoryName = t.Category?.Name ?? string.Empty
        };
    }

    public async Task<TransactionDto?> CreateTransactionAsync(Transaction transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        var t = await _db.Transactions.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == transaction.Id);
        if (t == null) return null;

        return new TransactionDto
        {
            Id = t.Id,
            ProfileId = t.ProfileId,
            CategoryId = t.CategoryId,
            Amount = t.Amount,
            Description = t.Description,
            Date = t.Date,
            Type = t.Type,
            CategoryName = t.Category?.Name ?? string.Empty
        };
    }

    public async Task<bool> DeleteTransactionAsync(int id)
    {
        var t = await _db.Transactions.FindAsync(id);
        if (t == null) return false;
        _db.Transactions.Remove(t);
        await _db.SaveChangesAsync();
        return true;
    }
}
