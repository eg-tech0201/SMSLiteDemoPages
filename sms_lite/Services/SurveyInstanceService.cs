namespace sms_lite.Services;

public sealed class SurveyInstanceService
{
    public IReadOnlyList<SurveyInstance> Instances => [];

    public SurveyInstance? FindInstance(DateTime referenceDate, int surveyId, string sampleId) => null;

    public FilterOptionsResponse GetFilterOptions(DateTime? referenceDate, int? surveyId, string? sampleId)
        => new([], [], []);

    public SurveyInstanceDetailResponse? GetDetail(DateTime referenceDate, int surveyId, string sampleId) => null;

    public List<SurveyInstance> FilterInstances(DateTime? referenceDate, int? surveyId, string? sampleId) => [];

    public IReadOnlyList<SurveyInstance> GetSurveyInstances(int surveyId) => [];

    public IReadOnlyList<SurveyRecordIndexItem> GetSurveyRecordIndex(int surveyId) => [];

    public IReadOnlyList<SurveyRespondentRecord> GetRespondentsForInstance(
        DateTime referenceDate,
        int surveyId,
        string sampleId,
        string? skey,
        int count = 24) => [];

    public IReadOnlyList<DateTime> GetReferenceDatesForSurveySample(int surveyId, string sampleId) => [];

    public IReadOnlyList<SurveyRespondentRecord> GetSurveyRespondentRecords(int surveyId) => [];

    public SurveyRecordLookupResult? FindSurveyRecordByPoid(int surveyId, string poid) => null;

    public SurveyRespondentRecord? GetRespondentByPoid(DateTime referenceDate, int surveyId, string sampleId, string poid) => null;

    public IReadOnlyList<RespondentTimelineEvent> GetRespondentTimeline(string poid, int surveyId, string sampleId) => [];
}

public sealed record SurveyInstance(
    int SurveyId,
    string SampleId,
    string SampleName,
    DateTime ReferenceDate,
    string Title,
    string SubTitle,
    string SurveyFrequency,
    string Version,
    DateTime SurveyDate,
    DateTime SurveyStartDate,
    DateTime SurveyStopDate,
    string HqSurveyAdmin,
    string ProjectCode,
    string Status,
    string OmbNumber,
    DateTime OmbExpires,
    string State,
    string Region,
    string StateId,
    string StateAlpha,
    string DcmsCodeId,
    string OpDomStatusId,
    string EnumeratorId,
    string EnumeratorName,
    string ManagerId,
    string ManagerName,
    string CoachId,
    string CoachName,
    string EnumeratorNotes,
    string ResponseCode,
    string Mode,
    List<ModeWindow> Modes,
    List<CountItem> OpDomCounts,
    List<CountItem> DcmsCounts,
    int TotalReceived,
    int TotalDeleted,
    decimal BudgetAllocation,
    int RespondentInstancesLast1Year,
    int RespondentInstancesLast3Years,
    int RespondentInstancesLast5Years,
    decimal ResponseHistoryRate,
    List<ResponseHistoryItem> ResponseHistoryBreakdown
);

public sealed record SurveyGridRow(
    int? SampleId,
    int? Fips,
    int? SurveyId,
    int? StateId,
    DateTime? SurveyDate,
    int? SKey,
    int? Poid,
    decimal? TargetPoid,
    int? Tract,
    int? Subtract,
    int? FrameId,
    int? OperationId,
    int? PersonId,
    int? ObjectiveYieldId,
    int? CommentFlag,
    string? Comments,
    string? OfficeNotes,
    int? PeriodId,
    int? Stratum,
    int? PartnerNo,
    int? Rep,
    int? KZero,
    int? X1,
    int? X2,
    int? X3,
    int? X4,
    int? X5,
    int? Flag1,
    int? Flag2,
    int? Flag3,
    int? Flag4,
    int? Flag5,
    short? HoldFlag,
    int? SZeroFlag,
    string? Label1,
    string? Label2,
    string? Label3,
    string? Label4,
    string? Label5,
    string? OtherField,
    short? CatiFlag,
    string? CatiGroup,
    short? CapiFlag,
    short? PhoneOnlyFlag,
    short? CawiFlag,
    short? MailFlag,
    short? PmcMailFlag,
    short? RfoMailFlag,
    DateTime? MailStartDate,
    DateTime? MailStopDate,
    DateTime? CawiStartDate,
    DateTime? CawiStopDate,
    DateTime? CapiStartDate,
    DateTime? CapiStopDate,
    DateTime? CatiStartDate,
    DateTime? CatiStopDate,
    string? CatiApp,
    int? ActiveStatusId,
    int? OpDomStatusId,
    int? ManagerFlag,
    string? ManagerFlagChar,
    decimal? StatePoid,
    string? OperationName,
    string? OperationAddress,
    string? OperationOtherAddress,
    string? OperationPlaceName,
    string? OperationStateAbbreviation,
    string? OperationZip5,
    string? OperationZip4,
    string? OperationPhone,
    string? OperationPhoneChar,
    int? OperationCellPhoneFlag,
    string? OperationCellPhoneFlagChar,
    string? OperationFax,
    string? OperationFaxChar,
    int? OperationFarmFlag,
    int? AgriculturalBusinessFlag,
    int? TaggedId,
    int? OperationCountyId,
    int? OperationDistrictId,
    string? OperationEmail,
    int? PersonStatusId,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? SuffixId,
    string? WholeName,
    string? Address,
    string? OtherAddress,
    string? PersonPlaceName,
    string? PersonStateAbbreviation,
    string? PersonZip5,
    string? PersonZip4,
    string? PersonPhone,
    string? PersonPhoneChar,
    int? PersonCellPhoneFlag,
    string? PersonCellPhoneFlagChar,
    string? PersonOtherPhone,
    string? PersonOtherPhoneChar,
    int? PersonOtherCellPhoneFlag,
    string? PersonOtherCellPhoneFlagChar,
    string? PersonFaxPhone,
    string? PersonFaxPhoneChar,
    int? CountyId,
    int? DistrictId,
    string? PersonEmail,
    int? LanguageId,
    decimal? Latitude,
    decimal? Longitude,
    int? DataCollectionModeCodeId,
    int? DcmsCodeId,
    int? PermSupervisorId,
    int? PermEnumeratorId,
    string? SurveyCode,
    string? Barcode,
    int? RfoReviewFlag,
    int? MarkedFlag,
    int? Mseqnum,
    string? CountyName,
    string? OperationCountyName,
    DateTime? RespDate,
    DateTime? DataPull,
    int? ResponseCode,
    int? RespondentCode,
    int? ReportingModeCode,
    int? DataCaptureCode,
    int? DataCollectionEnumeratorId,
    DateTime? DataCollectionDate,
    short? ReviewFlag,
    int? StartedFlag,
    int? SavedFlag,
    int? OutOfBusinessId,
    string? XStateLink,
    long? CoordinationId,
    string? SurveyTitle,
    string? SurveySubtitle,
    string? Version,
    string? StateName,
    string? StateAbbreviation,
    string? OperationComments,
    string? PersonComments,
    int? BackupDcmsCode,
    string? BackupEnumeratorId,
    string? SupervisorId,
    string? EnumeratorId,
    string? EnumeratorName,
    string? SupervisorName,
    string? BackupSupervisorId,
    int? SmsCheckIn,
    short? HistoricResponsePercentage1Year,
    short? HistoricResponseSurveys1Year,
    string? HistoricResponseMode1Year,
    short? HistoricResponsePercentage3Years,
    short? HistoricResponseSurveys3Years,
    string? HistoricResponseMode3Years,
    short? HistoricResponsePercentage5Years,
    short? HistoricResponseSurveys5Years,
    string? HistoricResponseMode5Years,
    int? StartDcDcmsCode,
    int? StartDcEnumerator,
    int? StartDcSupervisor,
    short? PartnershipFlag,
    int? AddedRecordFlag,
    string? EnumeratorNotes,
    int? CapiAppointmentFlag,
    int? CapiAttemptedContacts,
    int? Tier,
    int? ReactivatedFlag,
    DateTime? ReactivatedFlagDate,
    string? ManagerId,
    string? ManagerName,
    short? ComputedSkill,
    short? ManualSkill,
    short? CoordinationFlag,
    int? Ruid,
    short? ActiveFlag = null,
    DateTime? StartDate = null,
    DateTime? StopDate = null,
    int? SampleMonth = null,
    short? HqReviewFlag = null,
    int? PopulatedRows = null,
    int? FrequencyCode = null,
    string? FrequencyDescription = null,
    string? ProjectCodeValue = null,
    string? OmbNumberValue = null,
    DateTime? OmbExpires = null,
    string? BaseMonth = null,
    string? MarkedVersion = null,
    string? SampleNameValue = null,
    string? ElmoSurveyIdValue = null,
    string? ElmoPeriodId = null,
    string? ElmoMonth = null,
    int? TotalRowCount = null
)
{
    public string SampleName => DisplayValue.Text(SampleNameValue);
    public string ElmoSurveyId => DisplayValue.Text(ElmoSurveyIdValue);
    public string OmbNumber => DisplayValue.Text(OmbNumberValue);
}

public sealed record FilterOptionsResponse(
    List<string> AvailableReferenceDates,
    List<int> AvailableSurveyIds,
    List<string> AvailableSampleIds
);

public sealed record SurveyInstancePageRequest(
    int RowLimit = 20,
    int RowOffset = 0,
    int? SurveyId = null,
    int? SampleId = null,
    string? SurveySearch = null,
    DateTime? SurveyDate = null,
    DateTime? StartDate = null,
    DateTime? StopDate = null,
    bool Mail = false,
    bool Cawi = false,
    bool Cati = false,
    bool Capi = false,
    bool HqReview = false
);

public sealed record SurveyInstancePageResponse(
    IReadOnlyList<SurveyGridRow> Rows,
    int TotalRowCount
);

public sealed record SurveyInstanceDetailResponse(
    string SampleId,
    string SampleName,
    int SurveyId,
    string Title,
    string SubTitle,
    string SurveyFrequency,
    string Version,
    DateTime SurveyDate,
    DateTime ReferenceDate,
    DateTime SurveyStartDate,
    DateTime SurveyStopDate,
    string HqSurveyAdmin,
    string ProjectCode,
    string OmbDocket,
    string OmbExpiration,
    List<ModeWindow> Modes,
    List<CountItem> OpDomCounts,
    List<CountItem> DcmsCounts,
    List<CountItem> DataCollectionStatusCounts,
    List<CountItem> ReportsReceivedByModeCounts,
    int TotalSample,
    int TotalReceived,
    int TotalDeleted,
    decimal BudgetAllocation,
    int RespondentInstancesLast1Year,
    int RespondentInstancesLast3Years,
    int RespondentInstancesLast5Years,
    decimal ResponseHistoryRate,
    List<ResponseHistoryItem> ResponseHistoryBreakdown,
    List<SurveyDesignerAssociation> SurveyDesignerAssociations,
    List<CollectionMaterial> CollectionMaterials,
    List<SurveyDetailRecordRow> RecordRows,
    List<DetailField> FullRecord
);

public sealed record ModeWindow(string Mode, DateTime? StartDate, DateTime? StopDate);

public sealed record CountItem(string Code, string Definition, int Count);

public sealed record ResponseHistoryItem(string Label, int Count);

public sealed record DetailField(string Field, string Value);

public sealed record SurveyDetailRecordRow(
    string Fips,
    string State,
    string SKey,
    string Status,
    string Poid,
    string TargetPoid,
    string SampleId,
    DateTime ReferenceDate
);

public sealed record SurveyDesignerAssociation(
    string SKey,
    string QuestionnaireId,
    string CollectionMode,
    string QuestionnaireVersion,
    string QuestionnaireFormat,
    string QuestionnaireStatus,
    string QuestionnaireLink,
    string SpecificationsLink,
    string MetadataLink,
    List<QuestionnaireSpecItem> SpecificationItems
);

public sealed record QuestionnaireSpecItem(
    string ItemCode,
    string Description
);

public sealed record CollectionMaterial(
    string MaterialName,
    string MaterialType,
    string CollectionMode,
    string Version,
    DateTime UploadDate,
    long FileSizeBytes,
    string FileName
);

public sealed record SurveyRecordIndexItem(
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string Poid,
    string TargetPoid,
    string Mode,
    string StateAlpha,
    string StateId,
    string SKey
);

public sealed record SurveyRecordLookupResult(
    DateTime ReferenceDate,
    int SurveyId,
    string SampleId,
    string Poid,
    string TargetPoid
);

public sealed record SurveyRespondentRecord(
    int RespondentId,
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string StateId,
    string StateAlpha,
    string SKey,
    List<DetailField> Fields
);

public sealed record ResponseHistory(
    int Last1Year,
    int Last3Years,
    int Last5Years,
    decimal ResponseRate,
    List<ResponseHistoryItem> Breakdown
);

public sealed record RespondentTimelineEvent(
    DateTime EventDate,
    string EventType,
    string Actor,
    string Summary,
    string Link
);
