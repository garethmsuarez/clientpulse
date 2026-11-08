using ClientPulse.Api.Data;
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

        return app;
    }
}

public record ClientSummary(int Id, string Name, string Industry);
