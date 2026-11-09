using ClientPulse.Api.Assessments;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClientPulse.Tests;

public class AssessmentModelTests
{
    // Builds the EF model only; no database connection is opened.
    private static ClientPulseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ClientPulseDbContext>()
            .UseNpgsql("Host=unused")
            .Options);

    [Fact]
    public void Assessment_MapsInputIdsAsArrayAndSignalsAsJson()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Assessment))!;

        Assert.Equal("integer[]", entity.FindProperty(nameof(Assessment.AnalyzedCommunicationIds))!.GetColumnType());
        Assert.Equal("character(64)", entity.FindProperty(nameof(Assessment.InputFingerprint))!.GetColumnType());
        Assert.True(entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(ClientPulse.Api.Clients.Client)).IsRequired);

        var signals = entity.FindNavigation(nameof(Assessment.Signals))!.TargetEntityType;
        Assert.True(signals.IsMappedToJson());
        Assert.Equal("Signals", signals.GetContainerColumnName());
    }
}
