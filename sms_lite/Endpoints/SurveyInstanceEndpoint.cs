using sms_lite.Services;
using sms_lite.Server.Services;

public static class SurveyInstanceEndpoints
{
    public static void MapSurveyInstanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/survey-instances", async (
            ISurveyInstanceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var rows = await repository.GetSurveyInstancesAsync(cancellationToken);
            return Results.Ok(rows);
        });

        app.MapGet("/api/survey-instances/page", async (
            ISurveyInstanceRepository repository,
            int? rowLimit,
            int? rowOffset,
            int? surveyId,
            int? sampleId,
            string? surveySearch,
            DateTime? surveyDate,
            DateTime? startDate,
            DateTime? stopDate,
            bool? mail,
            bool? cawi,
            bool? cati,
            bool? capi,
            bool? hqReview,
            CancellationToken cancellationToken) =>
        {
            var page = await repository.GetSurveyInstancesPageAsync(
                new SurveyInstancePageRequest(
                    RowLimit: rowLimit ?? 20,
                    RowOffset: rowOffset ?? 0,
                    SurveyId: surveyId,
                    SampleId: sampleId,
                    SurveySearch: surveySearch,
                    SurveyDate: surveyDate,
                    StartDate: startDate,
                    StopDate: stopDate,
                    Mail: mail == true,
                    Cawi: cawi == true,
                    Cati: cati == true,
                    Capi: capi == true,
                    HqReview: hqReview == true),
                cancellationToken);

            return Results.Ok(page);
        });

        app.MapGet("/api/survey-instances/detail", async (
            ISurveyInstanceRepository repository,
            DateTime referenceDate,
            int surveyId,
            string sampleId,
            CancellationToken cancellationToken) =>
        {
            var detail = await repository.GetSurveyInstanceDetailAsync(referenceDate, surveyId, sampleId, cancellationToken);
            return detail is null
                ? Results.NotFound()
                : Results.Ok(detail);
        });
    }
}
