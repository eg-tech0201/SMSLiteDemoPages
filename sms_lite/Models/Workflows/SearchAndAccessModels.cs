namespace sms_lite.Models.Workflows;

public sealed record GlobalSearchQueryDto(
    string? SearchText = null,
    string? RecordType = null,
    string? Status = null,
    string? SurveyType = null,
    string? GeographicArea = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? Limit = null
);

public sealed record GlobalSearchResultDto(
    string RecordType,
    string Title,
    string Subtitle,
    string TrackingNumber,
    string? Identifier,
    DateTime Date,
    string? Status,
    string? SurveyType,
    string? GeographicArea,
    int Relevance,
    string Link
);

public sealed record SavedSearchDto(
    string Name,
    GlobalSearchQueryDto Query
);

public sealed record ExportRequestDto(
    string ExportType,
    string RequestedBy,
    string? ScopeId = null,
    IReadOnlyDictionary<string, string>? Parameters = null
);

public sealed record ExportResultDto(
    string FileName,
    string ContentType,
    byte[] Content
);

public sealed record UserAccessProfileDto(
    string UserId,
    string DisplayName,
    string AuthenticationProvider,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, string> Claims
);

public sealed record AuditTrailEntryDto(
    DateTimeOffset Timestamp,
    string Action,
    string ArtifactType,
    string ArtifactId,
    string FileName,
    string PerformedBy,
    string Details
);
