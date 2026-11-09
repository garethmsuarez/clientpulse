using Anthropic.Exceptions;

namespace ClientPulse.Api.Analysis.Claude;

// Maps SDK exceptions (thrown after the SDK's own retries) to a failure
// category. Callers log only the exception type; provider messages and
// response bodies are never surfaced.
public static class ClaudeErrors
{
    public static AnalysisFailureKind? Classify(Exception exception) => exception switch
    {
        // Rate limits, overload, and upstream 5xx/network problems.
        AnthropicRateLimitException
            or AnthropicOverloadedException
            or AnthropicServiceUnavailableException
            or AnthropicGatewayTimeoutException
            or Anthropic5xxException
            or AnthropicIOException => AnalysisFailureKind.Unavailable,

        // Any other API rejection (401/403 credentials, 400/404/422 request):
        // a server configuration problem, not invalid model output.
        AnthropicApiException => AnalysisFailureKind.Configuration,

        _ => null,
    };
}
