using Anthropic;
using Anthropic.Models.Messages;

namespace ClientPulse.Api.Analysis.Claude;

// The only place Anthropic SDK types are used. One synchronous Messages call
// per analysis, bounded by the SDK's per-attempt timeout and retries plus an
// overall deadline. Never logs prompts, response text, or provider error text.
public sealed class ClaudeCommunicationAnalyzer : ICommunicationAnalyzer
{
    private readonly AnthropicClient _client;
    private readonly AnthropicAnalysisOptions _options;
    private readonly ILogger<ClaudeCommunicationAnalyzer> _logger;

    public ClaudeCommunicationAnalyzer(
        string apiKey,
        AnthropicAnalysisOptions options,
        ILogger<ClaudeCommunicationAnalyzer> logger)
    {
        _options = options;
        _logger = logger;
        _client = new AnthropicClient
        {
            ApiKey = apiKey,
            Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds),
            MaxRetries = options.MaxRetries,
        };
    }

    public async Task<AnalyzerOutcome> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (ClaudeRequest.CheckLimits(request.Communications, _options) is { } reason)
        {
            return new AnalysisUnsupported(reason);
        }

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxOutputTokens,
            System = ClaudeRequest.SystemPrompt,
            OutputConfig = new OutputConfig
            {
                Effort = ParseEffort(_options.Effort),
                Format = new JsonOutputFormat
                {
                    Schema = ClaudeRequest.BuildOutputSchema(request.Communications.Select(c => c.Id).ToList()),
                },
            },
            Messages = [new UserMessageParam { Content = ClaudeRequest.BuildUserContent(request.Communications) }],
        };

        return await ProviderCall.RunAsync(
            async token =>
            {
                var message = await _client.Messages.Create(parameters, token);

                _logger.LogInformation(
                    "Claude analysis for client {ClientId}: model {Model}, stop {StopReason}, input {InputTokens} tokens, output {OutputTokens} tokens",
                    request.ClientId,
                    message.Model.Raw(),
                    message.StopReason?.Raw(),
                    message.Usage.InputTokens,
                    message.Usage.OutputTokens);

                var texts = message.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text).ToList();
                return ClaudeResponseParser.Parse(
                    message.StopReason?.Raw(), texts, _options.Model, message.Model.Raw(), _options.Effort);
            },
            ClassifyProviderException,
            TimeSpan.FromSeconds(_options.OverallTimeoutSeconds),
            _logger,
            cancellationToken);
    }

    // Only the exception type is logged; provider messages are never surfaced.
    private AnalysisFailureKind? ClassifyProviderException(Exception exception)
    {
        var kind = ClaudeErrors.Classify(exception);
        if (kind is not null)
        {
            _logger.LogWarning(
                "Claude call failed ({Kind}): {ExceptionType}", kind, exception.GetType().Name);
        }

        return kind;
    }

    private static Effort ParseEffort(string effort) => effort switch
    {
        "low" => Effort.Low,
        "medium" => Effort.Medium,
        "high" => Effort.High,
        "xhigh" => Effort.Xhigh,
        "max" => Effort.Max,
        _ => throw new InvalidOperationException($"Unsupported effort '{effort}'."),
    };
}
