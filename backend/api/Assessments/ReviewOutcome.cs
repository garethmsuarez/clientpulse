using ClientPulse.Api.Analysis;

namespace ClientPulse.Api.Assessments;

public enum ReviewStatus
{
    Unreviewed,
    Accepted,
    Adjusted,
    Rejected,
}

// What an assessment's latest review means for it.
public static class ReviewOutcome
{
    public static ReviewStatus Status(AssessmentReview? latestReview) => latestReview?.Decision switch
    {
        null => ReviewStatus.Unreviewed,
        ReviewDecision.Accepted => ReviewStatus.Accepted,
        ReviewDecision.Adjusted => ReviewStatus.Adjusted,
        _ => ReviewStatus.Rejected,
    };

    // The level a reviewer has endorsed: the original level when accepted,
    // the replacement when adjusted, and none when unreviewed or rejected.
    // A rejected assessment does not imply low attention.
    public static AttentionLevel? ApprovedLevel(AttentionLevel original, AssessmentReview? latestReview) =>
        latestReview?.Decision switch
        {
            ReviewDecision.Accepted => original,
            ReviewDecision.Adjusted => latestReview.ReviewedAttentionLevel,
            _ => null,
        };
}
