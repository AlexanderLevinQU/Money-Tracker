using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Api;

namespace MoneyTracker.Api.IntegrationTests;

public class CategoriesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CategoriesApiTests(WebApplicationFactory<Program> factory)
    {
        // Use an in-memory database for isolation during tests
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the registered DbContext with an in-memory one
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MoneyTracker.Models.MoneyTrackerContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<MoneyTracker.Models.MoneyTrackerContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_Categories");
                });
            });
        });
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/categories");
        response.EnsureSuccessStatusCode();
    }
}
