using ClientPulse.Api.Analysis.Claude;

namespace ClientPulse.Api.Analysis;

public static class AnalysisSetup
{
    public const string ApiKeySetting = "Anthropic:ApiKey";

    // Registers exactly one analyzer, chosen by Analysis:Provider (default
    // Fixture). Anthropic mode with missing or invalid settings fails at
    // startup; it never falls back to fixture results. Messages never include
    // the key itself.
    public static IServiceCollection AddCommunicationAnalysis(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new AnalysisOptions();
        configuration.GetSection("Analysis").Bind(options);

        if (options.Provider == AnalysisProvider.Fixture)
        {
            services.AddSingleton(new ActiveAnalysisProvider(AnalysisProvider.Fixture, null, null));
            services.AddSingleton<ICommunicationAnalyzer, FixtureCommunicationAnalyzer>();
            return services;
        }

        var claude = options.Anthropic;
        var apiKey = configuration[ApiKeySetting];
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            problems.Add($"'{ApiKeySetting}' is not set. Store it with user secrets or an environment variable (Anthropic__ApiKey).");
        }

        if (string.IsNullOrWhiteSpace(claude.Model))
        {
            problems.Add("'Analysis:Anthropic:Model' is not set.");
        }

        if (!AnthropicAnalysisOptions.EffortLevels.Contains(claude.Effort))
        {
            problems.Add($"'Analysis:Anthropic:Effort' must be one of: {string.Join(", ", AnthropicAnalysisOptions.EffortLevels)}.");
        }

        if (claude.MaxOutputTokens <= 0 || claude.AttemptTimeoutSeconds <= 0 || claude.OverallTimeoutSeconds <= 0 ||
            claude.MaxRetries < 0 || claude.MaxCommunications <= 0 || claude.MaxInputCharacters <= 0)
        {
            problems.Add("'Analysis:Anthropic' limits and timeouts must be positive (MaxRetries may be 0).");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Analysis:Provider is 'Anthropic' but its configuration is incomplete: " + string.Join(" ", problems));
        }

        services.AddSingleton(new ActiveAnalysisProvider(AnalysisProvider.Anthropic, claude.Model, claude.Effort));
        services.AddSingleton<ICommunicationAnalyzer>(sp => new ClaudeCommunicationAnalyzer(
            apiKey!, claude, sp.GetRequiredService<ILogger<ClaudeCommunicationAnalyzer>>()));
        return services;
    }
}
