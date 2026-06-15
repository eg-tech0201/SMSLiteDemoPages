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
