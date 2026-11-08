using System.Net;
using System.Net.Http.Json;
using ClientPulse.Api.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ClientPulse.Tests;

[Collection(ApiCollection.Name)]
public class ClientEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetClient_ReturnsDetailWithAccountOwner()
    {
        var client = await _client.GetFromJsonAsync<ClientDetail>("/api/clients/1");

        Assert.NotNull(client);
        Assert.Equal(1, client.Id);
        Assert.Equal("Northwind Logistics", client.Name);
        Assert.Equal("Priya Raman", client.AccountOwner);
    }

    [Fact]
    public async Task GetClient_UnknownId_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/api/clients/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Client not found", problem?.Title);
    }
}
