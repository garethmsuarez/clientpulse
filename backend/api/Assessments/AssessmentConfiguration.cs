using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientPulse.Api.Assessments;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> assessment)
    {
        assessment.Property(a => a.Source).HasConversion<string>().HasMaxLength(20);
        assessment.Property(a => a.FixtureName).HasMaxLength(AssessmentOutputRules.FixtureNameMaxLength);
        assessment.Property(a => a.RequestedModel).HasMaxLength(AssessmentOutputRules.ModelNameMaxLength);
        assessment.Property(a => a.ResponseModel).HasMaxLength(AssessmentOutputRules.ModelNameMaxLength);
        assessment.Property(a => a.Effort).HasMaxLength(10);
        assessment.Property(a => a.Summary).HasColumnType("text");
        assessment.Property(a => a.AttentionLevel).HasConversion<string>().HasMaxLength(10);
        assessment.Property(a => a.InputFingerprint).HasMaxLength(64).IsFixedLength();
        assessment.HasOne(a => a.Client)
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        assessment.HasIndex(a => new { a.ClientId, a.CreatedAt });
        assessment.OwnsMany(a => a.Signals, signals =>
        {
            signals.ToJson();
            signals.Property(s => s.Kind).HasConversion<string>();
        });
    }
}
