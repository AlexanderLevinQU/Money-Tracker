using Microsoft.EntityFrameworkCore;
using MoneyTracker.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MoneyTracker.Api.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    // Also enable NewtonsoftJson formatter for compatibility with TestHost serialization
    .AddNewtonsoftJson();

// Prefer Newtonsoft.Json formatter (remove System.Text.Json output formatter) to avoid TestHost PipeWriter issues
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    var stj = options.OutputFormatters.OfType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>().FirstOrDefault();
    if (stj != null) options.OutputFormatters.Remove(stj);
});
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure SQLite database
var dbPath = Path.Combine(AppContext.BaseDirectory, "moneytracker.db");
builder.Services.AddDbContext<MoneyTrackerContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Auth: Microsoft Entra (Azure AD or B2C) - prefer AzureAd (single-tenant) with fallback to AzureAdB2C
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var azureAd = builder.Configuration.GetSection("AzureAd");
var authority = azureAd.GetValue<string>("Authority");
var audience = azureAd.GetValue<string>("Audience");

// Fallback to AzureAdB2C section if AzureAd is not provided
if (string.IsNullOrEmpty(authority) && string.IsNullOrEmpty(audience))
{
    var b2c = builder.Configuration.GetSection("AzureAdB2C");
    authority = b2c.GetValue<string>("Authority");
    audience = b2c.GetValue<string>("Audience");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(authority)) options.Authority = authority;
        if (!string.IsNullOrEmpty(audience)) options.Audience = audience;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(authority),
            ValidateAudience = !string.IsNullOrEmpty(audience)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("ApiScope", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim(c => (c.Type == "scp" || c.Type == "scope") && c.Value.Split(' ').Contains("access_as_user"))
    ));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed the database
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MoneyTrackerContext>();
    context.Database.EnsureCreated();

    // Seed sample data
    if (!context.Profiles.Any())
    {
        var profile = new Profile
        {
            Name = "Personal",
            Description = "Personal finances",
            IsDefault = true,
            CreatedAt = DateTime.Now
        };
        context.Profiles.Add(profile);
        context.SaveChanges();

        // Add income categories
        var incomeCategories = new[]
        {
            new Category { Name = "Salary", Type = CategoryType.Income, CreatedAt = DateTime.Now },
            new Category { Name = "Bonus", Type = CategoryType.Income, CreatedAt = DateTime.Now },
            new Category { Name = "Freelance", Type = CategoryType.Income, CreatedAt = DateTime.Now },
            new Category { Name = "Investment", Type = CategoryType.Income, CreatedAt = DateTime.Now }
        };

        // Add expense categories
        var expenseCategories = new[]
        {
            new Category { Name = "Housing", Type = CategoryType.Expense, CreatedAt = DateTime.Now },
            new Category { Name = "Utilities", Type = CategoryType.Expense, CreatedAt = DateTime.Now },
            new Category { Name = "Food", Type = CategoryType.Expense, CreatedAt = DateTime.Now },
            new Category { Name = "Transportation", Type = CategoryType.Expense, CreatedAt = DateTime.Now },
            new Category { Name = "Entertainment", Type = CategoryType.Expense, CreatedAt = DateTime.Now }
        };

        context.Categories.AddRange(incomeCategories);
        context.Categories.AddRange(expenseCategories);
        context.SaveChanges();

        // Add sample transactions
        var transactions = new[]
        {
            new Transaction
            {
                ProfileId = profile.Id,
                CategoryId = incomeCategories[0].Id,
                Amount = 5000,
                Description = "Monthly Salary",
                Date = DateTime.Now,
                Type = CategoryType.Income,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Transaction
            {
                ProfileId = profile.Id,
                CategoryId = expenseCategories[0].Id,
                Amount = 1500,
                Description = "Rent",
                Date = DateTime.Now,
                Type = CategoryType.Expense,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new Transaction
            {
                ProfileId = profile.Id,
                CategoryId = expenseCategories[2].Id,
                Amount = 200,
                Description = "Groceries",
                Date = DateTime.Now,
                Type = CategoryType.Expense,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        context.Transactions.AddRange(transactions);
        context.SaveChanges();
    }

    // Backfill: ensure existing profiles have the full set of default categories
    var defaultIncome = new[] { "Salary", "Bonus", "Freelance", "Investment" };
    var defaultExpense = new[] { "Housing", "Utilities", "Food", "Transportation", "Entertainment" };

    var allProfiles = context.Profiles.ToList();
    foreach (var p in allProfiles)
    {
            var existingNames = context.Categories
                .Select(c => c.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Category>();

        foreach (var name in defaultIncome)
        {
                if (!existingNames.Contains(name))
                {
                    toAdd.Add(new Category { Name = name, Type = CategoryType.Income, CreatedAt = DateTime.Now });
                }
        }

        foreach (var name in defaultExpense)
        {
                if (!existingNames.Contains(name))
                {
                    toAdd.Add(new Category { Name = name, Type = CategoryType.Expense, CreatedAt = DateTime.Now });
                }
        }

        if (toAdd.Count > 0)
        {
            context.Categories.AddRange(toAdd);
        }
    }

    context.SaveChanges();
}
*/

app.Run();

// Expose the implicit Program class for integration testing (WebApplicationFactory)
public partial class Program { }
