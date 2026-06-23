using SMS.Integration.SurveyReview.Configuration;
using Microsoft.Extensions.Options;
using SMSLiteModels.Entities.Integrations;
using SMS.Integration.SurveyReview.Services.Contracts.Integration;
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

}
