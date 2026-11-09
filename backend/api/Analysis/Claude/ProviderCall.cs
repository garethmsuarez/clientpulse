namespace ClientPulse.Api.Analysis.Claude;

// Bounds one provider call and maps failures to categories. Separate from the
// SDK so cancellation and timeout handling can be unit tested.
public static class ProviderCall
{
    public static async Task<AnalyzerOutcome> RunAsync(
        Func<CancellationToken, Task<AnalyzerOutcome>> call,
        Func<Exception, AnalysisFailureKind?> classify,
        TimeSpan overallTimeout,
        ILogger logger,
        CancellationToken callerToken)
    {
        using var deadline = new CancellationTokenSource(overallTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken, deadline.Token);

        try
        {
            return await call(linked.Token);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            // The caller went away (for example the browser request was aborted).
            // Not a provider failure: let it propagate; nothing is saved.
            logger.LogDebug("Provider call cancelled by caller");
            throw;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            logger.LogWarning("Provider call exceeded the overall deadline of {Seconds}s", overallTimeout.TotalSeconds);
            return new AnalysisFailed(AnalysisFailureKind.Timeout);
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // The SDK's per-attempt timeout, after its retries were exhausted.
            logger.LogWarning("Provider call timed out after retries");
            return new AnalysisFailed(AnalysisFailureKind.Timeout);
        }
        catch (Exception ex) when (classify(ex) is { } kind)
        {
            return new AnalysisFailed(kind);
        }
    }
}
