using Microsoft.Extensions.Options;
using MySqlConnector;
using SMSLiteStaticDemo.Server.Configuration;
using SMSLiteStaticDemo.Server.Logging;
using SMSLiteStaticDemo.Services;
using System.Data;

namespace SMSLiteStaticDemo.Server.Services;

public sealed class MySqlSurveyInstanceRepository(
    IOptions<SmsLiteDatabaseOptions> options,
    IUserFileLogger fileLogger,
    ILogger<MySqlSurveyInstanceRepository> logger) : ISurveyInstanceRepository
{
    private readonly SmsLiteDatabaseOptions _options = options.Value;

    public async Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(CancellationToken cancellationToken)
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
                $"Executing stored procedure '{_options.StoredProcedure}' against '{_options.Database}' on '{_options.Host}:{_options.Port}' using source view '{_options.SourceView}'.",
                cancellationToken: cancellationToken);

            await using var connection = new MySqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = _options.StoredProcedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 120;

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
                    CatiApp: GetNullableInt16(reader, "cati_app"),
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
                    Ruid: GetNullableInt32(reader, "ruid")));
            }

            await fileLogger.LogAsync(
                $"Stored procedure '{_options.StoredProcedure}' completed successfully. Rows returned: {rows.Count}.",
                cancellationToken: cancellationToken);

            return rows;
        }
        catch (Exception ex)
        {
            await fileLogger.LogAsync(
                $"Stored procedure '{_options.StoredProcedure}' failed for host '{_options.Host}:{_options.Port}' database '{_options.Database}'.",
                ex,
                cancellationToken);
            logger.LogError(ex, "Failed to load survey instances from MySQL stored procedure {StoredProcedure}.", _options.StoredProcedure);
            throw;
        }
    }

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
