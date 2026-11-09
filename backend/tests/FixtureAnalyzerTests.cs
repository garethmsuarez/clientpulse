using ClientPulse.Api.Analysis;
using ClientPulse.Api.Data;

namespace ClientPulse.Tests;

public class FixtureAnalyzerTests
{
    private readonly FixtureCommunicationAnalyzer _analyzer = new();

    // Copies of a scenario's seed communications with fresh ids, as if the
    // database had been recreated or the data belonged to another client.
    private static List<AnalysisCommunication> ScenarioInput(FixtureScenario scenario, int firstId = 5000) =>
        scenario.CommunicationSubjects
            .Select(subject => ClientPulseDbContext.SeedCommunications.Single(c => c.Subject == subject))
            .Select((c, index) => new AnalysisCommunication(firstId + index, c.Type, c.OccurredAt, c.Subject, c.Body))
            .ToList();

    private async Task<AnalyzerOutcome> Analyze(IReadOnlyList<AnalysisCommunication> communications) =>
        await _analyzer.AnalyzeAsync(new AnalysisRequest(1, communications), CancellationToken.None);

    public static TheoryData<string> ScenarioNames() => new(FixtureScenarios.All.Select(s => s.Name));

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task MatchingInput_ReturnsScenarioWithEvidenceMappedToSuppliedIds(string name)
    {
        var scenario = FixtureScenarios.All.Single(s => s.Name == name);
        var input = ScenarioInput(scenario);
        input.Reverse(); // order must not matter

        var outcome = await Analyze(input);

        var result = Assert.IsType<AnalysisCompleted>(outcome).Result;
        Assert.Equal(AnalysisSource.Fixture, result.Source);
        Assert.Equal(scenario.Name, result.FixtureName);
        Assert.Equal(scenario.AttentionLevel, result.AttentionLevel);
        var suppliedIds = input.Select(c => c.Id).ToHashSet();
        Assert.All(result.Signals, signal => Assert.All(signal.EvidenceCommunicationIds,
            id => Assert.Contains(id, suppliedIds)));

        var bySubject = input.ToDictionary(c => c.Subject, c => c.Id);
        Assert.Equal(
            scenario.Signals.Select(s => s.EvidenceSubjects.Select(subject => bySubject[subject])),
            result.Signals.Select(s => s.EvidenceCommunicationIds));
    }

    [Fact]
    public async Task AddedCommunication_IsUnsupported()
    {
        var input = ScenarioInput(FixtureScenarios.All[0]);
        input.Add(input[0] with { Id = 9999, Subject = "Synthetic extra" });

        Assert.IsType<AnalysisUnsupported>(await Analyze(input));
    }

    [Fact]
    public async Task RemovedCommunication_IsUnsupported()
    {
        var input = ScenarioInput(FixtureScenarios.All[0]);
        input.RemoveAt(0);

        Assert.IsType<AnalysisUnsupported>(await Analyze(input));
    }

    [Fact]
    public async Task ModifiedCommunication_IsUnsupported()
    {
        var input = ScenarioInput(FixtureScenarios.All[0]);
        input[1] = input[1] with { Body = input[1].Body + " Edited." };

        Assert.IsType<AnalysisUnsupported>(await Analyze(input));
    }

    [Fact]
    public async Task EmptyInput_IsUnsupported()
    {
        Assert.IsType<AnalysisUnsupported>(await Analyze([]));
    }

    [Fact]
    public async Task CombinedScenarios_AreUnsupported()
    {
        var input = ScenarioInput(FixtureScenarios.All[0], 5000)
            .Concat(ScenarioInput(FixtureScenarios.All[1], 6000))
            .ToList();

        Assert.IsType<AnalysisUnsupported>(await Analyze(input));
    }
}
