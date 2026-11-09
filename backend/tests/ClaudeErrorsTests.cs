using System.Net;
using Anthropic.Exceptions;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;

namespace ClientPulse.Tests;

public class ClaudeErrorsTests
{
    public static TheoryData<Exception, AnalysisFailureKind> Classified() => new()
    {
        { new AnthropicRateLimitException { StatusCode = (HttpStatusCode)429, ResponseBody = "{}" }, AnalysisFailureKind.Unavailable },
        { new AnthropicOverloadedException { StatusCode = (HttpStatusCode)529, ResponseBody = "{}" }, AnalysisFailureKind.Unavailable },
        { new Anthropic5xxException { StatusCode = HttpStatusCode.InternalServerError, ResponseBody = "{}" }, AnalysisFailureKind.Unavailable },
        { new AnthropicUnauthorizedException { StatusCode = HttpStatusCode.Unauthorized, ResponseBody = "{}" }, AnalysisFailureKind.Configuration },
        { new AnthropicForbiddenException { StatusCode = HttpStatusCode.Forbidden, ResponseBody = "{}" }, AnalysisFailureKind.Configuration },
        { new AnthropicBadRequestException { StatusCode = HttpStatusCode.BadRequest, ResponseBody = "{}" }, AnalysisFailureKind.Configuration },
    };

    [Theory]
    [MemberData(nameof(Classified))]
    public void ProviderExceptions_MapToFailureKinds(Exception exception, AnalysisFailureKind expected)
    {
        Assert.Equal(expected, ClaudeErrors.Classify(exception));
    }

    [Fact]
    public void UnrelatedExceptions_AreNotClassified()
    {
        Assert.Null(ClaudeErrors.Classify(new InvalidOperationException()));
    }
}
