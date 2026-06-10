using sms_lite.Models.Workflows;

namespace sms_lite.Services.Contracts;

public interface IRespondentWorkspaceService
{
    Task<RespondentDetailDto?> GetRespondentDetailAsync(
        RespondentKey key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InteractionTimelineEventDto>> GetInteractionTimelineAsync(
        RespondentKey key,
        CancellationToken cancellationToken = default);
}
