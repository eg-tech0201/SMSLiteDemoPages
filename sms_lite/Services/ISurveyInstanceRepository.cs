using SMSLiteStaticDemo.Services;

namespace SMSLiteStaticDemo.Server.Services;

public interface ISurveyInstanceRepository
{
    Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(CancellationToken cancellationToken);
}
