using sms_lite.Services;

namespace sms_lite.Server.Services;

public interface ISurveyInstanceRepository
{
    Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(CancellationToken cancellationToken);
    Task<SurveyInstancePageResponse> GetSurveyInstancesPageAsync(SurveyInstancePageRequest request, CancellationToken cancellationToken);
    Task<SurveyInstanceDetailResponse?> GetSurveyInstanceDetailAsync(DateTime referenceDate, int surveyId, string sampleId, CancellationToken cancellationToken);
}
