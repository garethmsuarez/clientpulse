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
    ProviderFailed,
}

public record AnalyzeResult(
    AnalyzeStatus Status,
    AssessmentResponse? Assessment = null,
    string? Reason = null,
    AnalysisFailureKind? FailureKind = null);

// Runs one synchronous analysis: load the client's communications, invoke the
// analyzer, validate its output, and insert a new assessment. Any failure
// returns before saving, so previous assessments are never changed.
public class AssessmentService(
    ClientPulseDbContext db,
    ICommunicationAnalyzer analyzer,
    AssessmentQueries queries,
    TimeProvider timeProvider,
    ILogger<AssessmentService> logger)
{
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

        if (outcome is AnalysisFailed failed)
        {
            return new AnalyzeResult(AnalyzeStatus.ProviderFailed, FailureKind: failed.Kind);
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
                "Rejected invalid {Source} analyzer output for client {ClientId}: {Errors}",
                result.Source,
                clientId,
                string.Join(" ", errors));

            // Invalid output from an external provider is a 502; from the
            // in-process fixture analyzer it is a 500.
            return result.Source == AnalysisSource.Fixture
                ? new AnalyzeResult(AnalyzeStatus.InvalidOutput)
                : new AnalyzeResult(AnalyzeStatus.ProviderFailed, FailureKind: AnalysisFailureKind.InvalidOutput);
        }

        var assessment = Assessment.FromResult(clientId, result, communications, timeProvider.GetUtcNow());

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(cancellationToken);

        return new AnalyzeResult(AnalyzeStatus.Created, await queries.ToResponseAsync(assessment, cancellationToken));
    }
}
