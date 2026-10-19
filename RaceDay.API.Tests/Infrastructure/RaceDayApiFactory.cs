using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;

namespace RaceDay.API.Tests.Infrastructure;

/// <summary>
/// Hosts the real API (routing, session, filters, validation) in memory for tests.
/// SQL Server is swapped for a fresh EF Core in-memory database per factory, and
/// startup migration/seeding is switched off so each test controls its own data.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:MigrateAndSeed", "false");

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL Server registration made in Program.cs
            var sqlServerRegistrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                            d.ServiceType == typeof(IDbContextOptionsConfiguration<RaceDayDbContext>))
                .ToList();
            foreach (var descriptor in sqlServerRegistrations)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Runs an action against the test database (for arranging data or asserting what was saved).</summary>
    public async Task<T> WithDbAsync<T>(Func<RaceDayDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await action(db);
    }
}
