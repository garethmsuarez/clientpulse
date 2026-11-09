using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Assessments;

public enum ReviewSubmitStatus
{
    Created,
    NotFound,
    NotLatest,
    Invalid,
}

public record ReviewSubmitResult(
    ReviewSubmitStatus Status,
    ReviewResponse? Review = null,
    Dictionary<string, string[]>? Errors = null);

// Records a human review. Only the client's newest assessment can be reviewed;
// older ones are read-only history. Reviews are appended, never edited or deleted.
public class ReviewService(ClientPulseDbContext db, AssessmentQueries queries, TimeProvider timeProvider)
{
    public async Task<ReviewSubmitResult> AddReviewAsync(
        int clientId, int assessmentId, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments
            .AsNoTracking()
            .Where(a => a.Id == assessmentId && a.ClientId == clientId)
            .Select(a => new { a.Id, a.AttentionLevel })
            .SingleOrDefaultAsync(cancellationToken);

        if (assessment is null)
        {
            return new ReviewSubmitResult(ReviewSubmitStatus.NotFound);
        }

        if (await queries.LatestIdAsync(clientId, cancellationToken) != assessment.Id)
        {
            return new ReviewSubmitResult(ReviewSubmitStatus.NotLatest);
        }

        if (!ReviewRules.TryValidate(request, assessment.AttentionLevel, out var valid, out var errors))
        {
            return new ReviewSubmitResult(ReviewSubmitStatus.Invalid, Errors: errors);
        }

        var review = new AssessmentReview
        {
            AssessmentId = assessment.Id,
            CreatedAt = timeProvider.GetUtcNow(),
            Decision = valid.Decision,
            ReviewedAttentionLevel = valid.ReviewedAttentionLevel,
            Notes = valid.Notes,
            ReviewerName = valid.ReviewerName,
        };

        db.AssessmentReviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        return new ReviewSubmitResult(ReviewSubmitStatus.Created, ReviewOutcome.ToResponse(review));
    }
}
