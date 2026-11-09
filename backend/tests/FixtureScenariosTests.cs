using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Data;

namespace ClientPulse.Tests;

public class FixtureScenariosTests
{
    [Fact]
    public void EverySeededClientScenarioHasExactlyOneFixture()
    {
        var seedSubjectsByClient = DemoSeedData.Communications
            .GroupBy(c => c.ClientId)
            .Select(g => g.Select(c => c.Subject).Order().ToList())
            .ToList();
        var fixtureSubjects = FixtureScenarios.All
            .Select(s => s.CommunicationSubjects.Order().ToList())
            .ToList();

        Assert.Equal(seedSubjectsByClient.Count, fixtureSubjects.Count);
        Assert.All(seedSubjectsByClient, expected => Assert.Single(fixtureSubjects, actual => actual.SequenceEqual(expected)));
    }

    [Fact]
    public void SeedSubjectsAreUniqueSoFixturesCanReferenceThem()
    {
        var subjects = DemoSeedData.Communications.Select(c => c.Subject).ToList();

        Assert.Equal(subjects.Count, subjects.Distinct().Count());
    }

    [Fact]
    public void ScenariosAreWellFormed()
    {
        Assert.Equal(FixtureScenarios.All.Count, FixtureScenarios.All.Select(s => s.Name).Distinct().Count());
        Assert.All(FixtureScenarios.All, scenario =>
        {
            Assert.InRange(scenario.Name.Length, 1, AssessmentOutputRules.FixtureNameMaxLength);
            Assert.InRange(scenario.Summary.Length, 1, AssessmentOutputRules.SummaryMaxLength);
            Assert.NotEmpty(scenario.Signals);
            Assert.All(scenario.Signals, signal =>
            {
                Assert.InRange(signal.Description.Length, 1, AssessmentOutputRules.SignalDescriptionMaxLength);
                Assert.NotEmpty(signal.EvidenceSubjects);
                Assert.All(signal.EvidenceSubjects, subject => Assert.Contains(subject, scenario.CommunicationSubjects));
            });
        });
    }
}
