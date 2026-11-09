using ClientPulse.Api.Analysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClientPulse.Api.Assessments;

// Problem responses carry a stable "code" so the UI can tell failures apart.
public static class AssessmentProblems
{
    public const string NoCommunicationsCode = "no-communications";
    public const string UnsupportedInputCode = "unsupported-input";
    public const string InvalidAnalyzerOutputCode = "invalid-analyzer-output";
    public const string NotLatestAssessmentCode = "not-latest-assessment";
    public const string ProviderInvalidOutputCode = "provider_invalid_output";
    public const string ProviderOutputTruncatedCode = "provider_output_truncated";
    public const string ProviderRefusedCode = "provider_refused";
    public const string ProviderUnavailableCode = "provider_unavailable";
    public const string ProviderTimeoutCode = "provider_timeout";
    public const string ProviderConfigurationErrorCode = "provider_configuration_error";

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

    // Fixed messages only: raw provider errors and communication text are never included.
    public static ProblemHttpResult ProviderFailure(AnalysisFailureKind kind) => kind switch
    {
        AnalysisFailureKind.OutputTruncated => Problem(
            StatusCodes.Status502BadGateway,
            "Analysis failed",
            "The AI provider's response was cut off before it was complete. No assessment was saved.",
            ProviderOutputTruncatedCode),
        AnalysisFailureKind.Refused => Problem(
            StatusCodes.Status502BadGateway,
            "Analysis failed",
            "The AI provider declined to analyze these communications. No assessment was saved.",
            ProviderRefusedCode),
        AnalysisFailureKind.Unavailable => Problem(
            StatusCodes.Status503ServiceUnavailable,
            "AI provider unavailable",
            "The AI provider is busy or unavailable. Try again later. No assessment was saved.",
            ProviderUnavailableCode),
        AnalysisFailureKind.Configuration => Problem(
            StatusCodes.Status502BadGateway,
            "Analysis failed",
            "The AI provider rejected the server's credentials or request. No assessment was saved.",
            ProviderConfigurationErrorCode),
        AnalysisFailureKind.Timeout => Problem(
            StatusCodes.Status504GatewayTimeout,
            "AI provider timed out",
            "The AI provider did not respond in time. No assessment was saved.",
            ProviderTimeoutCode),
        _ => Problem(
            StatusCodes.Status502BadGateway,
            "Analysis failed",
            "The AI provider returned an invalid result. No assessment was saved.",
            ProviderInvalidOutputCode),
    };

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
