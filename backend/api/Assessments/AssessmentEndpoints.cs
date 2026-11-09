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
                    AnalyzeStatus.Created => TypedResults.Created(
                        $"/api/clients/{clientId}/assessments/{result.Assessment!.Id}", result.Assessment),
                    AnalyzeStatus.ClientNotFound => ClientProblems.NotFound(clientId),
                    AnalyzeStatus.NoCommunications => AssessmentProblems.NoCommunications(),
                    AnalyzeStatus.Unsupported => AssessmentProblems.UnsupportedInput(result.Reason!),
                    AnalyzeStatus.ProviderFailed => AssessmentProblems.ProviderFailure(result.FailureKind!.Value),
                    _ => AssessmentProblems.InvalidAnalyzerOutput(),
                };
            })
            .WithName("AnalyzeClientCommunications");

        group.MapGet("/", async Task<Results<Ok<List<AssessmentSummaryResponse>>, ProblemHttpResult>> (
                int clientId, ClientPulseDbContext db, AssessmentQueries queries, CancellationToken cancellationToken) =>
            {
                if (!await ClientExistsAsync(db, clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                return TypedResults.Ok(await queries.GetHistoryAsync(clientId, cancellationToken));
            })
            .WithName("GetClientAssessments");

        group.MapGet("/latest", async Task<Results<Ok<AssessmentResponse>, NoContent, ProblemHttpResult>> (
                int clientId, ClientPulseDbContext db, AssessmentQueries queries, CancellationToken cancellationToken) =>
            {
                if (!await ClientExistsAsync(db, clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                var latest = await queries.GetLatestAsync(clientId, cancellationToken);
                return latest is null ? TypedResults.NoContent() : TypedResults.Ok(latest);
            })
            .WithName("GetLatestClientAssessment");

        group.MapGet("/{assessmentId:int}", async Task<Results<Ok<AssessmentResponse>, ProblemHttpResult>> (
                int clientId,
                int assessmentId,
                ClientPulseDbContext db,
                AssessmentQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!await ClientExistsAsync(db, clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                var assessment = await queries.GetAsync(clientId, assessmentId, cancellationToken);
                return assessment is null
                    ? AssessmentProblems.AssessmentNotFound(assessmentId)
                    : TypedResults.Ok(assessment);
            })
            .WithName("GetClientAssessment");

        group.MapPost("/{assessmentId:int}/reviews",
                async Task<Results<Created<ReviewResponse>, ValidationProblem, ProblemHttpResult>> (
                    int clientId,
                    int assessmentId,
                    CreateReviewRequest request,
                    ClientPulseDbContext db,
                    ReviewService reviews,
                    CancellationToken cancellationToken) =>
                {
                    if (!await ClientExistsAsync(db, clientId, cancellationToken))
                    {
                        return ClientProblems.NotFound(clientId);
                    }

                    var result = await reviews.AddReviewAsync(clientId, assessmentId, request, cancellationToken);

                    return result.Status switch
                    {
                        // No single-review route exists, so no Location header is sent.
                        ReviewSubmitStatus.Created => TypedResults.Created((string?)null, result.Review!),
                        ReviewSubmitStatus.NotFound => AssessmentProblems.AssessmentNotFound(assessmentId),
                        ReviewSubmitStatus.NotLatest => AssessmentProblems.NotLatestAssessment(),
                        _ => TypedResults.ValidationProblem(result.Errors!),
                    };
                })
            .WithName("CreateAssessmentReview");

        return app;
    }

    private static Task<bool> ClientExistsAsync(ClientPulseDbContext db, int clientId, CancellationToken cancellationToken) =>
        db.Clients.AnyAsync(client => client.Id == clientId, cancellationToken);
}
