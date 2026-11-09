using ClientPulse.Api.Data;

namespace ClientPulse.Api.Analysis;

// Deterministic stand-in for a real analyzer. It returns a predefined result
// only when the submitted communications are exactly the synthetic seed set a
// scenario was written for (same type, time, subject, and body; nothing added
// or removed). Ids are ignored for matching; evidence is mapped onto the ids
// of the communications actually submitted.
public class FixtureCommunicationAnalyzer : ICommunicationAnalyzer
{
    public const string UnsupportedReason =
        "No fixture scenario matches this client's current communications. " +
        "Fixture analysis only supports the predefined synthetic scenarios, unchanged.";

    private static readonly IReadOnlyDictionary<string, FixtureScenario> ScenariosByFingerprint =
        FixtureScenarios.All.ToDictionary(scenario => InputFingerprint.Compute(
            scenario.CommunicationSubjects.Select(subject =>
                ClientPulseDbContext.SeedCommunications.Single(seed => seed.Subject == subject))));

    public Task<AnalyzerOutcome> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var fingerprint = InputFingerprint.Compute(request.Communications);
        if (request.Communications.Count == 0 || !ScenariosByFingerprint.TryGetValue(fingerprint, out var scenario))
        {
            return Task.FromResult<AnalyzerOutcome>(new AnalysisUnsupported(UnsupportedReason));
        }

        // An exact content match guarantees each subject appears once in the input.
        var idsBySubject = request.Communications.ToDictionary(c => c.Subject, c => c.Id);

        var result = new AnalysisResult(
            AnalysisSource.Fixture,
            scenario.Name,
            scenario.Summary,
            scenario.AttentionLevel,
            scenario.Signals
                .Select(signal => new AnalysisSignal(
                    signal.Kind,
                    signal.Description,
                    signal.EvidenceSubjects.Select(subject => idsBySubject[subject]).ToList()))
                .ToList());

        return Task.FromResult<AnalyzerOutcome>(new AnalysisCompleted(result));
    }
}
