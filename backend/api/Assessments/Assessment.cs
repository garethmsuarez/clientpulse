using ClientPulse.Api.Analysis;
using ClientPulse.Api.Clients;

namespace ClientPulse.Api.Assessments;

// One saved analysis result. Rows are only ever inserted, never updated, so
// earlier assessments remain available for later review.
public class Assessment
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public AnalysisSource Source { get; set; }

    public string? FixtureName { get; set; }

    public required string Summary { get; set; }

    public AttentionLevel AttentionLevel { get; set; }

    // Identifies the analyzed input by content (see InputFingerprint).
    public required string InputFingerprint { get; set; }

    public List<int> AnalyzedCommunicationIds { get; set; } = [];

    // Stored as a JSON column.
    public List<AssessmentSignal> Signals { get; set; } = [];
}

public class AssessmentSignal
{
    public SignalKind Kind { get; set; }

    public required string Description { get; set; }

    public List<int> EvidenceCommunicationIds { get; set; } = [];
}
