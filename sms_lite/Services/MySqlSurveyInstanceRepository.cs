using Microsoft.Extensions.Options;
using MySqlConnector;
using sms_lite.Server.Configuration;
using sms_lite.Server.Logging;
using sms_lite.Services;
using System.Data;

namespace sms_lite.Server.Services;

public sealed class MySqlSurveyInstanceRepository(
    IOptions<SmsLiteDatabaseOptions> options,
    IUserFileLogger fileLogger,
    ILogger<MySqlSurveyInstanceRepository> logger) : ISurveyInstanceRepository
{
    private readonly SmsLiteDatabaseOptions _options = options.Value;
    private const string GetSurveyInstancesProcedure = SmsLiteStoredProcedures.GetSurveyInstances;
    private const string GetSurveyInstanceCheckinValuesProcedure = SmsLiteStoredProcedures.GetSurveyInstanceCheckinValues;
    private const string MissingValue = DisplayValue.Missing;
    private const int DefaultSurveyInstanceFrameSize = 1000;

    public async Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(CancellationToken cancellationToken)
        => await GetSurveyInstancesFrameAsync(
            SurveyId: null,
            SampleId: null,
            Search: null,
            SurveyDate: null,
            StartDate: null,
            StopDate: null,
            MailFlag: null,
            CawiFlag: null,
            CatiFlag: null,
            CapiFlag: null,
            HqReviewFlag: null,
            Limit: DefaultSurveyInstanceFrameSize,
            Offset: 0,
            cancellationToken);

    public async Task<SurveyInstancePageResponse> GetSurveyInstancesPageAsync(
        SurveyInstancePageRequest request,
        CancellationToken cancellationToken)
    {
        var rowLimit = Math.Clamp(request.RowLimit, 1, 100);
        var rowOffset = Math.Max(0, request.RowOffset);
        var rows = await GetSurveyInstancesFrameAsync(
            SurveyId: request.SurveyId,
            SampleId: request.SampleId,
            Search: request.SurveySearch,
            SurveyDate: request.SurveyDate,
            StartDate: request.StartDate,
            StopDate: request.StopDate,
            MailFlag: request.Mail ? (short)1 : null,
            CawiFlag: request.Cawi ? (short)1 : null,
            CatiFlag: request.Cati ? (short)1 : null,
            CapiFlag: request.Capi ? (short)1 : null,
            HqReviewFlag: request.HqReview ? (short)1 : null,
            Limit: rowLimit,
            Offset: rowOffset,
            cancellationToken);

        var totalRowCount = rows.FirstOrDefault()?.TotalRowCount ?? 0;
        return new SurveyInstancePageResponse(rows, totalRowCount);
    }

    private async Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesFrameAsync(
        int? SurveyId,
        int? SampleId,
        string? Search,
        DateTime? SurveyDate,
        DateTime? StartDate,
        DateTime? StopDate,
        short? MailFlag,
        short? CawiFlag,
        short? CatiFlag,
        short? CapiFlag,
        short? HqReviewFlag,
        int Limit,
        int Offset,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            const string message = "SmsLiteDatabase username/password are blank. Fill appsettings before running the MySQL stored procedure.";
            await fileLogger.LogAsync(message, cancellationToken: cancellationToken);
            logger.LogWarning(message);
            return [];
        }

        try
        {
            await fileLogger.LogAsync(
                $"Executing stored procedure '{GetSurveyInstancesProcedure}' against '{_options.Database}' on '{_options.Host}:{_options.Port}'.",
                cancellationToken: cancellationToken);

            await using var connection = new MySqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = GetSurveyInstancesProcedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 120;
            command.Parameters.Add(new MySqlParameter("row_limit", MySqlDbType.Int32) { Value = Limit });
            command.Parameters.Add(new MySqlParameter("row_offset", MySqlDbType.Int32) { Value = Offset });
            command.Parameters.Add(new MySqlParameter("filter_survey_id", MySqlDbType.Int32) { Value = SurveyId.HasValue ? (object)SurveyId.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_sample_id", MySqlDbType.Int32) { Value = SampleId.HasValue ? (object)SampleId.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_survey_search", MySqlDbType.VarChar) { Value = string.IsNullOrWhiteSpace(Search) ? DBNull.Value : (object)Search.Trim() });
            command.Parameters.Add(new MySqlParameter("filter_survey_date", MySqlDbType.DateTime) { Value = SurveyDate.HasValue ? (object)SurveyDate.Value.Date : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_start_date", MySqlDbType.DateTime) { Value = StartDate.HasValue ? (object)StartDate.Value.Date : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_stop_date", MySqlDbType.DateTime) { Value = StopDate.HasValue ? (object)StopDate.Value.Date : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_mail_flag", MySqlDbType.Int16) { Value = MailFlag.HasValue ? (object)MailFlag.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_cawi_flag", MySqlDbType.Int16) { Value = CawiFlag.HasValue ? (object)CawiFlag.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_cati_flag", MySqlDbType.Int16) { Value = CatiFlag.HasValue ? (object)CatiFlag.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_capi_flag", MySqlDbType.Int16) { Value = CapiFlag.HasValue ? (object)CapiFlag.Value : DBNull.Value });
            command.Parameters.Add(new MySqlParameter("filter_hq_review_flag", MySqlDbType.Int16) { Value = HqReviewFlag.HasValue ? (object)HqReviewFlag.Value : DBNull.Value });

            var rows = new List<SurveyGridRow>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new SurveyGridRow(
                    SampleId: GetNullableInt32(reader, "sample_id"),
                    Fips: GetNullableInt32(reader, "fips"),
                    SurveyId: GetNullableInt32(reader, "survey_id"),
                    StateId: GetNullableInt32(reader, "state_id"),
                    SurveyDate: GetNullableDateTime(reader, "survey_date"),
                    SKey: GetNullableInt32(reader, "skey"),
                    Poid: GetNullableInt32(reader, "poid"),
                    TargetPoid: GetNullableDecimal(reader, "target_poid"),
                    Tract: GetNullableInt32(reader, "tract"),
                    Subtract: GetNullableInt32(reader, "subtract"),
                    FrameId: GetNullableInt32(reader, "frame_id"),
                    OperationId: GetNullableInt32(reader, "operid"),
                    PersonId: GetNullableInt32(reader, "personid"),
                    ObjectiveYieldId: GetNullableInt32(reader, "objective_yield_id"),
                    CommentFlag: GetNullableInt32(reader, "comment_flag"),
                    Comments: GetNullableString(reader, "comments"),
                    OfficeNotes: GetNullableString(reader, "office_notes"),
                    PeriodId: GetNullableInt32(reader, "period_id"),
                    Stratum: GetNullableInt32(reader, "stratum"),
                    PartnerNo: GetNullableInt32(reader, "partner_no"),
                    Rep: GetNullableInt32(reader, "rep"),
                    KZero: GetNullableInt32(reader, "k_zero"),
                    X1: GetNullableInt32(reader, "x1"),
                    X2: GetNullableInt32(reader, "x2"),
                    X3: GetNullableInt32(reader, "x3"),
                    X4: GetNullableInt32(reader, "x4"),
                    X5: GetNullableInt32(reader, "x5"),
                    Flag1: GetNullableInt32(reader, "flag1"),
                    Flag2: GetNullableInt32(reader, "flag2"),
                    Flag3: GetNullableInt32(reader, "flag3"),
                    Flag4: GetNullableInt32(reader, "flag4"),
                    Flag5: GetNullableInt32(reader, "flag5"),
                    HoldFlag: GetNullableInt16(reader, "hold_flag"),
                    SZeroFlag: GetNullableInt32(reader, "s_zero_flag"),
                    Label1: GetNullableString(reader, "label1"),
                    Label2: GetNullableString(reader, "label2"),
                    Label3: GetNullableString(reader, "label3"),
                    Label4: GetNullableString(reader, "label4"),
                    Label5: GetNullableString(reader, "label5"),
                    OtherField: GetNullableString(reader, "other_field"),
                    CatiFlag: GetNullableInt16(reader, "cati_flag"),
                    CatiGroup: GetNullableString(reader, "cati_group"),
                    CapiFlag: GetNullableInt16(reader, "capi_flag"),
                    PhoneOnlyFlag: GetNullableInt16(reader, "phone_only_flag"),
                    CawiFlag: GetNullableInt16(reader, "cawi_flag"),
                    MailFlag: GetNullableInt16(reader, "mail_flag"),
                    PmcMailFlag: GetNullableInt16(reader, "pmc_mail_flag"),
                    RfoMailFlag: GetNullableInt16(reader, "rfo_mail_flag"),
                    MailStartDate: GetNullableDateTime(reader, "mail_start_date"),
                    MailStopDate: GetNullableDateTime(reader, "mail_stop_date"),
                    CawiStartDate: GetNullableDateTime(reader, "cawi_start_date"),
                    CawiStopDate: GetNullableDateTime(reader, "cawi_stop_date"),
                    CapiStartDate: GetNullableDateTime(reader, "capi_start_date"),
                    CapiStopDate: GetNullableDateTime(reader, "capi_stop_date"),
                    CatiStartDate: GetNullableDateTime(reader, "cati_start_date"),
                    CatiStopDate: GetNullableDateTime(reader, "cati_stop_date"),
                    CatiApp: GetNullableString(reader, "cati_app"),
                    ActiveStatusId: GetNullableInt32(reader, "active_status_id"),
                    OpDomStatusId: GetNullableInt32(reader, "op_dom_status_id"),
                    ManagerFlag: GetNullableInt32(reader, "mgr_flag"),
                    ManagerFlagChar: GetNullableString(reader, "mgr_flag_char"),
                    StatePoid: GetNullableDecimal(reader, "state_poid"),
                    OperationName: GetNullableString(reader, "oper_name"),
                    OperationAddress: GetNullableString(reader, "op_addr_delivery"),
                    OperationOtherAddress: GetNullableString(reader, "op_addr_other"),
                    OperationPlaceName: GetNullableString(reader, "op_place_name"),
                    OperationStateAbbreviation: GetNullableString(reader, "op_state_alpha"),
                    OperationZip5: GetNullableString(reader, "op_zip5"),
                    OperationZip4: GetNullableString(reader, "op_zip4"),
                    OperationPhone: GetNullableString(reader, "op_phone"),
                    OperationPhoneChar: GetNullableString(reader, "op_phone_char"),
                    OperationCellPhoneFlag: GetNullableInt32(reader, "op_phone_cell_flag"),
                    OperationCellPhoneFlagChar: GetNullableString(reader, "op_phone_cell_flag_char"),
                    OperationFax: GetNullableString(reader, "op_phone_fax"),
                    OperationFaxChar: GetNullableString(reader, "op_phone_fax_char"),
                    OperationFarmFlag: GetNullableInt32(reader, "farm_flag"),
                    AgriculturalBusinessFlag: GetNullableInt32(reader, "ag_business_flag"),
                    TaggedId: GetNullableInt32(reader, "tagged_id"),
                    OperationCountyId: GetNullableInt32(reader, "op_county_id"),
                    OperationDistrictId: GetNullableInt32(reader, "op_district_id"),
                    OperationEmail: GetNullableString(reader, "op_email"),
                    PersonStatusId: GetNullableInt32(reader, "person_status_id"),
                    LastName: GetNullableString(reader, "last_name"),
                    FirstName: GetNullableString(reader, "first_name"),
                    MiddleName: GetNullableString(reader, "middle_name"),
                    SuffixId: GetNullableString(reader, "suffix_id"),
                    WholeName: GetNullableString(reader, "whole_name"),
                    Address: GetNullableString(reader, "addr_delivery"),
                    OtherAddress: GetNullableString(reader, "addr_other"),
                    PersonPlaceName: GetNullableString(reader, "place_name"),
                    PersonStateAbbreviation: GetNullableString(reader, "state_alpha"),
                    PersonZip5: GetNullableString(reader, "zip5"),
                    PersonZip4: GetNullableString(reader, "zip4"),
                    PersonPhone: GetNullableString(reader, "phone"),
                    PersonPhoneChar: GetNullableString(reader, "phone_char"),
                    PersonCellPhoneFlag: GetNullableInt32(reader, "phone_cell_flag"),
                    PersonCellPhoneFlagChar: GetNullableString(reader, "phone_cell_flag_char"),
                    PersonOtherPhone: GetNullableString(reader, "phone_other"),
                    PersonOtherPhoneChar: GetNullableString(reader, "phone_other_char"),
                    PersonOtherCellPhoneFlag: GetNullableInt32(reader, "phone_oth_cell_flag"),
                    PersonOtherCellPhoneFlagChar: GetNullableString(reader, "phone_oth_cell_flag_char"),
                    PersonFaxPhone: GetNullableString(reader, "phone_fax"),
                    PersonFaxPhoneChar: GetNullableString(reader, "phone_fax_char"),
                    CountyId: GetNullableInt32(reader, "county_id"),
                    DistrictId: GetNullableInt32(reader, "district_id"),
                    PersonEmail: GetNullableString(reader, "e_mail"),
                    LanguageId: GetNullableInt32(reader, "language_id"),
                    Latitude: GetNullableDecimal(reader, "latitude"),
                    Longitude: GetNullableDecimal(reader, "longitude"),
                    DataCollectionModeCodeId: GetNullableInt32(reader, "dcm_code_id"),
                    DcmsCodeId: GetNullableInt32(reader, "dcms_code_id"),
                    PermSupervisorId: GetNullableInt32(reader, "perm_super_id"),
                    PermEnumeratorId: GetNullableInt32(reader, "perm_enum_id"),
                    SurveyCode: GetNullableString(reader, "survey_code"),
                    Barcode: GetNullableString(reader, "barcode"),
                    RfoReviewFlag: GetNullableInt32(reader, "rfo_review_flag"),
                    MarkedFlag: GetNullableInt32(reader, "marked_flag"),
                    Mseqnum: GetNullableInt32(reader, "mseqnum"),
                    CountyName: GetNullableString(reader, "county_name"),
                    OperationCountyName: GetNullableString(reader, "opcounty_name"),
                    RespDate: GetNullableDateTime(reader, "respdate"),
                    DataPull: GetNullableDateTime(reader, "datapull"),
                    ResponseCode: GetNullableInt32(reader, "response_code"),
                    RespondentCode: GetNullableInt32(reader, "respondent_code"),
                    ReportingModeCode: GetNullableInt32(reader, "reporting_mode_code"),
                    DataCaptureCode: GetNullableInt32(reader, "data_capture_code"),
                    DataCollectionEnumeratorId: GetNullableInt32(reader, "data_collection_enumerator_id"),
                    DataCollectionDate: GetNullableDateTime(reader, "data_collection_date"),
                    ReviewFlag: GetNullableInt16(reader, "review_flag"),
                    StartedFlag: GetNullableInt32(reader, "started_flag"),
                    SavedFlag: GetNullableInt32(reader, "saved_flag"),
                    OutOfBusinessId: GetNullableInt32(reader, "out_of_business_id"),
                    XStateLink: GetNullableString(reader, "xstatelink"),
                    CoordinationId: GetNullableInt64(reader, "coordination_id"),
                    SurveyTitle: GetNullableString(reader, "survey_title"),
                    SurveySubtitle: GetNullableString(reader, "survey_subtitle"),
                    Version: GetNullableString(reader, "version"),
                    StateName: GetNullableString(reader, "state_name"),
                    StateAbbreviation: GetNullableString(reader, "state_abbrev"),
                    OperationComments: GetNullableString(reader, "oper_comments"),
                    PersonComments: GetNullableString(reader, "person_comments"),
                    BackupDcmsCode: GetNullableInt32(reader, "backup_dcms_code"),
                    BackupEnumeratorId: GetNullableString(reader, "backup_enumerator_id"),
                    SupervisorId: GetNullableString(reader, "supervisor_id"),
                    EnumeratorId: GetNullableString(reader, "enumerator_id"),
                    EnumeratorName: GetNullableString(reader, "enumerator_name"),
                    SupervisorName: GetNullableString(reader, "supervisor_name"),
                    BackupSupervisorId: GetNullableString(reader, "backup_supervisor_id"),
                    SmsCheckIn: GetNullableInt32(reader, "sms_check_in"),
                    HistoricResponsePercentage1Year: GetNullableInt16(reader, "hr_pct_1yr"),
                    HistoricResponseSurveys1Year: GetNullableInt16(reader, "hr_surveys_1yr"),
                    HistoricResponseMode1Year: GetNullableString(reader, "hr_mode_1yr"),
                    HistoricResponsePercentage3Years: GetNullableInt16(reader, "hr_pct_3yr"),
                    HistoricResponseSurveys3Years: GetNullableInt16(reader, "hr_surveys_3yr"),
                    HistoricResponseMode3Years: GetNullableString(reader, "hr_mode_3yr"),
                    HistoricResponsePercentage5Years: GetNullableInt16(reader, "hr_pct_5yr"),
                    HistoricResponseSurveys5Years: GetNullableInt16(reader, "hr_surveys_5yr"),
                    HistoricResponseMode5Years: GetNullableString(reader, "hr_mode_5yr"),
                    StartDcDcmsCode: GetNullableInt32(reader, "start_dc_dcms_code"),
                    StartDcEnumerator: GetNullableInt32(reader, "start_dc_enum"),
                    StartDcSupervisor: GetNullableInt32(reader, "start_dc_super"),
                    PartnershipFlag: GetNullableInt16(reader, "partnership_flag"),
                    AddedRecordFlag: GetNullableInt32(reader, "added_record_flag"),
                    EnumeratorNotes: GetNullableString(reader, "enumerator_notes"),
                    CapiAppointmentFlag: GetNullableInt32(reader, "capi_appt_flag"),
                    CapiAttemptedContacts: GetNullableInt32(reader, "capi_attempted_contacts"),
                    Tier: GetNullableInt32(reader, "tier"),
                    ReactivatedFlag: GetNullableInt32(reader, "reactivated_flag"),
                    ReactivatedFlagDate: GetNullableDateTime(reader, "reactivated_flag_date"),
                    ManagerId: GetNullableString(reader, "manager_id"),
                    ManagerName: GetNullableString(reader, "manager_name"),
                    ComputedSkill: GetNullableInt16(reader, "computed_skill"),
                    ManualSkill: GetNullableInt16(reader, "manual_skill"),
                    CoordinationFlag: GetNullableInt16(reader, "coordination_flag"),
                    Ruid: GetNullableInt32(reader, "ruid"),
                    ActiveFlag: GetNullableInt16(reader, "active_flag"),
                    StartDate: GetNullableDateTime(reader, "start_date"),
                    StopDate: GetNullableDateTime(reader, "stop_date"),
                    SampleMonth: GetNullableInt32(reader, "sample_month"),
                    HqReviewFlag: GetNullableInt16(reader, "hq_review_flag"),
                    PopulatedRows: GetNullableInt32(reader, "populated_rows"),
                    FrequencyCode: GetNullableInt32(reader, "frequency_code"),
                    FrequencyDescription: GetNullableString(reader, "frequency_desc"),
                    ProjectCodeValue: GetNullableString(reader, "project_code"),
                    OmbNumberValue: GetNullableString(reader, "omb_number"),
                    OmbExpires: GetNullableDateTime(reader, "omb_expires"),
                    BaseMonth: GetNullableString(reader, "base_month"),
                    MarkedVersion: GetNullableString(reader, "marked_version"),
                    SampleNameValue: GetNullableString(reader, "sample_name"),
                    ElmoSurveyIdValue: GetNullableString(reader, "elmo_survey_id"),
                    ElmoPeriodId: GetNullableString(reader, "elmo_period_id"),
                    ElmoMonth: GetNullableString(reader, "elmo_month"),
                    TotalRowCount: GetNullableInt32(reader, "total_row_count")));
            }

            await fileLogger.LogAsync(
                $"Stored procedure '{GetSurveyInstancesProcedure}' completed successfully. Rows returned: {rows.Count}.",
                cancellationToken: cancellationToken);

            return rows;
        }
        catch (Exception ex)
        {
            await fileLogger.LogAsync(
                $"Stored procedure '{GetSurveyInstancesProcedure}' failed for host '{_options.Host}:{_options.Port}' database '{_options.Database}'.",
                ex,
                cancellationToken);
            logger.LogError(ex, "Failed to load survey instances from MySQL stored procedure {StoredProcedure}.", GetSurveyInstancesProcedure);
            return [];
        }
    }

    public async Task<SurveyInstanceDetailResponse?> GetSurveyInstanceDetailAsync(
        DateTime referenceDate,
        int surveyId,
        string sampleId,
        CancellationToken cancellationToken)
    {
        var sampleIdFilter = int.TryParse(sampleId, out var parsedSampleId) ? parsedSampleId : (int?)null;
        var rows = await GetSurveyInstancesFrameAsync(
            SurveyId: surveyId,
            SampleId: sampleIdFilter,
            Search: null,
            SurveyDate: referenceDate,
            StartDate: null,
            StopDate: null,
            MailFlag: null,
            CawiFlag: null,
            CatiFlag: null,
            CapiFlag: null,
            HqReviewFlag: null,
            Limit: 25,
            Offset: 0,
            cancellationToken);
        var instanceRows = rows
            .Where(row =>
                row.SurveyId == surveyId &&
                row.SurveyDate.HasValue &&
                row.SurveyDate.Value.Date == referenceDate.Date &&
                string.Equals(FormatValue(row.SampleId), sampleId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (instanceRows.Count == 0)
            return null;

        var first = instanceRows[0];
        var checkinRows = await GetSurveyInstanceCheckinRowsAsync(surveyId, referenceDate, cancellationToken);
        var totalSample = first.PopulatedRows ?? instanceRows.Count;
        var totalReceived = checkinRows.Count(row => row.RespDate.HasValue);
        var totalNotReceived = Math.Max(0, totalSample - totalReceived);

        return new SurveyInstanceDetailResponse(
            SampleId: FormatValue(first.SampleId),
            SampleName: FormatValue(first.SampleName),
            SurveyId: surveyId,
            Title: FormatValue(first.SurveyTitle),
            SubTitle: FormatValue(first.SurveySubtitle),
            SurveyFrequency: FormatValue(first.FrequencyDescription),
            Version: FormatValue(first.MarkedVersion),
            SurveyDate: first.SurveyDate?.Date ?? referenceDate.Date,
            ReferenceDate: referenceDate.Date,
            SurveyStartDate: first.StartDate ?? GetEarliestModeStart(first) ?? referenceDate.Date,
            SurveyStopDate: first.StopDate ?? GetLatestModeStop(first) ?? referenceDate.Date,
            HqSurveyAdmin: first.HqReviewFlag == 1 ? "In HQ Review" : MissingValue,
            ProjectCode: FormatValue(first.ProjectCodeValue ?? first.SurveyCode),
            OmbDocket: FormatValue(first.OmbNumber),
            OmbExpiration: FormatDate(first.OmbExpires),
            Modes: BuildModeWindows(first),
            OpDomCounts: [],
            DcmsCounts: [],
            DataCollectionStatusCounts: BuildDataCollectionStatusCounts(checkinRows),
            ReportsReceivedByModeCounts: BuildReportsReceivedByModeCounts(checkinRows),
            TotalSample: totalSample,
            TotalReceived: totalReceived,
            TotalDeleted: totalNotReceived,
            BudgetAllocation: 0m,
            RespondentInstancesLast1Year: 0,
            RespondentInstancesLast3Years: 0,
            RespondentInstancesLast5Years: 0,
            ResponseHistoryRate: 0m,
            ResponseHistoryBreakdown: [],
            SurveyDesignerAssociations: [],
            CollectionMaterials: [],
            RecordRows: [],
            FullRecord: BuildFullRecord(first));
    }

    private async Task<IReadOnlyList<CheckinValueRow>> GetSurveyInstanceCheckinRowsAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new MySqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = GetSurveyInstanceCheckinValuesProcedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 120;
            command.Parameters.Add(new MySqlParameter("filter_survey_id", MySqlDbType.Int32) { Value = surveyId });
            command.Parameters.Add(new MySqlParameter("filter_survey_date", MySqlDbType.DateTime) { Value = surveyDate.Date });

            var rows = new List<CheckinValueRow>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new CheckinValueRow(
                    RespDate: GetNullableDateTime(reader, "respdate"),
                    DataCaptureCode: GetNullableInt32(reader, "data_capture_code"),
                    DcmsCodeId: GetNullableInt32(reader, "dcms_code_id"),
                    ResponseCode: GetNullableInt32(reader, "response_code")));
            }

            return rows;
        }
        catch (Exception ex)
        {
            await fileLogger.LogAsync(
                $"Stored procedure '{GetSurveyInstanceCheckinValuesProcedure}' failed for survey_id '{surveyId}' and survey_date '{surveyDate:yyyy-MM-dd}'.",
                ex,
                cancellationToken);
            logger.LogError(ex, "Failed to load survey instance check-in values from MySQL stored procedure {StoredProcedure}.", GetSurveyInstanceCheckinValuesProcedure);
            throw;
        }
    }

    private static List<ModeWindow> BuildModeWindows(SurveyGridRow row)
    {
        var modes = new List<ModeWindow>
        {
            new("Mail", row.MailStartDate, row.MailStopDate),
            new("CAWI", row.CawiStartDate, row.CawiStopDate),
            new("CAPI", row.CapiStartDate, row.CapiStopDate),
            new("CATI", row.CatiStartDate, row.CatiStopDate)
        };

        return modes
            .Where(mode => mode.StartDate.HasValue || mode.StopDate.HasValue)
            .ToList();
    }

    private static DateTime? GetEarliestModeStart(SurveyGridRow row)
        => new[] { row.MailStartDate, row.CawiStartDate, row.CapiStartDate, row.CatiStartDate }
            .Where(date => date.HasValue)
            .Min();

    private static DateTime? GetLatestModeStop(SurveyGridRow row)
        => new[] { row.MailStopDate, row.CawiStopDate, row.CapiStopDate, row.CatiStopDate }
            .Where(date => date.HasValue)
            .Max();

    private static List<CountItem> BuildDataCollectionStatusCounts(IReadOnlyCollection<CheckinValueRow> rows)
    {
        return
        [
            new("1", "Complete", rows.Count(row => row.RespDate.HasValue && row.ResponseCode == 1)),
            new("2", "Refusal", rows.Count(row => row.RespDate.HasValue && row.ResponseCode == 2)),
            new("3", "Inaccessible", rows.Count(row => row.RespDate.HasValue && row.ResponseCode == 3)),
            new("Other", "Other Complete", rows.Count(row => row.RespDate.HasValue && row.ResponseCode is not (1 or 2 or 3))),
            new("900+", "Office Hold", rows.Count(row => row.DcmsCodeId.GetValueOrDefault() >= 900)),
            new("Active", "Active & Not Checked-In", rows.Count(row => !row.RespDate.HasValue && row.DcmsCodeId.HasValue && row.DcmsCodeId < 900))
        ];
    }

    private static List<CountItem> BuildReportsReceivedByModeCounts(IReadOnlyCollection<CheckinValueRow> rows)
    {
        var receivedRows = rows
            .Where(row => row.RespDate.HasValue)
            .ToList();

        return
        [
            new("Mail", "Mail", receivedRows.Count(row => row.DataCaptureCode == 1)),
            new("CAWI", "CAWI", receivedRows.Count(row => row.DataCaptureCode == 5)),
            new("CAPI", "CAPI", receivedRows.Count(row => row.DataCaptureCode == 6)),
            new("READI", "READI", receivedRows.Count(row => row.DataCaptureCode == 10)),
            new("Other", "Other", receivedRows.Count(row => row.DataCaptureCode > 0 && row.DataCaptureCode is not (1 or 5 or 6 or 10)))
        ];
    }

    private static List<DetailField> BuildFullRecord(SurveyGridRow row)
        =>
        [
            new("sample_id", FormatValue(row.SampleId)),
            new("survey_id", FormatValue(row.SurveyId)),
            new("survey_date", FormatDate(row.SurveyDate)),
            new("survey_title", FormatValue(row.SurveyTitle)),
            new("survey_subtitle", FormatValue(row.SurveySubtitle)),
            new("frequency_code", FormatValue(row.FrequencyCode)),
            new("frequency_desc", FormatValue(row.FrequencyDescription)),
            new("project_code", FormatValue(row.ProjectCodeValue ?? row.SurveyCode)),
            new("omb_number", FormatValue(row.OmbNumber)),
            new("omb_expires", FormatDate(row.OmbExpires)),
            new("sample_name", FormatValue(row.SampleName)),
            new("elmo_survey_id", FormatValue(row.ElmoSurveyId)),
            new("elmo_period_id", FormatValue(row.ElmoPeriodId)),
            new("elmo_month", FormatValue(row.ElmoMonth)),
            new("marked_version", FormatValue(row.MarkedVersion)),
            new("base_month", FormatValue(row.BaseMonth)),
            new("hq_review_flag", FormatValue(row.HqReviewFlag)),
            new("populated_rows", FormatValue(row.PopulatedRows)),
            new("survey_code", FormatValue(row.SurveyCode)),
            new("respdate", FormatDate(row.RespDate)),
            new("response_code", FormatValue(row.ResponseCode)),
            new("dcms_code_id", FormatValue(row.DcmsCodeId)),
            new("data_capture_code", FormatValue(row.DataCaptureCode))
        ];

    private static string FormatDate(DateTime? value)
        => DisplayValue.Date(value);

    private static string FormatValue(object? value)
        => DisplayValue.Text(value);

    private sealed record CheckinValueRow(
        DateTime? RespDate,
        int? DataCaptureCode,
        int? DcmsCodeId,
        int? ResponseCode);

    private string BuildConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = _options.Host,
            Port = _options.Port,
            Database = _options.Database,
            UserID = _options.Username,
            Password = _options.Password,
            ConnectionTimeout = 15,
            DefaultCommandTimeout = 120
        };

        return builder.ConnectionString;
    }

    private static int? GetNullableInt32(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static short? GetNullableInt16(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return Convert.ToInt16(reader.GetValue(ordinal));
    }

    private static long? GetNullableInt64(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTime? GetNullableDateTime(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return reader.GetDateTime(ordinal);
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        if (!TryGetOrdinal(reader, columnName, out var ordinal))
            return null;
        if (reader.IsDBNull(ordinal))
            return null;

        return Convert.ToString(reader.GetValue(ordinal));
    }

    private static bool TryGetOrdinal(MySqlDataReader reader, string columnName, out int ordinal)
    {
        try
        {
            ordinal = reader.GetOrdinal(columnName);
            return true;
        }
        catch (IndexOutOfRangeException)
        {
            ordinal = -1;
            return false;
        }
    }
}
