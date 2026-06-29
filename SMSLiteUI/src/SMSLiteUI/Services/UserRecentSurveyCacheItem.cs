namespace SMSLiteUI.Services;

public sealed record UserRecentSurveyCacheItem(
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string? SurveyTitle,
    DateTime LastOpenedAtUtc);
