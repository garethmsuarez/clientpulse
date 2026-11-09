using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

// The pure mapping from a saved assessment to its API response.
public class AssessmentResponseTests
{
    private static readonly DateTimeOffset Day = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static Communication Comm(int id, string subject) => new()
    {
        Id = id,
        ClientId = 1,
        Type = CommunicationType.Email,
        OccurredAt = Day.AddDays(id),
        Subject = subject,
        Body = $"Synthetic body {id}",
        CreatedAt = Day,
    };

    private static readonly List<Communication> Analyzed = [Comm(10, "Late delivery"), Comm(11, "Renewal call")];

    private static Assessment Assessment() => new()
    {
        Id = 5,
        ClientId = 1,
        CreatedAt = Day,
        Source = AnalysisSource.Fixture,
        FixtureName = "synthetic",
        Summary = "Synthetic summary.",
        AttentionLevel = AttentionLevel.High,
        InputFingerprint = InputFingerprint.Compute(Analyzed),
        AnalyzedCommunicationIds = [10, 11],
        Signals =
        [
            new AssessmentSignal { Kind = SignalKind.Concern, Description = "Delays", EvidenceCommunicationIds = [10, 99] },
            new AssessmentSignal { Kind = SignalKind.Positive, Description = "Renewal", EvidenceCommunicationIds = [11] },
        ],
    };

    private static AssessmentReview Review(int id, ReviewDecision decision, AttentionLevel? level = null) => new()
    {
        Id = id,
        AssessmentId = 5,
        CreatedAt = Day.AddHours(id),
        Decision = decision,
        ReviewedAttentionLevel = level,
        Notes = "Synthetic note",
        ReviewerName = "Synthetic Reviewer",
    };

    [Fact]
    public void SplitsSignalsByKind_AndLinksOnlyAnalyzedEvidence()
    {
        var response = AssessmentResponse.From(Assessment(), Analyzed, [], latestAssessmentId: 5);

        var concern = Assert.Single(response.Concerns);
        Assert.Equal("Delays", concern.Description);
        // Id 99 was never analyzed, so it is not linked.
        Assert.Equal([10], concern.Evidence.Select(e => e.CommunicationId));
        Assert.Equal("Late delivery", concern.Evidence[0].Subject);

        var positive = Assert.Single(response.PositiveSignals);
        Assert.Equal([11], positive.Evidence.Select(e => e.CommunicationId));
    }

    [Fact]
    public void IsCurrentAndLatest_WhenInputUnchangedAndNewest()
    {
        var response = AssessmentResponse.From(Assessment(), Analyzed, [], latestAssessmentId: 5);

        Assert.True(response.IsCurrent);
        Assert.True(response.IsLatest);
        Assert.True(response.IsFixture);
        Assert.Equal(AssessmentDisclaimer.Fixture, response.Disclaimer);
    }

    [Fact]
    public void IsNotCurrent_WhenCommunicationsChanged_AndNotLatest_WhenNewerExists()
    {
        List<Communication> changed = [.. Analyzed, Comm(12, "New note")];

        var response = AssessmentResponse.From(Assessment(), changed, [], latestAssessmentId: 6);

        Assert.False(response.IsCurrent);
        Assert.False(response.IsLatest);
    }

    [Fact]
    public void ReviewOutcome_ComesFromTheFirstReview_WhichCallersPassNewestFirst()
    {
        List<AssessmentReview> newestFirst = [Review(2, ReviewDecision.Adjusted, AttentionLevel.Medium), Review(1, ReviewDecision.Accepted)];

        var response = AssessmentResponse.From(Assessment(), Analyzed, newestFirst, latestAssessmentId: 5);

        Assert.Equal(ReviewStatus.Adjusted, response.ReviewStatus);
        Assert.Equal(AttentionLevel.Medium, response.ReviewerApprovedAttentionLevel);
        Assert.Equal([2, 1], response.Reviews.Select(r => r.Id));
    }
}
