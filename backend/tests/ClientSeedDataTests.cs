using ClientPulse.Api.Clients;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClientPulse.Tests;

public class ClientSeedDataTests
{
    // Builds the EF model only; no database connection is opened.
    private static ClientPulseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ClientPulseDbContext>()
            .UseNpgsql("Host=unused")
            .Options);

    [Fact]
    public void Model_SeedsClients()
    {
        using var context = CreateContext();

        // Seed data lives in the design-time model (the one migrations are generated from).
        var model = context.GetService<IDesignTimeModel>().Model;
        var seeded = model.FindEntityType(typeof(Client))!.GetSeedData().ToList();

        Assert.Equal(ClientPulseDbContext.SeedClients.Count, seeded.Count);
        Assert.NotEmpty(seeded);
    }

    [Fact]
    public void SeedClients_HaveUniqueIdsAndRequiredValues()
    {
        var clients = ClientPulseDbContext.SeedClients;

        Assert.Equal(clients.Count, clients.Select(c => c.Id).Distinct().Count());
        Assert.All(clients, client =>
        {
            Assert.True(client.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(client.Name));
            Assert.False(string.IsNullOrWhiteSpace(client.Industry));
        });
    }
}
