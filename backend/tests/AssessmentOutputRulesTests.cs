using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;

namespace ClientPulse.Tests;

public class AssessmentOutputRulesTests
{
    private static readonly IReadOnlySet<int> InputIds = new HashSet<int> { 10, 11, 12 };

    private static AnalysisResult Valid() => new(
        AnalysisSource.Fixture,
        "synthetic-scenario",
        "Synthetic summary.",
        AttentionLevel.Medium,
        [
            new AnalysisSignal(SignalKind.Concern, "Synthetic concern.", [10, 11]),
            new AnalysisSignal(SignalKind.Positive, "Synthetic positive.", [12]),
        ]);

    private static AnalysisResult WithSignal(AnalysisSignal signal) => Valid() with { Signals = [signal] };

    [Fact]
    public void ValidResult_HasNoErrors()
    {
        Assert.Empty(AssessmentOutputRules.Validate(Valid(), InputIds));
    }

    [Fact]
    public void EvidenceOutsideInput_IsRejected()
    {
        var errors = AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal(SignalKind.Concern, "Synthetic.", [10, 99])), InputIds);

        Assert.Contains(errors, e => e.Contains("99"));
    }

    [Fact]
    public void SignalWithoutEvidence_IsRejected()
    {
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal(SignalKind.Concern, "Synthetic.", [])), InputIds));
    }

    [Fact]
    public void DuplicateEvidence_IsRejected()
    {
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal(SignalKind.Concern, "Synthetic.", [10, 10])), InputIds));
    }

    [Fact]
    public void BlankOrOverlongText_IsRejected()
    {
        Assert.NotEmpty(AssessmentOutputRules.Validate(Valid() with { Summary = " " }, InputIds));
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            Valid() with { Summary = new string('s', AssessmentOutputRules.SummaryMaxLength + 1) }, InputIds));
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal(SignalKind.Concern, "", [10])), InputIds));
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal(
                SignalKind.Concern, new string('d', AssessmentOutputRules.SignalDescriptionMaxLength + 1), [10])),
            InputIds));
    }

    [Fact]
    public void UndefinedEnumValues_AreRejected()
    {
        Assert.NotEmpty(AssessmentOutputRules.Validate(Valid() with { AttentionLevel = (AttentionLevel)42 }, InputIds));
        Assert.NotEmpty(AssessmentOutputRules.Validate(Valid() with { Source = (AnalysisSource)42 }, InputIds));
        Assert.NotEmpty(AssessmentOutputRules.Validate(
            WithSignal(new AnalysisSignal((SignalKind)42, "Synthetic.", [10])), InputIds));
    }

    [Fact]
    public void FixtureResultWithoutName_IsRejected()
    {
        Assert.NotEmpty(AssessmentOutputRules.Validate(Valid() with { FixtureName = null }, InputIds));
    }
}
