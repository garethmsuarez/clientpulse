using ClientPulse.Api.Analysis;
using ClientPulse.Api.Clients;
using ClientPulse.Api.Communications;
using ClientPulse.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ClientPulse.Tests;

public static class TestClients
{
    public static FixtureScenario Scenario(string name) => FixtureScenarios.All.Single(s => s.Name == name);

    // Creates a client in the test database whose communications are copies
    // of a scenario's seed communications (with new database-generated ids),
    // so each test works on its own data.
    public static async Task<int> CreateAsync(ApiFactory factory, FixtureScenario? scenario)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientPulseDbContext>();

        var client = new Client
        {
            Name = $"Synthetic test client {Guid.NewGuid():N}",
            Industry = "Testing",
            AccountOwner = "Synthetic Owner",
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        if (scenario is not null)
        {
            db.Communications.AddRange(scenario.CommunicationSubjects
                .Select(subject => ClientPulseDbContext.SeedCommunications.Single(c => c.Subject == subject))
                .Select(seed => new Communication
                {
                    ClientId = client.Id,
                    Type = seed.Type,
                    OccurredAt = seed.OccurredAt,
                    Subject = seed.Subject,
                    Body = seed.Body,
                    CreatedAt = seed.CreatedAt,
                }));
            await db.SaveChangesAsync();
        }

        return client.Id;
    }
}
