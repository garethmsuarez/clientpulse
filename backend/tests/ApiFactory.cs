using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ClientPulse.Tests;

// Runs the API in-process against a dedicated test database on the local
// Compose PostgreSQL. The test database is recreated once per test run;
// the development database is never touched.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseName = "clientpulse_tests";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__ClientPulseTests")
        ?? $"Host=localhost;Port=5433;Database={TestDatabaseName};Username=clientpulse;Password=clientpulse_dev_only";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ClientPulse", ConnectionString);

        // Tests never call a live AI provider, even if a developer's user
        // secrets select Anthropic. Tests that need provider behaviour swap in
        // a fake ICommunicationAnalyzer.
        builder.UseSetting("Analysis:Provider", "Fixture");
        builder.UseSetting("Anthropic:ApiKey", "");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientPulseDbContext>();

        // Guard: only ever reset the dedicated test database.
        var database = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database;
        if (database != TestDatabaseName)
        {
            throw new InvalidOperationException(
                $"Refusing to reset database '{database}'; tests must use '{TestDatabaseName}'.");
        }

        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                "Could not reach the test database. Start PostgreSQL with 'docker compose up -d postgres'.", ex);
        }
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
