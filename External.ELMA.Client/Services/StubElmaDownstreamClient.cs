using External.ELMA.Client.Configuration;
using Microsoft.Extensions.Options;
using SMSLiteStaticDemo.Models.Integrations;

namespace External.ELMA.Client.Services;

public sealed class StubElmaDownstreamClient : IElmaDownstreamClient
{
    private readonly IOptionsMonitor<ElmaClientOptions> _options;
    private readonly Dictionary<string, ElmaTransactionStatusResponse> _transactions = new(StringComparer.OrdinalIgnoreCase);

    public StubElmaDownstreamClient(IOptionsMonitor<ElmaClientOptions> options)
    {
        _options = options;
    }

    public Task<ElmaSubmitTransactionResponse> SubmitTransactionAsync(ElmaSubmitTransactionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var transactionId = $"ELMA-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var submittedAt = DateTime.UtcNow;
        var response = new ElmaSubmitTransactionResponse(
            transactionId,
            ElmaTransactionStatus.Submitted,
            "Contact update request accepted by the ELMA client shell.",
            submittedAt,
            request.CorrelationId);

        _transactions[transactionId] = new ElmaTransactionStatusResponse(
            transactionId,
            ElmaTransactionStatus.Submitted,
            "The transaction has been queued for downstream ELMA delivery.",
            submittedAt,
            request.CorrelationId,
            null);

        return Task.FromResult(response);
    }

    public Task<ElmaTransactionStatusResponse> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_transactions.TryGetValue(transactionId, out var status))
            return Task.FromResult(status);

        return Task.FromResult(new ElmaTransactionStatusResponse(
            transactionId,
            ElmaTransactionStatus.Pending,
            "Transaction not found in the demo ELMA store.",
            DateTime.UtcNow,
            null,
            null));
    }

    public Task<ElmaFoUpdateLinkResponse> GetFoUpdateRequestLinkAsync(ElmaFoUpdateLinkRequest? request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        var uriBuilder = new UriBuilder(new Uri(new Uri(options.BaseUrl), options.FoUpdateRequestPath));
        var queryParts = string.IsNullOrWhiteSpace(uriBuilder.Query)
            ? new List<string>()
            : [uriBuilder.Query.TrimStart('?')];

        AddQueryValue(queryParts, options.PoidQueryParameterName, request?.Poid);
        AddQueryValue(queryParts, options.StateIdQueryParameterName, request?.StateId);
        AddQueryValue(queryParts, options.FrameIdQueryParameterName, request?.FrameId);
        AddQueryValue(queryParts, options.SourceSystemQueryParameterName, request?.SourceSystem ?? options.SourceSystem);
        AddQueryValue(queryParts, options.CorrelationIdQueryParameterName, request?.CorrelationId);
        uriBuilder.Query = string.Join("&", queryParts);

        return Task.FromResult(new ElmaFoUpdateLinkResponse(
            uriBuilder.Uri,
            HasPrefillKeys(request) ? "static-prefill-link" : "static-link",
            HasPrefillKeys(request)
                ? "ELMA FO Update Request link includes respondent identifiers for form prefill."
                : "Current approved behavior is the static ELMA FO Update Request link."));
    }

    private static void AddQueryValue(List<string> queryParts, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            queryParts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value.Trim())}");
    }

    private static bool HasPrefillKeys(ElmaFoUpdateLinkRequest? request) =>
        !string.IsNullOrWhiteSpace(request?.Poid) ||
        !string.IsNullOrWhiteSpace(request?.StateId) ||
        !string.IsNullOrWhiteSpace(request?.FrameId);
}
