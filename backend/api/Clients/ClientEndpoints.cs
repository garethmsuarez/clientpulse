using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Clients;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", async (ClientPulseDbContext db, CancellationToken cancellationToken) =>
            await db.Clients
                .AsNoTracking()
                .OrderBy(client => client.Name)
                .Select(client => new ClientSummary(client.Id, client.Name, client.Industry))
                .ToListAsync(cancellationToken))
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

public record ClientSummary(int Id, string Name, string Industry);

public record ClientDetail(int Id, string Name, string Industry, string AccountOwner);
