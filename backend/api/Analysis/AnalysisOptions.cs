namespace ClientPulse.Api.Analysis;

public enum AnalysisProvider
{
    Fixture,
    Anthropic,
}

// Bound from the "Analysis" configuration section. The API key is separate
// (Anthropic:ApiKey in user secrets or environment) and never lives here.
public class AnalysisOptions
{
    public AnalysisProvider Provider { get; set; } = AnalysisProvider.Fixture;

    public AnthropicAnalysisOptions Anthropic { get; set; } = new();
}

public class AnthropicAnalysisOptions
{
    public static readonly string[] EffortLevels = ["low", "medium", "high", "xhigh", "max"];

    public string Model { get; set; } = "claude-sonnet-5-5";

    public string Effort { get; set; } = "medium";

    // Upper bound on generated tokens, including adaptive thinking.
    public int MaxOutputTokens { get; set; } = 16_000;

    // Per HTTP attempt (the SDK's Timeout). The SDK retries a timed-out attempt.
    public int AttemptTimeoutSeconds { get; set; } = 40;

    // The SDK's own retries (connection errors, timeouts, 408/409/429/5xx).
    // No other retry layer exists.
    public int MaxRetries { get; set; } = 2;

    // Hard deadline for the whole call, including SDK retries and backoff.
    public int OverallTimeoutSeconds { get; set; } = 120;

    // Larger inputs are rejected (422) rather than truncated.
    public int MaxCommunications { get; set; } = 200;

    public int MaxInputCharacters { get; set; } = 400_000;
}

// What the UI may show about the active analyzer. Contains no secrets.
public record ActiveAnalysisProvider(AnalysisProvider Provider, string? Model, string? Effort);
