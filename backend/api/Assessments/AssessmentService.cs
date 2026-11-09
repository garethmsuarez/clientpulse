using ClientPulse.Api.Analysis;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Assessments;

public enum AnalyzeStatus
{
    Created,
    ClientNotFound,
    NoCommunications,
    Unsupported,
    InvalidOutput,
}

public record AnalyzeResult(AnalyzeStatus Status, AssessmentResponse? Assessment = null, string? Reason = null);

// Runs one synchronous analysis: load the client's communications, invoke the
// analyzer, validate its output, and insert a new assessment. Any failure
// returns before saving, so previous assessments are never changed.
public class AssessmentService(
    ClientPulseDbContext db,
    ICommunicationAnalyzer analyzer,
    TimeProvider timeProvider,
    ILogger<AssessmentService> logger)
{
    public const string FixtureDisclaimer =
        "Fixture result: predefined for a synthetic demo scenario. Not produced by AI or any real analysis.";

    public async Task<AnalyzeResult> AnalyzeAsync(int clientId, CancellationToken cancellationToken)
    {
        if (!await db.Clients.AnyAsync(client => client.Id == clientId, cancellationToken))
        {
            return new AnalyzeResult(AnalyzeStatus.ClientNotFound);
        }

        var communications = await db.Communications
            .AsNoTracking()
            .Where(communication => communication.ClientId == clientId)
            .OrderBy(communication => communication.OccurredAt)
            .ThenBy(communication => communication.Id)
            .Select(communication => new AnalysisCommunication(
                communication.Id,
                communication.Type,
                communication.OccurredAt,
                communication.Subject,
                communication.Body))
            .ToListAsync(cancellationToken);

        if (communications.Count == 0)
        {
            return new AnalyzeResult(AnalyzeStatus.NoCommunications);
        }

        var outcome = await analyzer.AnalyzeAsync(new AnalysisRequest(clientId, communications), cancellationToken);

        if (outcome is AnalysisUnsupported unsupported)
        {
            return new AnalyzeResult(AnalyzeStatus.Unsupported, Reason: unsupported.Reason);
        }

        if (outcome is not AnalysisCompleted { Result: var result })
        {
            logger.LogWarning("Analyzer returned an unrecognised outcome for client {ClientId}", clientId);
            return new AnalyzeResult(AnalyzeStatus.InvalidOutput);
        }

        var inputIds = communications.Select(c => c.Id).ToHashSet();
        var errors = AssessmentOutputRules.Validate(result, inputIds);
        if (errors.Count > 0)
        {
            // Errors reference fields and ids only, never communication text.
            logger.LogWarning(
                "Rejected invalid analyzer output for client {ClientId}: {Errors}",
                clientId,
                string.Join(" ", errors));
            return new AnalyzeResult(AnalyzeStatus.InvalidOutput);
        }

        var assessment = new Assessment
        {
            ClientId = clientId,
            CreatedAt = timeProvider.GetUtcNow(),
            Source = result.Source,
            FixtureName = result.FixtureName,
            Summary = result.Summary,
            AttentionLevel = result.AttentionLevel,
            InputFingerprint = InputFingerprint.Compute(communications),
            AnalyzedCommunicationIds = communications.Select(c => c.Id).ToList(),
            Signals = result.Signals
                .Select(signal => new AssessmentSignal
                {
                    Kind = signal.Kind,
                    Description = signal.Description,
                    EvidenceCommunicationIds = signal.EvidenceCommunicationIds.ToList(),
                })
                .ToList(),
        };

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(cancellationToken);

        return new AnalyzeResult(AnalyzeStatus.Created, await ToResponseAsync(assessment, cancellationToken));
    }

    // Null when the client has no assessment yet. Callers check the client exists.
    public async Task<AssessmentResponse?> GetLatestAsync(int clientId, CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments
            .AsNoTracking()
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return assessment is null ? null : await ToResponseAsync(assessment, cancellationToken);
    }

    private async Task<AssessmentResponse> ToResponseAsync(Assessment assessment, CancellationToken cancellationToken)
    {
        var current = await db.Communications
            .AsNoTracking()
            .Where(c => c.ClientId == assessment.ClientId)
            .ToListAsync(cancellationToken);

        var evidenceById = current
            .Where(c => assessment.AnalyzedCommunicationIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => new EvidenceResponse(c.Id, c.Type, c.OccurredAt, c.Subject));

        IReadOnlyList<SignalResponse> Signals(SignalKind kind) => assessment.Signals
            .Where(signal => signal.Kind == kind)
            .Select(signal => new SignalResponse(
                signal.Description,
                signal.EvidenceCommunicationIds.Where(evidenceById.ContainsKey).Select(id => evidenceById[id]).ToList()))
            .ToList();

        var isFixture = assessment.Source == AnalysisSource.Fixture;

        return new AssessmentResponse(
            assessment.Id,
            assessment.ClientId,
            assessment.CreatedAt,
            assessment.Source,
            isFixture,
            assessment.FixtureName,
            isFixture ? FixtureDisclaimer : null,
            assessment.Summary,
            assessment.AttentionLevel,
            Signals(SignalKind.Concern),
            Signals(SignalKind.Positive),
            assessment.AnalyzedCommunicationIds,
            InputFingerprint.Compute(current) == assessment.InputFingerprint);
    }
}
