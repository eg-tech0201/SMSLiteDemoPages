using SMS.Integration.SurveyReview.Configuration;
using SMS.Integration.SurveyReview.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SMSLiteStaticDemo.Models.Integrations;
using SMSLiteStaticDemo.Services.Contracts.Integration;

namespace SMSLiteStaticDemo.Server.Endpoints;

public static class SurveyReviewEndpoints
{
    public static IEndpointRouteBuilder MapSurveyReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/survey-review")
            .WithTags("Survey Review");

        group.MapGet("/capabilities", (
            IOptionsMonitor<SurveyReviewClientOptions> options) =>
        {
            var current = options.CurrentValue;
            return Results.Ok(new SurveyReviewCapabilityResponse(
                new Uri(current.BaseUrl),
                TimeSpan.FromSeconds(current.DownstreamTimeoutSeconds),
                true,
                "Queries Survey Review SKey instances and instance output artifacts with graceful degradation on downstream failure."));
        })
        .WithName("GetSurveyReviewCapabilities")
        .WithSummary("Get Survey Review integration capabilities")
        .WithDescription("Returns configured Survey Review base URL, 5-second timeout window, and circuit breaker support.");

        group.MapGet("/skey-instances/{surveyId:int}/{surveyDate}", GetSKeyInstancesAsync)
            .WithName("GetSurveyReviewSKeyInstances")
            .WithSummary("Get SKey instances for a survey")
            .WithDescription("Queries Survey Review getskeyInstance for a survey id and survey date. The returned rows represent survey records differentiated by SKey.");

        group.MapGet("/questionnaire-versions/{surveyId:int}/{surveyDate}", GetQuestionnaireVersionsAsync)
            .WithName("GetSurveyReviewQuestionnaireVersions")
            .WithSummary("Get questionnaire versions and output links")
            .WithDescription("Builds SCT-facing questionnaire version rows from Survey Review SKey instances and exposes RTF, PDF, JSON, and PDF booklet links.");

        group.MapGet("/instances/{skey}/{publishedState:int}/outputs/{outputType:int}", GetInstanceOutputAsync)
            .WithName("GetSurveyReviewInstanceOutput")
            .WithSummary("Get questionnaire output for one SKey")
            .WithDescription("Retrieves an instance_output artifact by SKey, published state, and output type. Output types: RTF=1, PDF=2, JSON=3, PDF Booklet=4.");

        return endpoints;
    }

    private static async Task<IResult> GetSKeyInstancesAsync(
        int surveyId,
        string surveyDate,
        ISurveyReviewGateway gateway,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!DateTime.TryParse(surveyDate, out var parsedDate))
            return Results.BadRequest("surveyDate must use yyyy-MM-dd.");

        try
        {
            return Results.Ok(await gateway.GetSKeyInstancesAsync(surveyId, parsedDate, cancellationToken));
        }
        catch (SurveyReviewGatewayException ex)
        {
            return SurveyReviewProblem(ex, loggerFactory, "Survey Review SKey query failed.");
        }
    }

    private static async Task<IResult> GetQuestionnaireVersionsAsync(
        int surveyId,
        string surveyDate,
        ISurveyReviewGateway gateway,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!DateTime.TryParse(surveyDate, out var parsedDate))
            return Results.BadRequest("surveyDate must use yyyy-MM-dd.");

        try
        {
            return Results.Ok(await gateway.GetQuestionnaireVersionsAsync(surveyId, parsedDate, cancellationToken));
        }
        catch (SurveyReviewGatewayException ex)
        {
            return SurveyReviewProblem(ex, loggerFactory, "Survey Review questionnaire version query failed.");
        }
    }

    private static async Task<IResult> GetInstanceOutputAsync(
        string skey,
        int publishedState,
        int outputType,
        [FromQuery] bool download,
        ISurveyReviewGateway gateway,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (outputType is < 1 or > 4)
            return Results.BadRequest("outputType must be 1 (RTF), 2 (PDF), 3 (JSON), or 4 (PDF Booklet).");

        try
        {
            var output = await gateway.GetInstanceOutputAsync(skey, publishedState, outputType, cancellationToken);
            if (output?.Content is null)
                return Results.NotFound();

            return Results.File(
                output.Content,
                output.ContentType,
                fileDownloadName: download ? output.FileName : null,
                enableRangeProcessing: true);
        }
        catch (SurveyReviewGatewayException ex)
        {
            return SurveyReviewProblem(ex, loggerFactory, "Survey Review output retrieval failed.");
        }
    }

    private static IResult SurveyReviewProblem(
        SurveyReviewGatewayException ex,
        ILoggerFactory loggerFactory,
        string logMessage)
    {
        loggerFactory.CreateLogger("SurveyReview").LogWarning(ex, "{Message}", logMessage);
        return Results.Problem(
            title: "Survey Review unavailable",
            detail: "Questionnaire materials are temporarily unavailable from Survey Review. Try again later.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
    }
}
