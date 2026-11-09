using ClientPulse.Api.Clients;
using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Communications;

public static class CommunicationEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients/{clientId:int}/communications").WithTags("Communications");

        group.MapGet("/", async Task<Results<Ok<List<CommunicationResponse>>, ProblemHttpResult>> (
                int clientId, ClientPulseDbContext db, CancellationToken cancellationToken) =>
            {
                if (!await db.Clients.AnyAsync(client => client.Id == clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                var communications = await db.Communications
                    .AsNoTracking()
                    .Where(communication => communication.ClientId == clientId)
                    .OrderByDescending(communication => communication.OccurredAt)
                    .ThenByDescending(communication => communication.Id)
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(communications.Select(CommunicationResponse.From).ToList());
            })
            .WithName("GetClientCommunications");

        group.MapPost("/", async Task<Results<Created<CommunicationResponse>, ValidationProblem, ProblemHttpResult>> (
                int clientId,
                CreateCommunicationRequest request,
                ClientPulseDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                if (!await db.Clients.AnyAsync(client => client.Id == clientId, cancellationToken))
                {
                    return ClientProblems.NotFound(clientId);
                }

                var now = timeProvider.GetUtcNow();
                if (!CommunicationRules.TryValidate(request, now, out var valid, out var errors))
                {
                    return TypedResults.ValidationProblem(errors);
                }

                var communication = new Communication
                {
                    ClientId = clientId,
                    Type = valid.Type,
                    OccurredAt = valid.OccurredAt,
                    Subject = valid.Subject,
                    Body = valid.Body,
                    CreatedAt = now,
                };

                db.Communications.Add(communication);
                await db.SaveChangesAsync(cancellationToken);

                // No single-communication route exists yet, so no Location header is sent.
                return TypedResults.Created((string?)null, CommunicationResponse.From(communication));
            })
            .WithName("CreateClientCommunication");

        return app;
    }
}

public record CommunicationResponse(
    int Id,
    int ClientId,
    CommunicationType Type,
    DateTimeOffset OccurredAt,
    string Subject,
    string Body,
    DateTimeOffset CreatedAt)
{
    public static CommunicationResponse From(Communication communication) => new(
        communication.Id,
        communication.ClientId,
        communication.Type,
        communication.OccurredAt,
        communication.Subject,
        communication.Body,
        communication.CreatedAt);
}
