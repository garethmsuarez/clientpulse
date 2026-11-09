using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;

namespace ClientPulse.Tests;

public class ReviewOutcomeTests
{
    private static AssessmentReview Review(ReviewDecision decision, AttentionLevel? level = null) => new()
    {
        Decision = decision,
        ReviewedAttentionLevel = level,
        ReviewerName = "Synthetic Reviewer",
    };

    [Fact]
    public void Unreviewed_HasNoApprovedLevel()
    {
        Assert.Equal(ReviewStatus.Unreviewed, ReviewOutcome.Status(null));
        Assert.Null(ReviewOutcome.ApprovedLevel(AttentionLevel.High, null));
    }

    [Fact]
    public void Accepted_ApprovesOriginalLevel()
    {
        var review = Review(ReviewDecision.Accepted);

        Assert.Equal(ReviewStatus.Accepted, ReviewOutcome.Status(review));
        Assert.Equal(AttentionLevel.High, ReviewOutcome.ApprovedLevel(AttentionLevel.High, review));
    }

    [Fact]
    public void Adjusted_ApprovesReplacementLevel()
    {
        var review = Review(ReviewDecision.Adjusted, AttentionLevel.Low);

        Assert.Equal(ReviewStatus.Adjusted, ReviewOutcome.Status(review));
        Assert.Equal(AttentionLevel.Low, ReviewOutcome.ApprovedLevel(AttentionLevel.High, review));
    }

    [Fact]
    public void Rejected_HasNoApprovedLevel()
    {
        var review = Review(ReviewDecision.Rejected);

        Assert.Equal(ReviewStatus.Rejected, ReviewOutcome.Status(review));
        Assert.Null(ReviewOutcome.ApprovedLevel(AttentionLevel.High, review));
    }
}
