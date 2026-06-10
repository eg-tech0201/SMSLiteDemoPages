using SMSLiteStaticDemo.Workflows;
using SMSLiteStaticDemo.Models.Integrations;

namespace SMSLiteStaticDemo.Services.Contracts.Integration;

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

    Task<IReadOnlyList<QuestionnaireArtifactDto>> GetQuestionnairesAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectionMaterialDto>> GetCollectionMaterialsAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default);

    Task<DocumentPayloadDto?> GetDocumentAsync(
        DocumentRequestDto request,
        CancellationToken cancellationToken = default);
}
