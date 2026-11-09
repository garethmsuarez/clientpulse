using ClientPulse.Api.Analysis;
using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Assessments;

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
    bool IsCurrent);

public record SignalResponse(string Description, IReadOnlyList<EvidenceResponse> Evidence);

public record EvidenceResponse(int CommunicationId, CommunicationType Type, DateTimeOffset OccurredAt, string Subject);
