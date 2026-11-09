using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Communications;
using ClientPulse.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientPulse.Tests;

// Each test creates its own client so tests stay independent of each other
// and of the seeded clients (other tests add communications to those).
[Collection(ApiCollection.Name)]
public class AssessmentEndpointsTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static FixtureScenario Scenario(string name) => TestClients.Scenario(name);

    private Task<int> CreateClientAsync(FixtureScenario? scenario) => TestClients.CreateAsync(factory, scenario);

    private async Task<int> AssessmentCountAsync(int clientId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientPulseDbContext>();
        return await db.Assessments.CountAsync(a => a.ClientId == clientId);
    }

    private static async Task<AssessmentResponse> ReadAssessment(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<AssessmentResponse>(Json))!;

    private Task<HttpResponseMessage> Analyze(int clientId, HttpClient? client = null) =>
        (client ?? _client).PostAsync($"/api/clients/{clientId}/assessments", null);

    private async Task<AssessmentResponse> GetLatest(int clientId)
    {
        var response = await _client.GetAsync($"/api/clients/{clientId}/assessments/latest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAssessment(response);
    }

    private static async Task AssertProblemCode(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(code, problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Analyze_SupportedScenario_PersistsLabelledFixtureAssessment()
    {
        var scenario = Scenario("cedar-ridge-renewal-risk");
        var clientId = await CreateClientAsync(scenario);

        var response = await Analyze(clientId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assessment = await ReadAssessment(response);
        Assert.Equal(clientId, assessment.ClientId);
        Assert.Equal(AnalysisSource.Fixture, assessment.Source);
        Assert.True(assessment.IsFixture);
        Assert.Equal(scenario.Name, assessment.FixtureName);
        Assert.Contains("Fixture result", assessment.Disclaimer);
        Assert.Equal(AttentionLevel.High, assessment.AttentionLevel);
        Assert.Equal(scenario.Summary, assessment.Summary);
        Assert.Equal(3, assessment.Concerns.Count);
        Assert.True(assessment.IsCurrent);
        Assert.Equal(1, await AssessmentCountAsync(clientId));
    }

    [Fact]
    public async Task Analyze_EvidenceReferencesAreTraceableToThisClientsAnalyzedCommunications()
    {
        var clientId = await CreateClientAsync(Scenario("northwind-delivery-issues"));

        var assessment = await ReadAssessment(await Analyze(clientId));

        var communications = await _client.GetFromJsonAsync<List<CommunicationResponse>>(
            $"/api/clients/{clientId}/communications", Json);
        var byId = communications!.ToDictionary(c => c.Id);
        Assert.Equal(byId.Keys.Order(), assessment.AnalyzedCommunicationIds.Order());

        var evidence = assessment.Concerns.Concat(assessment.PositiveSignals).SelectMany(s => s.Evidence).ToList();
        Assert.NotEmpty(evidence);
        Assert.All(evidence, item =>
        {
            Assert.True(item.CommunicationId >= DemoSeedData.FirstGeneratedCommunicationId,
                "Evidence should point at this client's copies, not the seed rows.");
            var communication = byId[item.CommunicationId];
            Assert.Equal(communication.Subject, item.Subject);
            Assert.Equal(communication.OccurredAt, item.OccurredAt);
        });
    }

    [Fact]
    public async Task GetLatest_ReturnsAssessmentForTheRequestedClientOnly()
    {
        var lumenId = await CreateClientAsync(Scenario("lumen-steady-engagement"));
        var harbourviewId = await CreateClientAsync(Scenario("harbourview-audit-escalation"));
        var unanalyzedId = await CreateClientAsync(Scenario("bluefin-expansion-interest"));
        var lumen = await ReadAssessment(await Analyze(lumenId));
        var harbourview = await ReadAssessment(await Analyze(harbourviewId));

        Assert.Equal(lumen.Id, (await GetLatest(lumenId)).Id);
        Assert.Equal(harbourview.Id, (await GetLatest(harbourviewId)).Id);
        Assert.Equal(AttentionLevel.Low, (await GetLatest(lumenId)).AttentionLevel);

        var none = await _client.GetAsync($"/api/clients/{unanalyzedId}/assessments/latest");
        Assert.Equal(HttpStatusCode.NoContent, none.StatusCode);
        Assert.Empty(await none.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UnknownClient_ReturnsNotFound()
    {
        var analyze = await Analyze(999_999);
        var latest = await _client.GetAsync("/api/clients/999999/assessments/latest");

        Assert.Equal(HttpStatusCode.NotFound, analyze.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, latest.StatusCode);
    }

    [Fact]
    public async Task Analyze_ClientWithoutCommunications_ReturnsConflict()
    {
        var clientId = await CreateClientAsync(scenario: null);

        await AssertProblemCode(await Analyze(clientId), HttpStatusCode.Conflict, AssessmentProblems.NoCommunicationsCode);
        Assert.Equal(0, await AssessmentCountAsync(clientId));
    }

    [Fact]
    public async Task Analyze_AfterCommunicationAdded_IsUnsupportedAndPreservesPreviousAssessment()
    {
        var clientId = await CreateClientAsync(Scenario("northwind-delivery-issues"));
        var original = await ReadAssessment(await Analyze(clientId));

        var added = await _client.PostAsJsonAsync($"/api/clients/{clientId}/communications",
            new CreateCommunicationRequest("Note", "2026-11-05T10:00:00Z", "Synthetic follow-up", "Synthetic note."));
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);

        await AssertProblemCode(await Analyze(clientId), HttpStatusCode.UnprocessableEntity,
            AssessmentProblems.UnsupportedInputCode);

        var latest = await GetLatest(clientId);
        Assert.Equal(original.Id, latest.Id);
        Assert.Equal(original.Summary, latest.Summary);
        Assert.False(latest.IsCurrent);
        Assert.Equal(1, await AssessmentCountAsync(clientId));
    }

    [Fact]
    public async Task Analyze_InvalidAnalyzerOutput_ReturnsServerErrorWithoutSaving()
    {
        var clientId = await CreateClientAsync(Scenario("bluefin-expansion-interest"));
        var original = await ReadAssessment(await Analyze(clientId));

        using var faulty = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ICommunicationAnalyzer>(new EvidenceOutsideInputAnalyzer())));

        await AssertProblemCode(await Analyze(clientId, faulty.CreateClient()),
            HttpStatusCode.InternalServerError, AssessmentProblems.InvalidAnalyzerOutputCode);

        Assert.Equal(1, await AssessmentCountAsync(clientId));
        Assert.Equal(original.Id, (await GetLatest(clientId)).Id);
    }

    [Fact]
    public async Task Analyze_Repeated_CreatesNewAssessmentEachTime()
    {
        var clientId = await CreateClientAsync(Scenario("lumen-steady-engagement"));

        var first = await ReadAssessment(await Analyze(clientId));
        var second = await ReadAssessment(await Analyze(clientId));

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, await AssessmentCountAsync(clientId));
        Assert.Equal(second.Id, (await GetLatest(clientId)).Id);
    }

    // Returns otherwise-valid output whose evidence cites a communication that
    // was not part of the analyzed input.
    private sealed class EvidenceOutsideInputAnalyzer : ICommunicationAnalyzer
    {
        public Task<AnalyzerOutcome> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<AnalyzerOutcome>(new AnalysisCompleted(new AnalysisResult(
                AnalysisSource.Fixture,
                "invalid-test-output",
                "Synthetic summary.",
                AttentionLevel.Low,
                [
                    new AnalysisSignal(SignalKind.Concern, "Valid evidence.", [request.Communications[0].Id]),
                    new AnalysisSignal(SignalKind.Concern, "Invalid evidence.", [int.MaxValue]),
                ])));
    }
}
