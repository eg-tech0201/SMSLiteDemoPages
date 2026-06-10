using sms_lite.Models.Integrations;

namespace sms_lite.Services.Contracts.Integration;

public interface IElmaGateway
{
    Task<ElmaFoUpdateLinkResponse> GetFoUpdateRequestLinkAsync(ElmaFoUpdateLinkRequest? request = null, CancellationToken cancellationToken = default);
    Task<ElmaSubmitTransactionResponse> SubmitTransactionAsync(ElmaSubmitTransactionRequest request, CancellationToken cancellationToken = default);
    Task<ElmaTransactionStatusResponse> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<ElmaClientCapabilityResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}
