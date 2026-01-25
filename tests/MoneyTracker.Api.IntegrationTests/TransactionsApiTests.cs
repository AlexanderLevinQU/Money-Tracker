using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Api;
using MoneyTracker.Models;

namespace MoneyTracker.Api.IntegrationTests;

public class TransactionsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionsApiTests(WebApplicationFactory<Program> factory)
    {
        // Use in-memory DB to isolate tests
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MoneyTracker.Models.MoneyTrackerContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<MoneyTracker.Models.MoneyTrackerContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_Transactions_{System.Guid.NewGuid()}");
                });
            });
        });
    }

    [Fact]
    public async Task CreateAndGetTransaction_Works()
    {
        var client = _factory.CreateClient();

        // create a profile and categories exist from seed data; create a transaction DTO
        var dto = new
        {
            ProfileId = 1,
            CategoryId = 1,
            Amount = 123.45m,
            Description = "Integration test",
            Date = System.DateTime.UtcNow,
            Type = CategoryType.Income
        };

        var postResp = await client.PostAsJsonAsync("/api/transactions", dto);
        postResp.EnsureSuccessStatusCode();

        var createdJson = await postResp.Content.ReadAsStringAsync();
        Assert.Contains("id", createdJson.ToLower());

        // get list and ensure at least one transaction is returned
        var getResp = await client.GetAsync("/api/transactions");
        getResp.EnsureSuccessStatusCode();
        var listJson = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("integration test", listJson);
    }
}
