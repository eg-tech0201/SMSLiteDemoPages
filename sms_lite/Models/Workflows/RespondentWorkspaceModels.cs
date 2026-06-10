namespace sms_lite.Models.Workflows;

public sealed record RespondentKey(
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string StateId,
    string Poid
);

public sealed record BurdenMetricDto(
    string RangeLabel,
    int Surveys,
    int Percent,
    string Mode
);

public sealed record InteractionTimelineEventDto(
    DateTime EventDate,
    string EventType,
    string Actor,
    string Summary,
    string? Link
);

public sealed record RespondentDetailDto(
    RespondentKey Key,
    string DisplayName,
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyList<BurdenMetricDto> BurdenMetrics,
    IReadOnlyList<InteractionTimelineEventDto> Timeline
);
