using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Communications;
using Microsoft.AspNetCore.Mvc;

namespace ClientPulse.Tests;

[Collection(ApiCollection.Name)]
public class AssessmentReviewEndpointsTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    // Northwind's fixture level is Medium.
    private Task<int> CreateClientAsync() =>
        TestClients.CreateAsync(factory, TestClients.Scenario("northwind-delivery-issues"));

    private async Task<AssessmentResponse> AnalyzeAsync(int clientId)
    {
        var response = await _client.PostAsync($"/api/clients/{clientId}/assessments", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AssessmentResponse>(Json))!;
    }

    private Task<HttpResponseMessage> ReviewAsync(int clientId, int assessmentId, CreateReviewRequest request) =>
        _client.PostAsJsonAsync($"/api/clients/{clientId}/assessments/{assessmentId}/reviews", request);

    private async Task<AssessmentResponse> GetAsync(int clientId, int assessmentId) =>
        (await _client.GetFromJsonAsync<AssessmentResponse>(
            $"/api/clients/{clientId}/assessments/{assessmentId}", Json))!;

    private static CreateReviewRequest Accept(string? notes = null) => new("Accepted", null, notes, "Synthetic Reviewer");

    [Fact]
    public async Task Review_IsSavedAndLeavesOriginalAssessmentUnchanged()
    {
        var clientId = await CreateClientAsync();
        var assessment = await AnalyzeAsync(clientId);

        var response = await ReviewAsync(clientId, assessment.Id,
            new CreateReviewRequest("Adjusted", "High", "Synthetic escalation note.", "  Synthetic Reviewer  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var review = (await response.Content.ReadFromJsonAsync<ReviewResponse>(Json))!;
        Assert.Equal(assessment.Id, review.AssessmentId);
        Assert.Equal(ReviewDecision.Adjusted, review.Decision);
        Assert.Equal(AttentionLevel.High, review.ReviewedAttentionLevel);
        Assert.Equal("Synthetic Reviewer", review.ReviewerName);

        var reloaded = await GetAsync(clientId, assessment.Id);
        Assert.Equal(AttentionLevel.Medium, reloaded.AttentionLevel);
        Assert.Equal(assessment.Summary, reloaded.Summary);
        Assert.Equal(
            assessment.Concerns.Select(c => c.Description),
            reloaded.Concerns.Select(c => c.Description));
        Assert.Equal(ReviewStatus.Adjusted, reloaded.ReviewStatus);
        Assert.Equal(AttentionLevel.High, reloaded.ReviewerApprovedAttentionLevel);
        Assert.Equal([review.Id], reloaded.Reviews.Select(r => r.Id));
    }

    [Fact]
    public async Task Reviews_AreAppendedNewestFirstAndNewestIsCurrent()
    {
        var clientId = await CreateClientAsync();
        var assessment = await AnalyzeAsync(clientId);

        var first = await ReviewAsync(clientId, assessment.Id, Accept());
        var second = await ReviewAsync(clientId, assessment.Id,
            new CreateReviewRequest("Rejected", null, "Synthetic rejection note.", "Another Reviewer"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var reloaded = await GetAsync(clientId, assessment.Id);
        Assert.Equal([ReviewDecision.Rejected, ReviewDecision.Accepted], reloaded.Reviews.Select(r => r.Decision));
        Assert.Equal(ReviewStatus.Rejected, reloaded.ReviewStatus);
        Assert.Null(reloaded.ReviewerApprovedAttentionLevel);
        Assert.Equal(AttentionLevel.Medium, reloaded.AttentionLevel);
    }

    [Fact]
    public async Task NewAssessment_StartsUnreviewed_AndOlderAssessmentCannotBeReviewed()
    {
        var clientId = await CreateClientAsync();
        var older = await AnalyzeAsync(clientId);
        Assert.Equal(HttpStatusCode.Created, (await ReviewAsync(clientId, older.Id, Accept())).StatusCode);

        var newer = await AnalyzeAsync(clientId);
        Assert.Equal(ReviewStatus.Unreviewed, newer.ReviewStatus);
        Assert.Empty(newer.Reviews);
        Assert.True(newer.IsLatest);

        var response = await ReviewAsync(clientId, older.Id, Accept());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(AssessmentProblems.NotLatestAssessmentCode, problem?.Extensions["code"]?.ToString());

        var olderReloaded = await GetAsync(clientId, older.Id);
        Assert.False(olderReloaded.IsLatest);
        Assert.Single(olderReloaded.Reviews);
    }

    [Fact]
    public async Task InvalidReview_ReturnsFieldErrorsAndSavesNothing()
    {
        var clientId = await CreateClientAsync();
        var assessment = await AnalyzeAsync(clientId);

        var response = await ReviewAsync(clientId, assessment.Id, new CreateReviewRequest("Adjusted", "Medium", " ", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["notes", "reviewedAttentionLevel", "reviewerName"], problem!.Errors.Keys.Order());
        Assert.Empty((await GetAsync(clientId, assessment.Id)).Reviews);
    }

    [Fact]
    public async Task UnknownClientOrAssessment_ReturnsNotFound()
    {
        var clientId = await CreateClientAsync();
        var otherClientId = await CreateClientAsync();
        var assessment = await AnalyzeAsync(clientId);

        Assert.Equal(HttpStatusCode.NotFound, (await ReviewAsync(999_999, assessment.Id, Accept())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReviewAsync(clientId, int.MaxValue, Accept())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReviewAsync(otherClientId, assessment.Id, Accept())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/clients/{otherClientId}/assessments/{assessment.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/clients/999999/assessments")).StatusCode);
    }

    [Fact]
    public async Task History_ListsAssessmentsNewestFirstWithTheirOwnReviewStatus()
    {
        var clientId = await CreateClientAsync();
        var first = await AnalyzeAsync(clientId);
        await ReviewAsync(clientId, first.Id, Accept());
        var second = await AnalyzeAsync(clientId);
        await ReviewAsync(clientId, second.Id, new CreateReviewRequest("Adjusted", "Low", "Synthetic.", "Synthetic Reviewer"));
        var third = await AnalyzeAsync(clientId);

        var history = await _client.GetFromJsonAsync<List<AssessmentSummaryResponse>>(
            $"/api/clients/{clientId}/assessments", Json);

        Assert.Equal([third.Id, second.Id, first.Id], history!.Select(a => a.Id));
        Assert.Equal([true, false, false], history.Select(a => a.IsLatest));
        Assert.Equal(
            [ReviewStatus.Unreviewed, ReviewStatus.Adjusted, ReviewStatus.Accepted],
            history.Select(a => a.ReviewStatus));
        Assert.Equal([null, AttentionLevel.Low, AttentionLevel.Medium], history.Select(a => a.ReviewerApprovedAttentionLevel));
        Assert.Null(history[0].LatestReview);
        Assert.Equal(ReviewDecision.Accepted, history[2].LatestReview?.Decision);
        Assert.All(history, a => Assert.True(a.IsFixture));
    }

    [Fact]
    public async Task History_ForClientWithoutAssessments_IsEmpty()
    {
        var clientId = await CreateClientAsync();

        var history = await _client.GetFromJsonAsync<List<AssessmentSummaryResponse>>(
            $"/api/clients/{clientId}/assessments", Json);

        Assert.Empty(history!);
    }

    [Fact]
    public async Task History_MarksAssessmentsAsNotCurrentAfterCommunicationsChange()
    {
        var clientId = await CreateClientAsync();
        await AnalyzeAsync(clientId);
        await _client.PostAsJsonAsync($"/api/clients/{clientId}/communications",
            new CreateCommunicationRequest("Note", "2026-11-05T10:00:00Z", "Synthetic follow-up", "Synthetic note."));

        var history = await _client.GetFromJsonAsync<List<AssessmentSummaryResponse>>(
            $"/api/clients/{clientId}/assessments", Json);

        Assert.False(Assert.Single(history!).IsCurrent);
    }

    [Fact]
    public async Task CreatedAssessment_LocationHeaderResolves_AndLatestIncludesReviews()
    {
        var clientId = await CreateClientAsync();
        var response = await _client.PostAsync($"/api/clients/{clientId}/assessments", null);
        var created = (await response.Content.ReadFromJsonAsync<AssessmentResponse>(Json))!;
        await ReviewAsync(clientId, created.Id, Accept("Synthetic note."));

        var viaLocation = await _client.GetFromJsonAsync<AssessmentResponse>(response.Headers.Location, Json);
        var latest = await _client.GetFromJsonAsync<AssessmentResponse>(
            $"/api/clients/{clientId}/assessments/latest", Json);

        Assert.Equal(created.Id, viaLocation!.Id);
        Assert.Equal(created.Id, latest!.Id);
        Assert.Equal("Synthetic note.", Assert.Single(latest.Reviews).Notes);
        Assert.Equal(ReviewStatus.Accepted, latest.ReviewStatus);
    }
}
