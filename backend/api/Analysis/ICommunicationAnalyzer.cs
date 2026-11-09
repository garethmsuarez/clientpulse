using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Analysis;

// The single application-owned seam for communication analysis. The only
// implementation for now is the deterministic FixtureCommunicationAnalyzer.
public interface ICommunicationAnalyzer
{
    Task<AnalyzerOutcome> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

public record AnalysisRequest(int ClientId, IReadOnlyList<AnalysisCommunication> Communications);

public record AnalysisCommunication(
    int Id,
    CommunicationType Type,
    DateTimeOffset OccurredAt,
    string Subject,
    string Body);

public abstract record AnalyzerOutcome;

public record AnalysisCompleted(AnalysisResult Result) : AnalyzerOutcome;

// The analyzer cannot produce a result for this input (for the fixture
// analyzer: no predefined scenario matches the communications supplied).
public record AnalysisUnsupported(string Reason) : AnalyzerOutcome;

public record AnalysisResult(
    AnalysisSource Source,
    string? FixtureName,
    string Summary,
    AttentionLevel AttentionLevel,
    IReadOnlyList<AnalysisSignal> Signals);

public record AnalysisSignal(SignalKind Kind, string Description, IReadOnlyList<int> EvidenceCommunicationIds);

public enum AnalysisSource
{
    Fixture,
}

public enum AttentionLevel
{
    Low,
    Medium,
    High,
}

public enum SignalKind
{
    Concern,
    Positive,
}
