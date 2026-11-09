namespace ClientPulse.Api.Assessments;

// "Newest" means latest CreatedAt, with the higher id breaking ties, so the
// order is stable when two rows share a timestamp.
public static class AssessmentOrdering
{
    public static IOrderedQueryable<Assessment> NewestFirst(this IQueryable<Assessment> assessments) =>
        assessments.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id);

    public static IOrderedQueryable<AssessmentReview> NewestFirst(this IQueryable<AssessmentReview> reviews) =>
        reviews.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
}
