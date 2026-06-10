using sms_lite.Services;

namespace sms_lite.Server.Services;

public interface ISurveyInstanceRepository
{
    Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(CancellationToken cancellationToken);
}
