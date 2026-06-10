using SMSLiteStaticDemo.Models.Workflows;

namespace SMSLiteStaticDemo.Services.Contracts;

public interface IRespondentWorkspaceService
{
    Task<RespondentDetailDto?> GetRespondentDetailAsync(
        RespondentKey key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InteractionTimelineEventDto>> GetInteractionTimelineAsync(
        RespondentKey key,
        CancellationToken cancellationToken = default);
}
