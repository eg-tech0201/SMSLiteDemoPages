using SMSLiteModels.Entities.Integrations;

namespace External.ELMA.Client.Services;

public interface IElmaDownstreamClient
{
    Task<ElmaSubmitTransactionResponse> SubmitTransactionAsync(ElmaSubmitTransactionRequest request, CancellationToken cancellationToken);
    Task<ElmaTransactionStatusResponse> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken);
    Task<ElmaFoUpdateLinkResponse> GetFoUpdateRequestLinkAsync(ElmaFoUpdateLinkRequest? request, CancellationToken cancellationToken);
}
