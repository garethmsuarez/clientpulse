using ClientPulse.Api.Assessments;
using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Clients;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", async (ClientPulseDbContext db, AssessmentQueries assessments, CancellationToken cancellationToken) =>
            {
                var clients = await db.Clients
                    .AsNoTracking()
                    .OrderBy(client => client.Name)
                    .Select(client => new { client.Id, client.Name, client.Industry })
                    .ToListAsync(cancellationToken);

                var latest = await assessments.GetLatestByClientAsync(cancellationToken);

                return clients
                    .Select(client => new ClientSummary(
                        client.Id, client.Name, client.Industry, latest.GetValueOrDefault(client.Id)))
                    .ToList();
            })
            .WithName("GetClients");

        group.MapGet("/{id:int}", async Task<Results<Ok<ClientDetail>, ProblemHttpResult>> (
                int id, ClientPulseDbContext db, CancellationToken cancellationToken) =>
            {
                var client = await db.Clients
                    .AsNoTracking()
                    .Where(client => client.Id == id)
                    .Select(client => new ClientDetail(client.Id, client.Name, client.Industry, client.AccountOwner))
                    .SingleOrDefaultAsync(cancellationToken);

                return client is null ? ClientProblems.NotFound(id) : TypedResults.Ok(client);
            })
            .WithName("GetClient");

        return app;
    }
}

// LatestAssessment is null when the client has not been assessed yet.
public record ClientSummary(int Id, string Name, string Industry, AssessmentSummaryResponse? LatestAssessment);

public record ClientDetail(int Id, string Name, string Industry, string AccountOwner);
