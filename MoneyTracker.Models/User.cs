using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MoneyTracker.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    [Required]
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [JsonIgnore]
    public ICollection<Profile> Profiles { get; set; } = new List<Profile>();

}