using Microsoft.AspNetCore.Http.HttpResults;

namespace ClientPulse.Api.Clients;

public static class ClientProblems
{
    public static ProblemHttpResult NotFound(int clientId) =>
        TypedResults.Problem(
            title: "Client not found",
            detail: $"No client with id {clientId}.",
            statusCode: StatusCodes.Status404NotFound);
}
