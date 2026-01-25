using Microsoft.EntityFrameworkCore;

namespace MoneyTracker.Models;

public class MoneyTrackerContext : DbContext
{
    public MoneyTrackerContext(DbContextOptions<MoneyTrackerContext> options) : base(options) { }

    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Profile relationships
        // Categories are now global, not tied to Profile

        modelBuilder.Entity<Profile>()
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Profile)
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Category relationships
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Transactions)
            .WithOne(t => t.Category)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enum conversion for CategoryType
        modelBuilder.Entity<Category>()
            .Property(c => c.Type)
            .HasConversion<string>();

        // Indexes
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.ProfileId, t.Date });

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name);

        // Enum conversion for Transaction.Type
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Type)
            .HasConversion<string>();
    }
}
