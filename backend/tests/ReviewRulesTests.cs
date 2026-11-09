using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;

namespace ClientPulse.Tests;

public class ReviewRulesTests
{
    private const AttentionLevel Original = AttentionLevel.Medium;

    private static CreateReviewRequest Request(
        string? decision, string? level = null, string? notes = null, string? reviewer = "Synthetic Reviewer") =>
        new(decision, level, notes, reviewer);

    private static Dictionary<string, string[]> ErrorsFor(CreateReviewRequest request)
    {
        Assert.False(ReviewRules.TryValidate(request, Original, out var result, out var errors));
        Assert.Null(result);
        return errors;
    }

    [Fact]
    public void Accepted_WithoutNotes_IsValid()
    {
        Assert.True(ReviewRules.TryValidate(Request("accepted"), Original, out var review, out _));
        Assert.Equal(ReviewDecision.Accepted, review.Decision);
        Assert.Null(review.ReviewedAttentionLevel);
        Assert.Null(review.Notes);
    }

    [Fact]
    public void Accepted_WithOptionalNotes_TrimsThem()
    {
        Assert.True(ReviewRules.TryValidate(
            Request("Accepted", notes: "  Looks right.  ", reviewer: "  Synthetic Reviewer "), Original, out var review, out _));
        Assert.Equal("Looks right.", review.Notes);
        Assert.Equal("Synthetic Reviewer", review.ReviewerName);
    }

    [Fact]
    public void Adjusted_WithDifferentLevelAndNotes_IsValid()
    {
        Assert.True(ReviewRules.TryValidate(
            Request("Adjusted", "High", "Escalating after call."), Original, out var review, out _));
        Assert.Equal(ReviewDecision.Adjusted, review.Decision);
        Assert.Equal(AttentionLevel.High, review.ReviewedAttentionLevel);
    }

    [Fact]
    public void Rejected_WithNotes_IsValidAndHasNoLevel()
    {
        Assert.True(ReviewRules.TryValidate(
            Request("Rejected", notes: "Does not reflect the account."), Original, out var review, out _));
        Assert.Null(review.ReviewedAttentionLevel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NeedsFollowUp")]
    [InlineData("0")]
    public void MissingOrUnknownDecision_IsRejected(string? decision)
    {
        Assert.Equal(["decision"], ErrorsFor(Request(decision)).Keys);
    }

    [Fact]
    public void Adjusted_WithoutLevel_IsRejected()
    {
        Assert.Contains("reviewedAttentionLevel", ErrorsFor(Request("Adjusted", notes: "Synthetic.")).Keys);
    }

    [Fact]
    public void Adjusted_WithSameLevelAsOriginal_IsRejected()
    {
        Assert.Equal(["reviewedAttentionLevel"], ErrorsFor(Request("Adjusted", "Medium", "Synthetic.")).Keys);
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Rejected")]
    public void AcceptedOrRejected_WithLevel_IsRejected(string decision)
    {
        Assert.Contains("reviewedAttentionLevel", ErrorsFor(Request(decision, "High", "Synthetic.")).Keys);
    }

    [Fact]
    public void UnknownLevel_IsRejected()
    {
        Assert.Equal(["reviewedAttentionLevel"], ErrorsFor(Request("Adjusted", "Critical", "Synthetic.")).Keys);
    }

    [Theory]
    [InlineData("Adjusted", "High")]
    [InlineData("Rejected", null)]
    public void AdjustedOrRejected_WithoutNotes_IsRejected(string decision, string? level)
    {
        Assert.Equal(["notes"], ErrorsFor(Request(decision, level, "   ")).Keys);
    }

    [Fact]
    public void MissingReviewerName_IsRejected()
    {
        Assert.Equal(["reviewerName"], ErrorsFor(Request("Accepted", reviewer: "  ")).Keys);
    }

    [Fact]
    public void OverlongText_IsRejectedWithoutEchoingNotes()
    {
        var notes = "Synthetic-secret-note " + new string('n', ReviewRules.NotesMaxLength);

        var errors = ErrorsFor(Request("Rejected", notes: notes, reviewer: new string('r', ReviewRules.ReviewerNameMaxLength + 1)));

        Assert.Equal(["notes", "reviewerName"], errors.Keys.Order());
        Assert.DoesNotContain(errors.Values.SelectMany(m => m), m => m.Contains("Synthetic-secret-note"));
    }
}
