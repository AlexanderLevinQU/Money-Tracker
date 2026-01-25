using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Models;

public class Profile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDefault { get; set; } = false;

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Description))
            {
                return $"{Name} - {Description}";
            }
            else if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }
            else if (!string.IsNullOrWhiteSpace(Description))
            {
                return Description;
            }
            else
            {
                return $"Profile {Id}";
            }
        }
    }

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
