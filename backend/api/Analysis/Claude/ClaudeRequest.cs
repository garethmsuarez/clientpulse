using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClientPulse.Api.Analysis.Claude;

// Builds the provider-neutral parts of a Claude analysis request. Pure: no SDK
// types and no I/O, so it can be unit tested.
public static class ClaudeRequest
{
    public const string SystemPrompt = """
        You review synthetic client communications for an account manager.
        All data is synthetic demo content.

        Assess only what the supplied communications say:
        - Write a short, factual summary of the client relationship.
        - Choose an attention level: Low, Medium, or High, meaning how soon the
          account manager should look at this client.
        - List concerns and positive signals. Each signal must cite the ids of
          the supplied communications that support it, and only those ids.
        - Do not predict churn or retention, and do not give probabilities or scores.
        - If the evidence is thin or ambiguous, say so in the summary rather than guessing.
        """;

    private static readonly JsonSerializerOptions InputJson = new(JsonSerializerDefaults.Web);

    // The communications as JSON for the user message.
    public static string BuildUserContent(IReadOnlyList<AnalysisCommunication> communications)
    {
        var payload = communications.Select(c => new
        {
            id = c.Id,
            type = c.Type.ToString(),
            occurredAt = c.OccurredAt.UtcDateTime,
            subject = c.Subject,
            body = c.Body,
        });

        return "Analyze these communications:\n" + JsonSerializer.Serialize(payload, InputJson);
    }

    // Output schema. Evidence ids are restricted to the communications sent in
    // this request; AssessmentOutputRules re-checks this server-side regardless.
    public static Dictionary<string, JsonElement> BuildOutputSchema(IReadOnlyCollection<int> communicationIds)
    {
        var evidenceIds = new JsonArray(communicationIds.Order().Select(id => (JsonNode)JsonValue.Create(id)).ToArray());

        var signal = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray("kind", "description", "evidenceCommunicationIds"),
            ["properties"] = new JsonObject
            {
                ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("Concern", "Positive") },
                ["description"] = new JsonObject { ["type"] = "string" },
                ["evidenceCommunicationIds"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "integer", ["enum"] = evidenceIds },
                },
            },
        };

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray("summary", "attentionLevel", "signals"),
            ["properties"] = new JsonObject
            {
                ["summary"] = new JsonObject { ["type"] = "string" },
                ["attentionLevel"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("Low", "Medium", "High") },
                ["signals"] = new JsonObject { ["type"] = "array", ["items"] = signal },
            },
        };

        return schema.ToDictionary(p => p.Key, p => JsonSerializer.SerializeToElement(p.Value));
    }

    // Inputs above the configured limits are rejected rather than truncated.
    public static string? CheckLimits(IReadOnlyList<AnalysisCommunication> communications, AnthropicAnalysisOptions options)
    {
        if (communications.Count > options.MaxCommunications)
        {
            return $"This client has more than {options.MaxCommunications} communications, " +
                   "which is above the limit for one analysis. Nothing was sent to the provider.";
        }

        var characters = communications.Sum(c => (long)c.Subject.Length + c.Body.Length);
        if (characters > options.MaxInputCharacters)
        {
            return $"This client's communications exceed {options.MaxInputCharacters:N0} characters, " +
                   "which is above the limit for one analysis. Nothing was sent to the provider.";
        }

        return null;
    }
}
