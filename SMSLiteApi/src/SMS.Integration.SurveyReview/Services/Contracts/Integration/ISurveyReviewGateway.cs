using SMSLiteModels.Entities.Integrations;

namespace SMS.Integration.SurveyReview.Services.Contracts.Integration;

public interface ISurveyReviewGateway
{
    Task<IReadOnlyList<SurveyReviewSKeyInstance>> GetSKeyInstancesAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SurveyReviewQuestionnaireVersion>> GetQuestionnaireVersionsAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken = default);

    Task<SurveyReviewOutputPayload?> GetInstanceOutputAsync(
        string sKey,
        int publishedState,
        int outputType,
        CancellationToken cancellationToken = default);
}
