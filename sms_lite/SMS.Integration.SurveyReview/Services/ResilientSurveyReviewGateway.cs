using SMS.Integration.SurveyReview.Configuration;
using sms_lite.Workflows;
using Microsoft.Extensions.Options;
using sms_lite.Models.Integrations;
using sms_lite.Services.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace SMS.Integration.SurveyReview.Services;

public sealed class ResilientSurveyReviewGateway : ISurveyReviewGateway
{
    private readonly ISurveyReviewDownstreamClient _downstreamClient;
    private readonly ISurveyReviewCircuitBreaker _circuitBreaker;
    private readonly IOptionsMonitor<SurveyReviewClientOptions> _options;
    private readonly ILogger<ResilientSurveyReviewGateway> _logger;

    public ResilientSurveyReviewGateway(
        ISurveyReviewDownstreamClient downstreamClient,
        ISurveyReviewCircuitBreaker circuitBreaker,
        IOptionsMonitor<SurveyReviewClientOptions> options,
        ILogger<ResilientSurveyReviewGateway> logger)
    {
        _downstreamClient = downstreamClient;
        _circuitBreaker = circuitBreaker;
        _options = options;
        _logger = logger;
    }

    public Task<IReadOnlyList<SurveyReviewSKeyInstance>> GetSKeyInstancesAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(ct => _downstreamClient.GetSKeyInstancesAsync(surveyId, surveyDate, ct), cancellationToken);

    public async Task<IReadOnlyList<SurveyReviewQuestionnaireVersion>> GetQuestionnaireVersionsAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken = default)
    {
        var instances = await GetSKeyInstancesAsync(surveyId, surveyDate, cancellationToken);
        return instances
            .Select(instance => new SurveyReviewQuestionnaireVersion(
                instance.SKey,
                instance.SurveyId,
                instance.SurveyDate,
                instance.PublishedState,
                instance.PageCount,
                ResolveStatus(instance.PublishedState),
                BuildOutputLinks(instance.SKey, instance.PublishedState)))
            .ToList();
    }

    public Task<SurveyReviewOutputPayload?> GetInstanceOutputAsync(
        string sKey,
        int publishedState,
        int outputType,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(ct => _downstreamClient.GetInstanceOutputAsync(sKey, publishedState, outputType, ct), cancellationToken);

    public async Task<IReadOnlyList<QuestionnaireArtifactDto>> GetQuestionnairesAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default)
    {
        var versions = await GetQuestionnaireVersionsAsync(key.SurveyId, key.ReferenceDate, cancellationToken);
        return versions
            .SelectMany(version => version.Outputs.Select(output => new QuestionnaireArtifactDto(
                $"{version.SurveyId}-{version.SKey}-{output.OutputType}",
                version.SKey,
                "CAPI",
                version.SurveyDate.ToString("yyyy-MM-dd"),
                output.Format,
                version.Status,
                output.Url,
                $"/api/survey-review/skey-instances/{version.SurveyId}/{version.SurveyDate:yyyy-MM-dd}",
                string.Empty)))
            .ToList();
    }

    public Task<IReadOnlyList<CollectionMaterialDto>> GetCollectionMaterialsAsync(
        SurveyInstanceKey key,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CollectionMaterialDto>>(Array.Empty<CollectionMaterialDto>());

    public async Task<DocumentPayloadDto?> GetDocumentAsync(
        DocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDocumentId(request.DocumentId, out var sKey, out var publishedState, out var outputType))
            return null;

        var output = await GetInstanceOutputAsync(sKey, publishedState, outputType, cancellationToken);
        if (output is null)
            return null;

        return new DocumentPayloadDto(
            request.DocumentId,
            output.OutputName,
            request.DocumentType,
            output.OutputName,
            output.FileName,
            output.ContentType,
            output.Content,
            output.ExternalUrl,
            output.PreviewText);
    }

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (_circuitBreaker.IsOpen())
            throw new SurveyReviewGatewayException("SURVEY_REVIEW_UNAVAILABLE", "Survey Review is temporarily unavailable. Questionnaire materials may be viewed later.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.CurrentValue.DownstreamTimeoutSeconds)));

        try
        {
            var result = await operation(timeoutCts.Token);
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogWarning("Survey Review request timed out after {TimeoutSeconds} seconds.", _options.CurrentValue.DownstreamTimeoutSeconds);
            throw new SurveyReviewGatewayException("SURVEY_REVIEW_TIMEOUT", "Survey Review did not respond within the configured timeout window.");
        }
        catch (SurveyReviewGatewayException)
        {
            _circuitBreaker.RecordFailure();
            throw;
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Unexpected Survey Review downstream failure.");
            throw new SurveyReviewGatewayException("SURVEY_REVIEW_FAILURE", "Survey Review could not process the request at this time.");
        }
    }

    private static IReadOnlyList<SurveyReviewOutputLink> BuildOutputLinks(string sKey, int publishedState)
        => Enumerable.Range(1, 4)
            .Select(outputType => new SurveyReviewOutputLink(
                outputType,
                SurveyReviewOutputTypes.GetName(outputType),
                SurveyReviewOutputTypes.GetFormat(outputType),
                $"/api/survey-review/instances/{Uri.EscapeDataString(sKey)}/{publishedState}/outputs/{outputType}"))
            .ToList();

    private static string ResolveStatus(int publishedState)
        => publishedState == 400 ? "Published" : $"State {publishedState}";

    private static bool TryParseDocumentId(string documentId, out string sKey, out int publishedState, out int outputType)
    {
        sKey = string.Empty;
        publishedState = 0;
        outputType = 0;

        var parts = documentId.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], "survey-review", StringComparison.OrdinalIgnoreCase))
            return false;

        sKey = parts[1];
        return int.TryParse(parts[2], out publishedState) && int.TryParse(parts[3], out outputType);
    }
}
