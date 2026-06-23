namespace SMSLiteModels.Entities.Integrations;

public sealed record SurveyReviewSKeyInstance(
    DateTime SurveyDate,
    int SurveyId,
    int PageCount,
    string SKey,
    int PublishedState
);

public sealed record SurveyReviewQuestionnaireVersion(
    string SKey,
    int SurveyId,
    DateTime SurveyDate,
    int PublishedState,
    int PageCount,
    string Status,
    IReadOnlyList<SurveyReviewOutputLink> Outputs
);

public sealed record SurveyReviewOutputLink(
    int OutputType,
    string OutputName,
    string Format,
    string Url
);

public sealed record SurveyReviewOutputPayload(
    string SKey,
    int PublishedState,
    int OutputType,
    string OutputName,
    string FileName,
    string ContentType,
    byte[]? Content,
    string? ExternalUrl,
    string? PreviewText
);

public sealed record SurveyReviewCapabilityResponse(
    Uri BaseUrl,
    TimeSpan Timeout,
    bool CircuitBreakerEnabled,
    string Notes
);
