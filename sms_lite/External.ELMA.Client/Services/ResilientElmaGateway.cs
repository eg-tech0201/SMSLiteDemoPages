using Microsoft.Extensions.Options;
using sms_lite.Services.Contracts.Integration;
using sms_lite.Models.Integrations;
using External.ELMA.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace External.ELMA.Client.Services;

public sealed class ResilientElmaGateway : IElmaGateway
{
    private readonly IElmaDownstreamClient _downstreamClient;
    private readonly IElmaCircuitBreaker _circuitBreaker;
    private readonly IOptionsMonitor<ElmaClientOptions> _options;
    private readonly ILogger<ResilientElmaGateway> _logger;

    public ResilientElmaGateway(
        IElmaDownstreamClient downstreamClient,
        IElmaCircuitBreaker circuitBreaker,
        IOptionsMonitor<ElmaClientOptions> options,
        ILogger<ResilientElmaGateway> logger)
    {
        _downstreamClient = downstreamClient;
        _circuitBreaker = circuitBreaker;
        _options = options;
        _logger = logger;
    }

    public Task<ElmaClientCapabilityResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        return Task.FromResult(new ElmaClientCapabilityResponse(
            options.AuthenticationMode,
            TimeSpan.FromSeconds(options.DownstreamTimeoutSeconds),
            true,
            options.SupportsFormPrefill,
            "ELMA client shell supports static FO Update link now; direct form submission remains pending external ELMA specification."));
    }

    public Task<ElmaFoUpdateLinkResponse> GetFoUpdateRequestLinkAsync(ElmaFoUpdateLinkRequest? request = null, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _downstreamClient.GetFoUpdateRequestLinkAsync(request, ct), cancellationToken);

    public Task<ElmaTransactionStatusResponse> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _downstreamClient.GetTransactionStatusAsync(transactionId, ct), cancellationToken);

    public Task<ElmaSubmitTransactionResponse> SubmitTransactionAsync(ElmaSubmitTransactionRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _downstreamClient.SubmitTransactionAsync(request, ct), cancellationToken);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (_circuitBreaker.IsOpen())
            throw new ElmaGatewayException("ELMA_UNAVAILABLE", "ELMA is temporarily unavailable. Try again after the circuit breaker resets.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.CurrentValue.DownstreamTimeoutSeconds)));

        try
        {
            var result = await operation(timeoutCts.Token);
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogWarning("ELMA downstream request timed out after {TimeoutSeconds} seconds.", _options.CurrentValue.DownstreamTimeoutSeconds);
            throw new ElmaGatewayException("ELMA_TIMEOUT", "ELMA did not respond within the configured timeout window.");
        }
        catch (ElmaGatewayException)
        {
            _circuitBreaker.RecordFailure();
            throw;
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Unexpected ELMA downstream failure.");
            throw new ElmaGatewayException("ELMA_FAILURE", "ELMA could not process the request at this time.");
        }
    }
}
