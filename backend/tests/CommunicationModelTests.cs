using ClientPulse.Api.Communications;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClientPulse.Tests;

public class CommunicationModelTests
{
    // Builds the EF model only; no database connection is opened.
    private static ClientPulseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ClientPulseDbContext>()
            .UseNpgsql("Host=unused")
            .Options);

    [Fact]
    public void Communication_MapsTypeAsStringAndTimestampsAsTimestamptz()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Communication))!;

        Assert.Equal("character varying(20)", entity.FindProperty(nameof(Communication.Type))!.GetColumnType());
        Assert.Equal("timestamp with time zone", entity.FindProperty(nameof(Communication.OccurredAt))!.GetColumnType());
        Assert.Equal("text", entity.FindProperty(nameof(Communication.Body))!.GetColumnType());
        Assert.True(entity.GetForeignKeys().Single().IsRequired);
    }

    [Fact]
    public void Model_SeedsCommunications()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var seeded = model.FindEntityType(typeof(Communication))!.GetSeedData().ToList();

        Assert.Equal(DemoSeedData.Communications.Count, seeded.Count);
    }

    [Fact]
    public void SeedCommunications_AreValidAndBelowGeneratedIdRange()
    {
        var communications = DemoSeedData.Communications;
        var clientIds = DemoSeedData.Clients.Select(c => c.Id).ToHashSet();

        Assert.Equal(communications.Count, communications.Select(c => c.Id).Distinct().Count());
        Assert.All(communications, c =>
        {
            Assert.InRange(c.Id, 1, DemoSeedData.FirstGeneratedCommunicationId - 1);
            Assert.Contains(c.ClientId, clientIds);
            Assert.True(Enum.IsDefined(c.Type));
            Assert.Equal(TimeSpan.Zero, c.OccurredAt.Offset);
            Assert.False(string.IsNullOrWhiteSpace(c.Subject));
            Assert.InRange(c.Subject.Length, 1, CommunicationRules.SubjectMaxLength);
            Assert.InRange(c.Body.Length, 1, CommunicationRules.BodyMaxLength);
        });
        Assert.All(clientIds, id => Assert.Contains(communications, c => c.ClientId == id));
    }
}
