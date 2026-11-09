using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientPulse.Api.Analysis.Claude;

// Turns a Claude reply into an analyzer outcome. Pure: takes plain values
// (no SDK types) so every failure path can be unit tested without the API.
// Domain checks (lengths, evidence within the input) happen afterwards in
// AssessmentOutputRules.
public static class ClaudeResponseParser
{
    private static readonly JsonSerializerOptions Strict = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public static AnalyzerOutcome Parse(
        string? stopReason,
        IReadOnlyList<string> textBlocks,
        string requestedModel,
        string? responseModel,
        string effort)
    {
        switch (stopReason)
        {
            case "refusal":
                return new AnalysisFailed(AnalysisFailureKind.Refused);
            case "max_tokens":
                return new AnalysisFailed(AnalysisFailureKind.OutputTruncated);
            case "end_turn":
                break;
            default:
                return new AnalysisFailed(AnalysisFailureKind.InvalidOutput);
        }

        if (textBlocks.Count != 1)
        {
            return new AnalysisFailed(AnalysisFailureKind.InvalidOutput);
        }

        ClaudeOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<ClaudeOutput>(textBlocks[0], Strict);
        }
        catch (JsonException)
        {
            return new AnalysisFailed(AnalysisFailureKind.InvalidOutput);
        }

        if (output is null)
        {
            return new AnalysisFailed(AnalysisFailureKind.InvalidOutput);
        }

        return new AnalysisCompleted(new AnalysisResult(
            AnalysisSource.Anthropic,
            FixtureName: null,
            output.Summary,
            output.AttentionLevel,
            output.Signals
                .Select(s => new AnalysisSignal(s.Kind, s.Description, s.EvidenceCommunicationIds))
                .ToList(),
            requestedModel,
            responseModel,
            effort));
    }

    private sealed record ClaudeOutput(string Summary, AttentionLevel AttentionLevel, List<ClaudeSignal> Signals);

    private sealed record ClaudeSignal(SignalKind Kind, string Description, List<int> EvidenceCommunicationIds);
}
