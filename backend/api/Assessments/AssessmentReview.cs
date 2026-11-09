using ClientPulse.Api.Analysis;

namespace ClientPulse.Api.Assessments;

public enum ReviewDecision
{
    Accepted,
    Adjusted,
    Rejected,
}

// A human review of one assessment. Reviews are append-only (no editing or
// deleting); the newest review is the assessment's current review. They form
// a review history, not a verified audit trail: the reviewer name is
// self-reported and unverified.
public class AssessmentReview
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public Assessment? Assessment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ReviewDecision Decision { get; set; }

    // Set only for Adjusted reviews; never changes the original assessment.
    public AttentionLevel? ReviewedAttentionLevel { get; set; }

    public string? Notes { get; set; }

    public required string ReviewerName { get; set; }
}
