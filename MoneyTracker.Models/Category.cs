using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MoneyTracker.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CategoryType Type { get; set; } = CategoryType.Expense; // Income or Expense

    public bool IsCustom { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Category {Id}" : Name;

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
