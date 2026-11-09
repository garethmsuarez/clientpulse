using System.Text.Json;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;
using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

public class ClaudeRequestTests
{
    private static readonly List<AnalysisCommunication> Input =
    [
        new(1007, CommunicationType.Email, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero), "Synthetic A", "Synthetic body A."),
        new(1003, CommunicationType.Call, new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero), "Synthetic B", "Synthetic body B."),
    ];

    private static JsonElement Schema() =>
        JsonSerializer.SerializeToElement(ClaudeRequest.BuildOutputSchema(Input.Select(c => c.Id).ToList()));

    [Fact]
    public void Schema_IsClosedAndRequiresAllFields()
    {
        var schema = Schema();

        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["summary", "attentionLevel", "signals"],
            schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(["Low", "Medium", "High"],
            schema.GetProperty("properties").GetProperty("attentionLevel").GetProperty("enum")
                .EnumerateArray().Select(e => e.GetString()));

        var signal = schema.GetProperty("properties").GetProperty("signals").GetProperty("items");
        Assert.False(signal.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["Concern", "Positive"],
            signal.GetProperty("properties").GetProperty("kind").GetProperty("enum")
                .EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Schema_RestrictsEvidenceToTheSuppliedIds()
    {
        var evidenceItems = Schema().GetProperty("properties").GetProperty("signals").GetProperty("items")
            .GetProperty("properties").GetProperty("evidenceCommunicationIds").GetProperty("items");

        Assert.Equal("integer", evidenceItems.GetProperty("type").GetString());
        Assert.Equal([1003, 1007], evidenceItems.GetProperty("enum").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public void UserContent_IncludesEachCommunicationWithItsId()
    {
        var content = ClaudeRequest.BuildUserContent(Input);
        var json = JsonDocument.Parse(content[content.IndexOf('[')..]).RootElement;

        Assert.Equal([1007, 1003], json.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));
        Assert.Equal("Email", json[0].GetProperty("type").GetString());
        Assert.Equal("Synthetic body B.", json[1].GetProperty("body").GetString());
    }

    [Fact]
    public void SystemPrompt_StatesTheGuardrails()
    {
        Assert.Contains("synthetic", ClaudeRequest.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("only those ids", ClaudeRequest.SystemPrompt);
        Assert.Contains("Do not predict churn", ClaudeRequest.SystemPrompt);
    }

    [Fact]
    public void Limits_RejectInsteadOfTruncating()
    {
        Assert.Null(ClaudeRequest.CheckLimits(Input, new AnthropicAnalysisOptions()));
        Assert.NotNull(ClaudeRequest.CheckLimits(Input, new AnthropicAnalysisOptions { MaxCommunications = 1 }));
        Assert.NotNull(ClaudeRequest.CheckLimits(Input, new AnthropicAnalysisOptions { MaxInputCharacters = 10 }));
    }
}
