using SMSLiteStaticDemo.Services;

public static class SurveyInstanceEndpoints
{
    public static void MapSurveyInstanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/survey-instances/detail",
        (HttpContext ctx, SurveyInstanceService svc,
         DateTime referenceDate, int surveyId, string sampleId) =>
        {
            var detail = svc.GetDetail(referenceDate, surveyId, sampleId);
            return detail is null
                ? Results.NotFound()
                : Results.Ok(detail);
        });
    }
}