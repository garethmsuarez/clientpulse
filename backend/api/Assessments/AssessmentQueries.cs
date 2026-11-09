using ClientPulse.Api.Analysis;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Assessments;

// Read-side projections of assessments: full responses with evidence, history
// summaries, and the client list's latest-per-client view. IsCurrent compares
// the stored input fingerprint with the client's communications as they are now.
public class AssessmentQueries(ClientPulseDbContext db)
{
    // Null when the client has no assessment yet. Callers check the client exists.
    public async Task<AssessmentResponse?> GetLatestAsync(int clientId, CancellationToken cancellationToken)
    {
        var latestId = await LatestIdAsync(clientId, cancellationToken);
        return latestId is null ? null : await GetAsync(clientId, latestId.Value, cancellationToken);
    }

    // Null when the assessment does not exist or belongs to another client.
    public async Task<AssessmentResponse?> GetAsync(int clientId, int assessmentId, CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == assessmentId && a.ClientId == clientId, cancellationToken);

        return assessment is null ? null : await ToResponseAsync(assessment, cancellationToken);
    }

    // Newest first. Callers check the client exists.
    public async Task<List<AssessmentSummaryResponse>> GetHistoryAsync(int clientId, CancellationToken cancellationToken)
    {
        // Order before projecting: EF cannot translate ordering on members of
        // a constructor-projected record.
        var rows = await SummaryRows(db.Assessments.Where(a => a.ClientId == clientId).NewestFirst())
            .ToListAsync(cancellationToken);

        var currentFingerprint = await CurrentFingerprintAsync(clientId, cancellationToken);
        var latestId = rows.FirstOrDefault()?.Id;

        return rows.Select(row => ToSummary(row, row.InputFingerprint == currentFingerprint, row.Id == latestId)).ToList();
    }

    // Each client's latest assessment, keyed by client id; clients without one
    // are absent. Two queries in total, however many clients there are.
    public async Task<Dictionary<int, AssessmentSummaryResponse>> GetLatestByClientAsync(CancellationToken cancellationToken)
    {
        var latest = db.Assessments.Where(a => !db.Assessments.Any(newer =>
            newer.ClientId == a.ClientId &&
            (newer.CreatedAt > a.CreatedAt || (newer.CreatedAt == a.CreatedAt && newer.Id > a.Id))));

        var rows = await SummaryRows(latest).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var clientIds = rows.Select(row => row.ClientId).ToList();
        var communications = await db.Communications
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId))
            .ToListAsync(cancellationToken);
        var fingerprints = communications
            .GroupBy(c => c.ClientId)
            .ToDictionary(group => group.Key, group => InputFingerprint.Compute(group));

        return rows.ToDictionary(
            row => row.ClientId,
            row => ToSummary(row, fingerprints.GetValueOrDefault(row.ClientId) == row.InputFingerprint, isLatest: true));
    }

    // Only the newest assessment is reviewable, so reviews use this too.
    public Task<int?> LatestIdAsync(int clientId, CancellationToken cancellationToken) =>
        db.Assessments
            .Where(a => a.ClientId == clientId)
            .NewestFirst()
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AssessmentResponse> ToResponseAsync(Assessment assessment, CancellationToken cancellationToken)
    {
        var current = await db.Communications
            .AsNoTracking()
            .Where(c => c.ClientId == assessment.ClientId)
            .ToListAsync(cancellationToken);

        var reviews = await db.AssessmentReviews
            .AsNoTracking()
            .Where(r => r.AssessmentId == assessment.Id)
            .NewestFirst()
            .ToListAsync(cancellationToken);

        var latestId = await LatestIdAsync(assessment.ClientId, cancellationToken);

        var evidenceById = current
            .Where(c => assessment.AnalyzedCommunicationIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => new EvidenceResponse(c.Id, c.Type, c.OccurredAt, c.Subject));

        IReadOnlyList<SignalResponse> Signals(SignalKind kind) => assessment.Signals
            .Where(signal => signal.Kind == kind)
            .Select(signal => new SignalResponse(
                signal.Description,
                signal.EvidenceCommunicationIds.Where(evidenceById.ContainsKey).Select(id => evidenceById[id]).ToList()))
            .ToList();

        var latestReview = reviews.FirstOrDefault();

        return new AssessmentResponse(
            assessment.Id,
            assessment.ClientId,
            assessment.CreatedAt,
            assessment.Source,
            assessment.Source == AnalysisSource.Fixture,
            assessment.FixtureName,
            assessment.RequestedModel,
            assessment.ResponseModel,
            assessment.Effort,
            AssessmentDisclaimer.For(assessment.Source, assessment.ResponseModel ?? assessment.RequestedModel),
            assessment.Summary,
            assessment.AttentionLevel,
            Signals(SignalKind.Concern),
            Signals(SignalKind.Positive),
            assessment.AnalyzedCommunicationIds,
            InputFingerprint.Compute(current) == assessment.InputFingerprint,
            assessment.Id == latestId,
            ReviewOutcome.Status(latestReview),
            ReviewOutcome.ApprovedLevel(assessment.AttentionLevel, latestReview),
            reviews.Select(ReviewOutcome.ToResponse).ToList());
    }

    private sealed record SummaryRow(
        int Id,
        int ClientId,
        DateTimeOffset CreatedAt,
        AnalysisSource Source,
        string? FixtureName,
        string? ResponseModel,
        AttentionLevel AttentionLevel,
        string InputFingerprint,
        AssessmentReview? LatestReview);

    private IQueryable<SummaryRow> SummaryRows(IQueryable<Assessment> assessments) =>
        assessments
            .AsNoTracking()
            .Select(a => new SummaryRow(
                a.Id,
                a.ClientId,
                a.CreatedAt,
                a.Source,
                a.FixtureName,
                a.ResponseModel,
                a.AttentionLevel,
                a.InputFingerprint,
                // Ordered inline: EF cannot translate a NewestFirst() call
                // nested inside the projection's expression tree.
                db.AssessmentReviews
                    .Where(r => r.AssessmentId == a.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenByDescending(r => r.Id)
                    .FirstOrDefault()));

    private static AssessmentSummaryResponse ToSummary(SummaryRow row, bool isCurrent, bool isLatest) => new(
        row.Id,
        row.CreatedAt,
        row.Source,
        row.Source == AnalysisSource.Fixture,
        row.FixtureName,
        row.ResponseModel,
        row.AttentionLevel,
        isCurrent,
        isLatest,
        ReviewOutcome.Status(row.LatestReview),
        ReviewOutcome.ApprovedLevel(row.AttentionLevel, row.LatestReview),
        row.LatestReview is null ? null : ReviewOutcome.ToResponse(row.LatestReview));

    private async Task<string> CurrentFingerprintAsync(int clientId, CancellationToken cancellationToken) =>
        InputFingerprint.Compute(await db.Communications
            .AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .ToListAsync(cancellationToken));
}
