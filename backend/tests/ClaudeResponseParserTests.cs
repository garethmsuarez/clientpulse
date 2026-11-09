using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;

namespace ClientPulse.Tests;

public class ClaudeResponseParserTests
{
    private const string ValidJson = """
        {"summary":"Synthetic summary.","attentionLevel":"High",
         "signals":[{"kind":"Concern","description":"Synthetic concern.","evidenceCommunicationIds":[1001,1002]}]}
        """;

    private static AnalyzerOutcome Parse(string? stopReason, params string[] texts) =>
        ClaudeResponseParser.Parse(stopReason, texts, "claude-sonnet-5-5", "claude-sonnet-5-5", "medium");

    private static void AssertFailed(AnalyzerOutcome outcome, AnalysisFailureKind kind) =>
        Assert.Equal(kind, Assert.IsType<AnalysisFailed>(outcome).Kind);

    [Fact]
    public void ValidOutput_MapsToAnthropicResultWithProvenance()
    {
        var result = Assert.IsType<AnalysisCompleted>(Parse("end_turn", ValidJson)).Result;

        Assert.Equal(AnalysisSource.Anthropic, result.Source);
        Assert.Null(result.FixtureName);
        Assert.Equal(AttentionLevel.High, result.AttentionLevel);
        Assert.Equal("claude-sonnet-5-5", result.RequestedModel);
        Assert.Equal("claude-sonnet-5-5", result.ResponseModel);
        Assert.Equal("medium", result.Effort);
        var signal = Assert.Single(result.Signals);
        Assert.Equal(SignalKind.Concern, signal.Kind);
        Assert.Equal([1001, 1002], signal.EvidenceCommunicationIds);
    }

    [Fact]
    public void Refusal_IsRefused() => AssertFailed(Parse("refusal"), AnalysisFailureKind.Refused);

    [Fact]
    public void MaxTokens_IsTruncated() => AssertFailed(Parse("max_tokens", "{\"summary\":"), AnalysisFailureKind.OutputTruncated);

    [Theory]
    [InlineData(null)]
    [InlineData("tool_use")]
    [InlineData("pause_turn")]
    [InlineData("stop_sequence")]
    public void OtherStopReasons_AreInvalid(string? stopReason) =>
        AssertFailed(Parse(stopReason, ValidJson), AnalysisFailureKind.InvalidOutput);

    [Fact]
    public void NoTextOrSeveralTextBlocks_AreInvalid()
    {
        AssertFailed(Parse("end_turn"), AnalysisFailureKind.InvalidOutput);
        AssertFailed(Parse("end_turn", ValidJson, ValidJson), AnalysisFailureKind.InvalidOutput);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"summary":"S","attentionLevel":"Critical","signals":[]}""")]
    [InlineData("""{"summary":"S","attentionLevel":2,"signals":[]}""")]
    [InlineData("""{"summary":"S","attentionLevel":"Low"}""")]
    [InlineData("""{"summary":null,"attentionLevel":"Low","signals":[]}""")]
    [InlineData("""{"summary":"S","attentionLevel":"Low","signals":[],"churnProbability":0.4}""")]
    [InlineData("""{"summary":"S","attentionLevel":"Low","signals":[{"kind":"Concern","description":"D"}]}""")]
    public void MalformedOrOffSchemaJson_IsInvalid(string json) =>
        AssertFailed(Parse("end_turn", json), AnalysisFailureKind.InvalidOutput);
}
