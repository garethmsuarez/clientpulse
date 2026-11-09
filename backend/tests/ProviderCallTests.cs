using ClientPulse.Api.Analysis;
using ClientPulse.Api.Analysis.Claude;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientPulse.Tests;

public class ProviderCallTests
{
    private static readonly AnalyzerOutcome Ok = new AnalysisUnsupported("placeholder outcome");

    private static Task<AnalyzerOutcome> Run(
        Func<CancellationToken, Task<AnalyzerOutcome>> call,
        CancellationToken caller = default,
        TimeSpan? overall = null,
        Func<Exception, AnalysisFailureKind?>? classify = null) =>
        ProviderCall.RunAsync(call, classify ?? (_ => null), overall ?? TimeSpan.FromSeconds(30),
            NullLogger.Instance, caller);

    [Fact]
    public async Task Success_IsReturnedUnchanged()
    {
        Assert.Same(Ok, await Run(_ => Task.FromResult(Ok)));
    }

    [Fact]
    public async Task CallerCancellation_PropagatesAndIsNotATimeout()
    {
        using var caller = new CancellationTokenSource();

        var task = Run(async token =>
        {
            await caller.CancelAsync();
            await Task.Delay(Timeout.Infinite, token);
            return Ok;
        }, caller.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task OverallDeadline_IsTimeout()
    {
        var outcome = await Run(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Ok;
        }, overall: TimeSpan.FromMilliseconds(50));

        Assert.Equal(AnalysisFailureKind.Timeout, Assert.IsType<AnalysisFailed>(outcome).Kind);
    }

    [Fact]
    public async Task SdkAttemptTimeoutAfterRetries_IsTimeout()
    {
        // The SDK throws TaskCanceledException wrapping TimeoutException.
        var outcome = await Run(_ => throw new TaskCanceledException("timed out", new TimeoutException()));

        Assert.Equal(AnalysisFailureKind.Timeout, Assert.IsType<AnalysisFailed>(outcome).Kind);
    }

    [Fact]
    public async Task ClassifiedProviderException_MapsToItsKind()
    {
        var outcome = await Run(
            _ => throw new HttpRequestException("synthetic"),
            classify: ex => ex is HttpRequestException ? AnalysisFailureKind.Unavailable : null);

        Assert.Equal(AnalysisFailureKind.Unavailable, Assert.IsType<AnalysisFailed>(outcome).Kind);
    }

    [Fact]
    public async Task UnclassifiedException_Propagates()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(_ => throw new InvalidOperationException()));
    }

    [Fact]
    public async Task CallIsMadeExactlyOnce_NoExtraRetryLayer()
    {
        var calls = 0;

        await Run(_ =>
        {
            calls++;
            throw new HttpRequestException("synthetic");
        }, classify: _ => AnalysisFailureKind.Unavailable);

        Assert.Equal(1, calls);
    }
}
