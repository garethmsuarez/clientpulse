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

        var problems = claude.Validate(apiKey);
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
