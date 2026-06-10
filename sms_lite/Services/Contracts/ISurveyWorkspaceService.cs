using sms_lite.Workflows;

namespace sms_lite.Services.Contracts;

public interface ISurveyWorkspaceService
{
    Task<IReadOnlyList<SurveyListItemDto>> GetSurveyListAsync(
        SurveyListQuery query,
        CancellationToken cancellationToken = default);

    Task<SurveyDetailDto?> GetSurveyDetailAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default);

    Task<SurveyRecordGridDto?> GetSurveyRecordGridAsync(
        SurveyRecordGridQuery query,
        CancellationToken cancellationToken = default);
}
