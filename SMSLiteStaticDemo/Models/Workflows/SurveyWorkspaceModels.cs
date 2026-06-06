namespace SMSLiteStaticDemo.Workflows;

public sealed record SurveyInstanceKey(
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate
);

public sealed record SurveyListQuery(
    string? SearchText = null,
    string? Status = null,
    string? SurveyType = null,
    string? GeographicArea = null,
    string? CollectionMode = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    DateTime? ReferenceDate = null,
    int? PageNumber = null,
    int? PageSize = null
);

public sealed record SurveyListItemDto(
    SurveyInstanceKey Key,
    string Title,
    string Subtitle,
    string Status,
    string SurveyType,
    string StateAlpha,
    string ProjectCode,
    string OmbNumber,
    DateTime SurveyDate,
    DateTime SurveyStartDate,
    DateTime SurveyStopDate
);

public sealed record ModeWindowDto(
    string Mode,
    DateTime StartDate,
    DateTime StopDate
);

public sealed record CountSliceDto(
    string Code,
    string Definition,
    int Count
);

public sealed record QuestionnaireArtifactDto(
    string ArtifactId,
    string SKey,
    string CollectionMode,
    string Version,
    string Format,
    string Status,
    string SourceUrl,
    string SpecificationsUrl,
    string MetadataUrl
);

public sealed record CollectionMaterialDto(
    string MaterialId,
    string MaterialName,
    string MaterialType,
    string CollectionMode,
    string Version,
    DateTime UploadDate,
    long FileSizeBytes,
    string FileName
);

public sealed record SurveyDetailDto(
    SurveyInstanceKey Key,
    string Title,
    string Subtitle,
    string SampleName,
    string SurveyFrequency,
    string Version,
    string HqSurveyAdmin,
    string ProjectCode,
    int TotalReceived,
    int TotalDeleted,
    decimal BudgetAllocation,
    IReadOnlyList<ModeWindowDto> Modes,
    IReadOnlyList<CountSliceDto> OpDomCounts,
    IReadOnlyList<CountSliceDto> DcmsCounts,
    IReadOnlyList<QuestionnaireArtifactDto> Questionnaires,
    IReadOnlyList<CollectionMaterialDto> CollectionMaterials,
    IReadOnlyDictionary<string, string> FullRecord
);

public sealed record SurveyRecordGridQuery(
    SurveyInstanceKey Key,
    string? SearchText = null,
    IReadOnlyDictionary<string, string>? ColumnFilters = null,
    string? SortField = null,
    bool SortAscending = true,
    int? PageNumber = null,
    int? PageSize = null
);

public sealed record SurveyRecordGridRowDto(
    int RowId,
    string SKey,
    string StateAlpha,
    string StateId,
    string Poid,
    string TargetPoid,
    IReadOnlyDictionary<string, string> Fields
);

public sealed record SurveyRecordGridDto(
    SurveyInstanceKey Key,
    IReadOnlyList<string> Columns,
    IReadOnlyList<SurveyRecordGridRowDto> Rows
);

public sealed record DocumentRequestDto(
    string DocumentId,
    string DocumentType,
    string Format,
    string? SourceUrl = null
);

public sealed record DocumentPayloadDto(
    string DocumentId,
    string Title,
    string DocumentType,
    string Format,
    string FileName,
    string? ContentType,
    byte[]? Content,
    string? ExternalUrl,
    string? PreviewText
);
