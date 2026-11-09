using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientPulse.Api.Assessments;

public class AssessmentReviewConfiguration : IEntityTypeConfiguration<AssessmentReview>
{
    public void Configure(EntityTypeBuilder<AssessmentReview> review)
    {
        review.Property(r => r.Decision).HasConversion<string>().HasMaxLength(10);
        review.Property(r => r.ReviewedAttentionLevel).HasConversion<string>().HasMaxLength(10);
        review.Property(r => r.Notes).HasColumnType("text");
        review.Property(r => r.ReviewerName).HasMaxLength(ReviewRules.ReviewerNameMaxLength);
        review.HasOne(r => r.Assessment)
            .WithMany()
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        review.HasIndex(r => new { r.AssessmentId, r.CreatedAt });
    }
}
