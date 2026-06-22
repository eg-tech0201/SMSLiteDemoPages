using Microsoft.Extensions.Options;
using MySqlConnector;
using sms_lite.Models;
using sms_lite.Server.Configuration;
using System.Data;

namespace sms_lite.Server.Services;

public sealed class SurveyInstanceDao(
    IOptions<SmsLiteDatabaseOptions> options,
    ILogger<SurveyInstanceDao> logger)
{
    private readonly SmsLiteDatabaseOptions _options = options.Value;

    public async Task<IReadOnlyList<SurveyInstance>> GetAsync(
        SurveyInstanceQuery query,
        CancellationToken cancellationToken)
    {
        if (!HasDatabaseCredentials())
            return [];

        try
        {
            await using var connection = new MySqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);
            await using var command = CreateStoredProcedureCommand(connection, SmsLiteStoredProcedures.GetSurveyInstances);
            command.Parameters.Add("row_limit", MySqlDbType.Int32).Value = query.RowLimit;
            command.Parameters.Add("row_offset", MySqlDbType.Int32).Value = query.RowOffset;
            AddNullable(command, "filter_survey_id", MySqlDbType.Int32, query.SurveyId);
            AddNullable(command, "filter_sample_id", MySqlDbType.Int32, query.SampleId);
            AddNullable(command, "filter_survey_search", MySqlDbType.VarChar, query.SurveySearch?.Trim());
            AddNullable(command, "filter_survey_date", MySqlDbType.Date, query.SurveyDate?.Date);
            AddNullable(command, "filter_start_date", MySqlDbType.Date, query.StartDate?.Date);
            AddNullable(command, "filter_stop_date", MySqlDbType.Date, query.StopDate?.Date);
            AddNullable(command, "filter_mail_flag", MySqlDbType.Int16, query.Mail ? (short)1 : null);
            AddNullable(command, "filter_cawi_flag", MySqlDbType.Int16, query.Cawi ? (short)1 : null);
            AddNullable(command, "filter_cati_flag", MySqlDbType.Int16, query.Cati ? (short)1 : null);
            AddNullable(command, "filter_capi_flag", MySqlDbType.Int16, query.Capi ? (short)1 : null);
            AddNullable(command, "filter_hq_review_flag", MySqlDbType.Int16, query.HqReview ? (short)1 : null);

            var rows = new List<SurveyInstance>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(MapSurveyInstance(reader));

            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stored procedure {Procedure} failed.", SmsLiteStoredProcedures.GetSurveyInstances);
            return [];
        }
    }

    public async Task<IReadOnlyList<SurveyInstanceCheckin>> GetCheckinsAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateStoredProcedureCommand(
            connection,
            SmsLiteStoredProcedures.GetSurveyInstanceCheckinValues);
        command.Parameters.Add("filter_survey_id", MySqlDbType.Int32).Value = surveyId;
        command.Parameters.Add("filter_survey_date", MySqlDbType.Date).Value = surveyDate.Date;

        var rows = new List<SurveyInstanceCheckin>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SurveyInstanceCheckin(
                ResponseDate: reader.NullableDateTime("respdate"),
                ResponseCode: reader.NullableInt32("response_code"),
                DataCaptureCode: reader.NullableInt32("data_capture_code"),
                DcmsCodeId: reader.NullableInt32("dcms_code_id")));
        }

        return rows;
    }

    private static SurveyInstance MapSurveyInstance(MySqlDataReader reader)
        => new(
            SurveyId: reader.NullableInt32("survey_id"),
            SurveyDate: reader.NullableDateTime("survey_date"),
            SampleId: reader.NullableInt32("sample_id"),
            PeriodId: reader.NullableInt32("period_id"),
            ActiveFlag: reader.NullableInt16("active_flag"),
            StartDate: reader.NullableDateTime("start_date"),
            StopDate: reader.NullableDateTime("stop_date"),
            SampleMonth: reader.NullableInt32("sample_month"),
            HqReviewFlag: reader.NullableInt16("hq_review_flag"),
            PopulatedRows: reader.NullableInt32("populated_rows"),
            MailFlag: reader.NullableInt16("mail_flag"),
            CawiFlag: reader.NullableInt16("cawi_flag"),
            CatiFlag: reader.NullableInt16("cati_flag"),
            CapiFlag: reader.NullableInt16("capi_flag"),
            MailStartDate: reader.NullableDateTime("mail_start_date"),
            MailStopDate: reader.NullableDateTime("mail_stop_date"),
            CawiStartDate: reader.NullableDateTime("cawi_start_date"),
            CawiStopDate: reader.NullableDateTime("cawi_stop_date"),
            CapiStartDate: reader.NullableDateTime("capi_start_date"),
            CapiStopDate: reader.NullableDateTime("capi_stop_date"),
            CatiStartDate: reader.NullableDateTime("cati_start_date"),
            CatiStopDate: reader.NullableDateTime("cati_stop_date"),
            CatiApp: reader.NullableString("cati_app"),
            SurveyTitle: reader.NullableString("survey_title"),
            SurveySubtitle: reader.NullableString("survey_subtitle"),
            FrequencyCode: reader.NullableInt32("frequency_code"),
            FrequencyDescription: reader.NullableString("frequency_desc"),
            ProjectCode: reader.NullableString("project_code"),
            OmbNumber: reader.NullableString("omb_number"),
            OmbExpires: reader.NullableDateTime("omb_expires"),
            BaseMonth: reader.NullableString("base_month"),
            MarkedVersion: reader.NullableString("marked_version"),
            SampleName: reader.NullableString("sample_name"),
            ElmoSurveyId: reader.NullableString("elmo_survey_id"),
            ElmoPeriodId: reader.NullableString("elmo_period_id"),
            ElmoMonth: reader.NullableString("elmo_month"),
            TotalRowCount: reader.NullableInt32("total_row_count"));

    private bool HasDatabaseCredentials()
    {
        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            return true;

        logger.LogWarning("SmsLiteDatabase username/password are blank.");
        return false;
    }

    private static MySqlCommand CreateStoredProcedureCommand(MySqlConnection connection, string procedure)
        => new(procedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

    private static void AddNullable(MySqlCommand command, string name, MySqlDbType type, object? value)
        => command.Parameters.Add(name, type).Value = value ?? DBNull.Value;

    private string BuildConnectionString()
        => new MySqlConnectionStringBuilder
        {
            Server = _options.Host,
            Port = _options.Port,
            Database = _options.Database,
            UserID = _options.Username,
            Password = _options.Password,
            ConnectionTimeout = 15,
            DefaultCommandTimeout = 120
        }.ConnectionString;
}
