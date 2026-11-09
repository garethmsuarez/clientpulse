using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Clients;
using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

[Collection(ApiCollection.Name)]
public class ClientListEndpointsTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private Task<int> CreateClientAsync(string? scenario = "cedar-ridge-renewal-risk") =>
        TestClients.CreateAsync(factory, scenario is null ? null : TestClients.Scenario(scenario));

    private async Task<AssessmentResponse> AnalyzeAsync(int clientId) =>
        (await (await _client.PostAsync($"/api/clients/{clientId}/assessments", null))
            .Content.ReadFromJsonAsync<AssessmentResponse>(Json))!;

    private Task ReviewAsync(int clientId, int assessmentId, CreateReviewRequest request) =>
        _client.PostAsJsonAsync($"/api/clients/{clientId}/assessments/{assessmentId}/reviews", request);

    private async Task<ClientSummary> ListEntryAsync(int clientId)
    {
        var clients = await _client.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);
        return Assert.Single(clients!, c => c.Id == clientId);
    }

    [Fact]
    public async Task ClientWithoutAssessment_HasNullLatestAssessment()
    {
        var clientId = await CreateClientAsync(scenario: null);

        Assert.Null((await ListEntryAsync(clientId)).LatestAssessment);
    }

    [Fact]
    public async Task LatestAssessment_IsTheNewestWithItsOwnReviewOutcome()
    {
        var clientId = await CreateClientAsync();
        var first = await AnalyzeAsync(clientId);
        await ReviewAsync(clientId, first.Id, new CreateReviewRequest("Accepted", null, null, "Synthetic Reviewer"));
        var second = await AnalyzeAsync(clientId);

        var unreviewed = (await ListEntryAsync(clientId)).LatestAssessment!;
        Assert.Equal(second.Id, unreviewed.Id);
        Assert.True(unreviewed.IsLatest);
        Assert.True(unreviewed.IsCurrent);
        Assert.True(unreviewed.IsFixture);
        Assert.Equal(AttentionLevel.High, unreviewed.AttentionLevel);
        Assert.Equal(ReviewStatus.Unreviewed, unreviewed.ReviewStatus);
        Assert.Null(unreviewed.ReviewerApprovedAttentionLevel);

        await ReviewAsync(clientId, second.Id,
            new CreateReviewRequest("Adjusted", "Medium", "Synthetic note.", "Synthetic Reviewer"));

        var adjusted = (await ListEntryAsync(clientId)).LatestAssessment!;
        Assert.Equal(ReviewStatus.Adjusted, adjusted.ReviewStatus);
        Assert.Equal(AttentionLevel.Medium, adjusted.ReviewerApprovedAttentionLevel);
        Assert.Equal(AttentionLevel.High, adjusted.AttentionLevel);
    }

    [Fact]
    public async Task LatestAssessment_IsMarkedNotCurrentAfterCommunicationsChange()
    {
        var clientId = await CreateClientAsync();
        await AnalyzeAsync(clientId);
        await _client.PostAsJsonAsync($"/api/clients/{clientId}/communications",
            new CreateCommunicationRequest("Note", "2026-11-05T10:00:00Z", "Synthetic follow-up", "Synthetic note."));

        Assert.False((await ListEntryAsync(clientId)).LatestAssessment!.IsCurrent);
    }

    [Fact]
    public async Task List_KeepsNameOrder()
    {
        var clients = await _client.GetFromJsonAsync<List<ClientSummary>>("/api/clients", Json);

        var seeded = clients!.Where(c => c.Id <= 5).Select(c => c.Name).ToList();
        Assert.Equal(
            ["Bluefin Health Partners", "Cedar Ridge Manufacturing", "Harbourview Financial", "Lumen Retail Group", "Northwind Logistics"],
            seeded);
    }
}
