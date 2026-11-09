using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;

namespace ClientPulse.Tests;

public class AnthropicAnalysisOptionsTests
{
    private const string SyntheticKey = "synthetic-test-key-not-a-real-credential";

    [Fact]
    public void Defaults_WithAKey_AreValid()
    {
        Assert.Empty(new AnthropicAnalysisOptions().Validate(SyntheticKey));
    }

    [Fact]
    public void ReportsEveryProblem_WithoutRevealingTheKey()
    {
        var options = new AnthropicAnalysisOptions { Model = " ", Effort = "extreme", MaxOutputTokens = 0 };

        var withoutKey = options.Validate(null);
        Assert.Equal(4, withoutKey.Count);
        Assert.Contains(withoutKey, p => p.Contains("Anthropic:ApiKey"));
        Assert.Contains(withoutKey, p => p.Contains("Model"));
        Assert.Contains(withoutKey, p => p.Contains("Effort"));
        Assert.Contains(withoutKey, p => p.Contains("limits and timeouts"));

        Assert.All(options.Validate(SyntheticKey), p => Assert.DoesNotContain(SyntheticKey, p));
    }

    [Fact]
    public void ZeroRetries_IsAllowed()
    {
        Assert.Empty(new AnthropicAnalysisOptions { MaxRetries = 0 }.Validate(SyntheticKey));
    }

    // Validation and the SDK mapping keep separate lists; this keeps them in step.
    [Fact]
    public void EveryAllowedEffort_MapsToAnSdkEffort()
    {
        Assert.All(AnthropicAnalysisOptions.EffortLevels, level => ClaudeCommunicationAnalyzer.ParseEffort(level));
    }
}
