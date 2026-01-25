using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MoneyTracker.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Profile")]
    public int ProfileId { get; set; }

    [ForeignKey("Category")]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.Now;

    [Required]
    public CategoryType Type { get; set; } = CategoryType.Expense; // Income or Expense

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [JsonIgnore]
    public Profile Profile { get; set; } = null!;

    [JsonIgnore]
    public Category Category { get; set; } = null!;

    [NotMapped]
    public string CategoryName => Category?.Name ?? string.Empty;
}
