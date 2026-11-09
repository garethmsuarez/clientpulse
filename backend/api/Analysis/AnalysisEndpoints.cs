namespace ClientPulse.Api.Analysis;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        // The active analyzer, for display. No secrets.
        app.MapGet("/api/analysis/provider", (ActiveAnalysisProvider provider) => TypedResults.Ok(provider))
            .WithTags("Analysis")
            .WithName("GetAnalysisProvider");

        return app;
    }
}
