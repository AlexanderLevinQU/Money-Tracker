using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace MoneyTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiScope")]
public class TransactionsController : ControllerBase
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public CategoryType Type { get; set; } = CategoryType.Expense;
        public string CategoryName { get; set; } = string.Empty;
    }


    private readonly MoneyTrackerContext _context;

    public TransactionsController(MoneyTrackerContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                ProfileId = t.ProfileId, // keep for DTO, but category is now global
                CategoryId = t.CategoryId,
                Amount = t.Amount,
                Description = t.Description,
                Date = t.Date,
                Type = t.Type,
                CategoryName = t.Category.Name
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        return new TransactionDto
        {
            Id = transaction.Id,
            ProfileId = transaction.ProfileId, // keep for DTO, but category is now global
            CategoryId = transaction.CategoryId,
            Amount = transaction.Amount,
            Description = transaction.Description,
            Date = transaction.Date,
            Type = transaction.Type,
            CategoryName = transaction.Category.Name
        };
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(TransactionDto dto)
    {
        var transaction = new Transaction
        {
            ProfileId = dto.ProfileId, // keep for DTO, but category is now global
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            Description = dto.Description,
            Date = dto.Date,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();

        // Return location only to avoid async JSON streaming issues with the TestHost pipewriter
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, TransactionDto dto)
    {
        var existing = await _context.Transactions.FindAsync(id);
        if (existing == null) return NotFound();

        // Map allowed fields from DTO
        existing.ProfileId = dto.ProfileId;
        existing.CategoryId = dto.CategoryId;
        existing.Amount = dto.Amount;
        existing.Description = dto.Description;
        existing.Date = dto.Date;

        existing.Type = dto.Type;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TransactionExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool TransactionExists(int id)
    {
        return _context.Transactions.Any(e => e.Id == id);
    }
}

