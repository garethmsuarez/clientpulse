using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientPulse.Api.Communications;

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> communication)
    {
        communication.Property(c => c.Id)
            .HasIdentityOptions(startValue: DemoSeedData.FirstGeneratedCommunicationId);
        communication.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);
        communication.Property(c => c.Subject).HasMaxLength(CommunicationRules.SubjectMaxLength);
        communication.Property(c => c.Body).HasColumnType("text");
        communication.HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        communication.HasIndex(c => new { c.ClientId, c.OccurredAt });
        communication.HasData(DemoSeedData.Communications);
    }
}
