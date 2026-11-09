using ClientPulse.Api.Clients;
using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Assessments;

public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients/{clientId:int}/assessments").WithTags("Assessments");

        group.MapPost("/", async Task<Results<Created<AssessmentResponse>, ProblemHttpResult>> (
                int clientId, AssessmentService service, CancellationToken cancellationToken) =>
            {
                var result = await service.AnalyzeAsync(clientId, cancellationToken);

                return result.Status switch
                {
                    // No single-assessment route exists yet, so no Location header is sent.
                    AnalyzeStatus.Created => TypedResults.Created((string?)null, result.Assessment!),
                    AnalyzeStatus.ClientNotFound => ClientProblems.NotFound(clientId),
                    AnalyzeStatus.NoCommunications => AssessmentProblems.NoCommunications(),
                    AnalyzeStatus.Unsupported => AssessmentProblems.UnsupportedInput(result.Reason!),
                    _ => AssessmentProblems.InvalidAnalyzerOutput(),
                };
            })
            .WithName("AnalyzeClientCommunications");

        group.MapGet("/latest", async Task<Results<Ok<AssessmentResponse>, NoContent, ProblemHttpResult>> (
                int clientId, ClientPulseDbContext db, AssessmentService service, CancellationToken cancellationToken) =>
            {
                if (!await db.Clients.AnyAsync(client => client.Id == clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                var latest = await service.GetLatestAsync(clientId, cancellationToken);
                return latest is null ? TypedResults.NoContent() : TypedResults.Ok(latest);
            })
            .WithName("GetLatestClientAssessment");

        return app;
    }
}
