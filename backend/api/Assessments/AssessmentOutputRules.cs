using ClientPulse.Api.Analysis;

namespace ClientPulse.Api.Assessments;

// Validates analyzer output before anything is saved. Messages describe the
// problem by field and id only; they never include communication text.
public static class AssessmentOutputRules
{
    public const int SummaryMaxLength = 2_000;
    public const int SignalDescriptionMaxLength = 500;
    public const int FixtureNameMaxLength = 100;
    public const int ModelNameMaxLength = 100;

    public static IReadOnlyList<string> Validate(AnalysisResult result, IReadOnlySet<int> inputCommunicationIds)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(result.Source))
        {
            errors.Add("Source is not a known analysis source.");
        }

        if (result.Source == AnalysisSource.Fixture &&
            (string.IsNullOrWhiteSpace(result.FixtureName) || result.FixtureName.Length > FixtureNameMaxLength))
        {
            errors.Add("Fixture results must name their fixture.");
        }

        if (result.Source == AnalysisSource.Anthropic &&
            (string.IsNullOrWhiteSpace(result.RequestedModel) || result.RequestedModel.Length > ModelNameMaxLength ||
             result.ResponseModel?.Length > ModelNameMaxLength || string.IsNullOrWhiteSpace(result.Effort)))
        {
            errors.Add("Provider results must record the requested model and effort.");
        }

        if (string.IsNullOrWhiteSpace(result.Summary))
        {
            errors.Add("Summary is required.");
        }
        else if (result.Summary.Length > SummaryMaxLength)
        {
            errors.Add($"Summary exceeds {SummaryMaxLength} characters.");
        }

        if (!Enum.IsDefined(result.AttentionLevel))
        {
            errors.Add("Attention level is not a known value.");
        }

        for (var i = 0; i < result.Signals.Count; i++)
        {
            var signal = result.Signals[i];

            if (!Enum.IsDefined(signal.Kind))
            {
                errors.Add($"Signal {i}: kind is not a known value.");
            }

            if (string.IsNullOrWhiteSpace(signal.Description))
            {
                errors.Add($"Signal {i}: description is required.");
            }
            else if (signal.Description.Length > SignalDescriptionMaxLength)
            {
                errors.Add($"Signal {i}: description exceeds {SignalDescriptionMaxLength} characters.");
            }

            if (signal.EvidenceCommunicationIds.Count == 0)
            {
                errors.Add($"Signal {i}: at least one evidence reference is required.");
            }

            if (signal.EvidenceCommunicationIds.Distinct().Count() != signal.EvidenceCommunicationIds.Count)
            {
                errors.Add($"Signal {i}: evidence references must be unique.");
            }

            foreach (var id in signal.EvidenceCommunicationIds.Where(id => !inputCommunicationIds.Contains(id)))
            {
                errors.Add($"Signal {i}: evidence communication {id} was not part of the analyzed input.");
            }
        }

        return errors;
    }
}
