using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClientPulse.Api.Communications;
using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClientPulse.Tests;

[Collection(ApiCollection.Name)]
public class CommunicationEndpointsTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    // Client 1 is read-only in these tests; writes go to client 4 so ordering
    // assertions do not depend on test execution order.
    private const int ReadClientId = 1;
    private const int WriteClientId = 4;

    [Fact]
    public async Task GetCommunications_ReturnsSeededCommunicationsNewestFirst()
    {
        var communications = await _client.GetFromJsonAsync<List<CommunicationResponse>>(
            $"/api/clients/{ReadClientId}/communications", Json);

        Assert.NotNull(communications);
        var expectedIds = DemoSeedData.Communications
            .Where(c => c.ClientId == ReadClientId)
            .OrderByDescending(c => c.OccurredAt)
            .Select(c => c.Id);
        Assert.Equal(expectedIds, communications.Select(c => c.Id));
        Assert.All(communications, c => Assert.Equal(ReadClientId, c.ClientId));
    }

    [Fact]
    public async Task GetCommunications_SerializesTypeByName()
    {
        var json = await _client.GetStringAsync($"/api/clients/{ReadClientId}/communications");

        Assert.Contains("\"type\":\"Email\"", json);
    }

    [Fact]
    public async Task GetCommunications_UnknownClient_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/clients/999/communications");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommunication_UnknownClient_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/clients/999/communications", ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommunication_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/clients/{WriteClientId}/communications",
            new CreateCommunicationRequest("Fax", "not a date", "  ", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["body", "occurredAt", "subject", "type"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task CreateCommunication_Valid_PersistsAndAppearsInList()
    {
        var occurredAt = new DateTimeOffset(2026, 11, 1, 9, 30, 0, TimeSpan.FromHours(-6));
        var request = new CreateCommunicationRequest(
            "meeting", occurredAt.ToString("O"), "  Synthetic check-in  ", "Synthetic meeting notes.");

        var response = await _client.PostAsJsonAsync($"/api/clients/{WriteClientId}/communications", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommunicationResponse>(Json);
        Assert.NotNull(created);
        Assert.True(created.Id >= DemoSeedData.FirstGeneratedCommunicationId);
        Assert.Equal(WriteClientId, created.ClientId);
        Assert.Equal(CommunicationType.Meeting, created.Type);
        Assert.Equal("Synthetic check-in", created.Subject);
        Assert.Equal(occurredAt, created.OccurredAt);
        Assert.Equal(TimeSpan.Zero, created.OccurredAt.Offset);

        var list = await _client.GetFromJsonAsync<List<CommunicationResponse>>(
            $"/api/clients/{WriteClientId}/communications", Json);
        Assert.Contains(list!, c => c.Id == created.Id && c.Body == "Synthetic meeting notes.");
    }

    private static CreateCommunicationRequest ValidRequest() =>
        new("Email", "2026-11-01T12:00:00Z", "Synthetic subject", "Synthetic body.");
}
