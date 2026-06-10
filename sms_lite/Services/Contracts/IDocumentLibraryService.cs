using sms_lite.Workflows;

namespace sms_lite.Services.Contracts;

public interface IDocumentLibraryService
{
    Task<IReadOnlyList<QuestionnaireArtifactDto>> GetQuestionnairesAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectionMaterialDto>> GetCollectionMaterialsAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default);

    Task<DocumentPayloadDto?> GetDocumentAsync(
        DocumentRequestDto request,
        CancellationToken cancellationToken = default);

    Task LogDocumentDownloadAsync(
        string documentId,
        string documentType,
        string fileName,
        string requestedBy,
        CancellationToken cancellationToken = default);
}
