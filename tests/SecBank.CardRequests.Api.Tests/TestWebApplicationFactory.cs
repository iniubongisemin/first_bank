using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SecBank.CardRequests.Api.Data;

namespace SecBank.CardRequests.Api.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSecurity:ApiKey"] = ApiKey
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CardRequestsDbContext>>();
            services.RemoveAll<CardRequestsDbContext>();
            _connection.Open();
            services.AddSingleton(_connection);
            services.AddDbContext<CardRequestsDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
