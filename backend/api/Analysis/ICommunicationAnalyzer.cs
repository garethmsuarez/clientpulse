using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Analysis;

// The single application-owned seam for communication analysis. One
// implementation is active, chosen by server configuration (Analysis:Provider):
// the deterministic FixtureCommunicationAnalyzer (default) or the Claude-backed
// ClaudeCommunicationAnalyzer. Provider SDK types never cross this interface.
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

// The provider could not produce a usable result. Carries only a category;
// raw provider errors and response text are never passed through.
public record AnalysisFailed(AnalysisFailureKind Kind) : AnalyzerOutcome;

public enum AnalysisFailureKind
{
    InvalidOutput,
    OutputTruncated,
    Refused,
    Unavailable,
    Timeout,
    Configuration,
}

public record AnalysisResult(
    AnalysisSource Source,
    string? FixtureName,
    string Summary,
    AttentionLevel AttentionLevel,
    IReadOnlyList<AnalysisSignal> Signals,
    string? RequestedModel = null,
    string? ResponseModel = null,
    string? Effort = null);

public record AnalysisSignal(SignalKind Kind, string Description, IReadOnlyList<int> EvidenceCommunicationIds);

public enum AnalysisSource
{
    Fixture,
    Anthropic,
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
