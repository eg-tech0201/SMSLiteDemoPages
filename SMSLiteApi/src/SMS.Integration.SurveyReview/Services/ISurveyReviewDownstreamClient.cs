using SMSLiteModels.Entities.Integrations;

namespace SMS.Integration.SurveyReview.Services;

public interface ISurveyReviewDownstreamClient
{
    Task<IReadOnlyList<SurveyReviewSKeyInstance>> GetSKeyInstancesAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken);

    Task<SurveyReviewOutputPayload?> GetInstanceOutputAsync(
        string sKey,
        int publishedState,
        int outputType,
        CancellationToken cancellationToken);
}
