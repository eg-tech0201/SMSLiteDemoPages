namespace SMS.Integration.SurveyReview.Configuration;

public sealed class SurveyReviewClientOptions
{
    public const string SectionName = "SurveyReviewClient";

    public string BaseUrl { get; set; } = "https://surveyreview.nass.usda.gov/";
    public string TrainingBaseUrl { get; set; } = "https://capitrainingbeta.nass.usda.gov/";
    public string SKeyInstancePathTemplate { get; set; } = "services/getskeyInstance/{surveyId}/{surveyDate}";
    public string InstanceOutputPathTemplate { get; set; } = "services/getinstanceoutput/{skey}/{publishedState}/{outputType}";
    public int DownstreamTimeoutSeconds { get; set; } = 5;
    public int CircuitBreakerFailureThreshold { get; set; } = 3;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
}
