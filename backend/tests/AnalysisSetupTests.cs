using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClientPulse.Tests;

public class AnalysisSetupTests
{
    private const string SyntheticKey = "synthetic-test-key-not-a-real-credential";

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddCommunicationAnalysis(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void NoConfiguration_UsesFixture()
    {
        using var provider = Build([]);

        Assert.IsType<FixtureCommunicationAnalyzer>(provider.GetRequiredService<ICommunicationAnalyzer>());
        Assert.Equal(new ActiveAnalysisProvider(AnalysisProvider.Fixture, null, null),
            provider.GetRequiredService<ActiveAnalysisProvider>());
    }

    [Fact]
    public void AnthropicWithoutKey_FailsClearly_AndDoesNotFallBackToFixture()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Build(new() { ["Analysis:Provider"] = "Anthropic" }));

        Assert.Contains("Anthropic:ApiKey", error.Message);
    }

    [Fact]
    public void AnthropicWithInvalidEffort_FailsWithoutRevealingTheKey()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Build(new()
        {
            ["Analysis:Provider"] = "Anthropic",
            ["Analysis:Anthropic:Effort"] = "extreme",
            ["Anthropic:ApiKey"] = SyntheticKey,
        }));

        Assert.Contains("Effort", error.Message);
        Assert.DoesNotContain(SyntheticKey, error.Message);
    }

    [Fact]
    public void AnthropicWithKey_RegistersClaudeWithConfiguredModelAndEffort()
    {
        using var provider = Build(new()
        {
            ["Analysis:Provider"] = "Anthropic",
            ["Analysis:Anthropic:Model"] = "claude-sonnet-5-5",
            ["Analysis:Anthropic:Effort"] = "medium",
            ["Anthropic:ApiKey"] = SyntheticKey,
        });

        Assert.IsType<ClaudeCommunicationAnalyzer>(provider.GetRequiredService<ICommunicationAnalyzer>());
        Assert.Equal(new ActiveAnalysisProvider(AnalysisProvider.Anthropic, "claude-sonnet-5-5", "medium"),
            provider.GetRequiredService<ActiveAnalysisProvider>());
    }

    [Fact]
    public void Defaults_MatchTheApprovedStartingPoint()
    {
        var options = new AnthropicAnalysisOptions();

        Assert.Equal("claude-sonnet-5-5", options.Model);
        Assert.Equal("medium", options.Effort);
        // The SDK's worst case is every attempt timing out plus backoff and any
        // Retry-After delay, so a deadline at or below attempts x timeout binds.
        Assert.True(options.OverallTimeoutSeconds <= options.AttemptTimeoutSeconds * (options.MaxRetries + 1),
            "The overall deadline should be the binding bound, not the SDK's worst case.");
    }
}
