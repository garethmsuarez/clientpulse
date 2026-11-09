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

    // Provenance for provider results (null for fixture results): the model
    // requested, the model the provider reported, and the effort used.
    public string? RequestedModel { get; set; }

    public string? ResponseModel { get; set; }

    public string? Effort { get; set; }

    public required string Summary { get; set; }

    public AttentionLevel AttentionLevel { get; set; }

    // Identifies the analyzed input by content (see InputFingerprint).
    public required string InputFingerprint { get; set; }

    public List<int> AnalyzedCommunicationIds { get; set; } = [];

    // Stored as a JSON column.
    public List<AssessmentSignal> Signals { get; set; } = [];

    // Builds a new row from validated analyzer output and the exact input it was given.
    public static Assessment FromResult(
        int clientId,
        AnalysisResult result,
        IReadOnlyList<AnalysisCommunication> communications,
        DateTimeOffset createdAt) => new()
    {
        ClientId = clientId,
        CreatedAt = createdAt,
        Source = result.Source,
        FixtureName = result.FixtureName,
        RequestedModel = result.RequestedModel,
        ResponseModel = result.ResponseModel,
        Effort = result.Effort,
        Summary = result.Summary,
        AttentionLevel = result.AttentionLevel,
        InputFingerprint = Analysis.InputFingerprint.Compute(communications),
        AnalyzedCommunicationIds = communications.Select(c => c.Id).ToList(),
        Signals = result.Signals
            .Select(signal => new AssessmentSignal
            {
                Kind = signal.Kind,
                Description = signal.Description,
                EvidenceCommunicationIds = signal.EvidenceCommunicationIds.ToList(),
            })
            .ToList(),
    };
}

public class AssessmentSignal
{
    public SignalKind Kind { get; set; }

    public required string Description { get; set; }

    public List<int> EvidenceCommunicationIds { get; set; } = [];
}
