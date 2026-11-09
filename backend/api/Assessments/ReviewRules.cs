using System.Diagnostics.CodeAnalysis;
using ClientPulse.Api.Analysis;

namespace ClientPulse.Api.Assessments;

public record CreateReviewRequest(string? Decision, string? ReviewedAttentionLevel, string? Notes, string? ReviewerName);

public record ValidReview(ReviewDecision Decision, AttentionLevel? ReviewedAttentionLevel, string? Notes, string ReviewerName);

public static class ReviewRules
{
    public const int NotesMaxLength = 2_000;
    public const int ReviewerNameMaxLength = 100;

    // Error messages never include the submitted notes.
    public static bool TryValidate(
        CreateReviewRequest request,
        AttentionLevel originalLevel,
        [NotNullWhen(true)] out ValidReview? result,
        out Dictionary<string, string[]> errors)
    {
        errors = [];

        var decision = ParseName<ReviewDecision>(request.Decision);
        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            errors["decision"] = ["Decision is required."];
        }
        else if (decision is null)
        {
            errors["decision"] = [$"Decision must be one of: {string.Join(", ", Enum.GetNames<ReviewDecision>())}."];
        }

        AttentionLevel? level = null;
        if (!string.IsNullOrWhiteSpace(request.ReviewedAttentionLevel))
        {
            level = ParseName<AttentionLevel>(request.ReviewedAttentionLevel);
            if (level is null)
            {
                errors["reviewedAttentionLevel"] =
                    [$"Attention level must be one of: {string.Join(", ", Enum.GetNames<AttentionLevel>())}."];
            }
        }

        if (!errors.ContainsKey("reviewedAttentionLevel"))
        {
            if (decision == ReviewDecision.Adjusted && level is null)
            {
                errors["reviewedAttentionLevel"] = ["Adjusted reviews require a replacement attention level."];
            }
            else if (decision == ReviewDecision.Adjusted && level == originalLevel)
            {
                errors["reviewedAttentionLevel"] = ["The replacement level must differ from the original level."];
            }
            else if (decision is ReviewDecision.Accepted or ReviewDecision.Rejected && level is not null)
            {
                errors["reviewedAttentionLevel"] = [$"{decision} reviews must not set an attention level."];
            }
        }

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes is null && decision is ReviewDecision.Adjusted or ReviewDecision.Rejected)
        {
            errors["notes"] = [$"Notes are required for {decision} reviews."];
        }
        else if (notes?.Length > NotesMaxLength)
        {
            errors["notes"] = [$"Notes must be {NotesMaxLength:N0} characters or fewer."];
        }

        var reviewerName = request.ReviewerName?.Trim() ?? "";
        if (reviewerName.Length == 0)
        {
            errors["reviewerName"] = ["Reviewer name is required."];
        }
        else if (reviewerName.Length > ReviewerNameMaxLength)
        {
            errors["reviewerName"] = [$"Reviewer name must be {ReviewerNameMaxLength} characters or fewer."];
        }

        if (errors.Count > 0 || decision is null)
        {
            result = null;
            return false;
        }

        result = new ValidReview(decision.Value, level, notes, reviewerName);
        return true;
    }

    // Accept names only (case-insensitive); Enum.TryParse would also accept numbers.
    private static T? ParseName<T>(string? value) where T : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (string.Equals(candidate.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
