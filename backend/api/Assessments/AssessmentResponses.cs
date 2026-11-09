using ClientPulse.Api.Analysis;
using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Assessments;

public enum ReviewStatus
{
    Unreviewed,
    Accepted,
    Adjusted,
    Rejected,
}

public record AssessmentResponse(
    int Id,
    int ClientId,
    DateTimeOffset CreatedAt,
    AnalysisSource Source,
    bool IsFixture,
    string? FixtureName,
    string? Disclaimer,
    string Summary,
    AttentionLevel AttentionLevel,
    IReadOnlyList<SignalResponse> Concerns,
    IReadOnlyList<SignalResponse> PositiveSignals,
    IReadOnlyList<int> AnalyzedCommunicationIds,
    bool IsCurrent,
    bool IsLatest,
    ReviewStatus ReviewStatus,
    AttentionLevel? ReviewerApprovedAttentionLevel,
    IReadOnlyList<ReviewResponse> Reviews);

public record SignalResponse(string Description, IReadOnlyList<EvidenceResponse> Evidence);

public record EvidenceResponse(int CommunicationId, CommunicationType Type, DateTimeOffset OccurredAt, string Subject);

// ReviewerName is self-reported and unverified.
public record ReviewResponse(
    int Id,
    int AssessmentId,
    DateTimeOffset CreatedAt,
    ReviewDecision Decision,
    AttentionLevel? ReviewedAttentionLevel,
    string? Notes,
    string ReviewerName);

public record AssessmentSummaryResponse(
    int Id,
    DateTimeOffset CreatedAt,
    AnalysisSource Source,
    bool IsFixture,
    string? FixtureName,
    AttentionLevel AttentionLevel,
    bool IsCurrent,
    bool IsLatest,
    ReviewStatus ReviewStatus,
    AttentionLevel? ReviewerApprovedAttentionLevel,
    ReviewResponse? LatestReview);

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

    public static ReviewResponse ToResponse(AssessmentReview review) => new(
        review.Id,
        review.AssessmentId,
        review.CreatedAt,
        review.Decision,
        review.ReviewedAttentionLevel,
        review.Notes,
        review.ReviewerName);
}
