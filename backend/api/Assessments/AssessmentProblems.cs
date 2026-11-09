using Microsoft.AspNetCore.Http.HttpResults;

namespace ClientPulse.Api.Assessments;

// Problem responses carry a stable "code" so the UI can tell failures apart.
public static class AssessmentProblems
{
    public const string NoCommunicationsCode = "no-communications";
    public const string UnsupportedInputCode = "unsupported-input";
    public const string InvalidAnalyzerOutputCode = "invalid-analyzer-output";
    public const string NotLatestAssessmentCode = "not-latest-assessment";

    public static ProblemHttpResult NoCommunications() => Problem(
        StatusCodes.Status409Conflict,
        "No communications to analyze",
        "Add at least one communication before analyzing.",
        NoCommunicationsCode);

    public static ProblemHttpResult UnsupportedInput(string reason) => Problem(
        StatusCodes.Status422UnprocessableEntity,
        "Unsupported analysis input",
        reason,
        UnsupportedInputCode);

    // In-process analyzer produced invalid output. 502 is reserved for invalid
    // responses from a future external AI provider.
    public static ProblemHttpResult InvalidAnalyzerOutput() => Problem(
        StatusCodes.Status500InternalServerError,
        "Analysis failed",
        "The analyzer returned an invalid result. No assessment was saved.",
        InvalidAnalyzerOutputCode);

    public static ProblemHttpResult AssessmentNotFound(int assessmentId) => Problem(
        StatusCodes.Status404NotFound,
        "Assessment not found",
        $"No assessment with id {assessmentId} for this client.",
        "assessment-not-found");

    public static ProblemHttpResult NotLatestAssessment() => Problem(
        StatusCodes.Status409Conflict,
        "Assessment is not the latest",
        "A newer assessment exists for this client. Only the latest assessment can be reviewed.",
        NotLatestAssessmentCode);

    private static ProblemHttpResult Problem(int status, string title, string detail, string code) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
