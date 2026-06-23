using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteCommandAPI.Repositories;

namespace SMSLiteCommandAPI.Services;

public sealed class SurveyInstanceService(SurveyInstanceDao dao)
{
    private const int DefaultPageSize = 1000;

    public async Task<IReadOnlyList<SurveyGridRow>> GetSurveyInstancesAsync(
        CancellationToken cancellationToken)
    {
        var instances = await dao.GetAsync(
            new SurveyInstanceQuery(DefaultPageSize, 0),
            cancellationToken);

        return instances.Select(ToGridRow).ToList();
    }

    public async Task<SurveyInstancePageResponse> GetSurveyInstancesPageAsync(
        SurveyInstancePageRequest request,
        CancellationToken cancellationToken)
    {
        var query = new SurveyInstanceQuery(
            RowLimit: Math.Clamp(request.RowLimit, 1, 100),
            RowOffset: Math.Max(0, request.RowOffset),
            SurveyId: request.SurveyId,
            SampleId: request.SampleId,
            SurveySearch: request.SurveySearch,
            SurveyDate: request.SurveyDate,
            StartDate: request.StartDate,
            StopDate: request.StopDate,
            Mail: request.Mail,
            Cawi: request.Cawi,
            Cati: request.Cati,
            Capi: request.Capi,
            HqReview: request.HqReview);
        var instances = await dao.GetAsync(query, cancellationToken);

        return new SurveyInstancePageResponse(
            instances.Select(ToGridRow).ToList(),
            instances.FirstOrDefault()?.TotalRowCount ?? 0);
    }

    public async Task<SurveyInstanceDetailResponse?> GetSurveyInstanceDetailAsync(
        DateTime referenceDate,
        int surveyId,
        string sampleId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(sampleId, out var parsedSampleId))
            return null;

        var survey = (await dao.GetAsync(
            new SurveyInstanceQuery(
                RowLimit: 1,
                RowOffset: 0,
                SurveyId: surveyId,
                SampleId: parsedSampleId,
                SurveyDate: referenceDate.Date),
            cancellationToken)).FirstOrDefault();
        if (survey is null)
            return null;

        var checkins = await dao.GetCheckinsAsync(
            surveyId,
            referenceDate.Date,
            cancellationToken);

        return ToDetailResponse(survey, checkins, referenceDate.Date);
    }

    private static SurveyGridRow ToGridRow(SurveyInstance survey)
        => new(
            survey.SurveyId,
            survey.SurveyDate,
            survey.SampleId,
            survey.PeriodId,
            survey.ActiveFlag,
            survey.StartDate,
            survey.StopDate,
            survey.SampleMonth,
            survey.HqReviewFlag,
            survey.PopulatedRows,
            survey.MailFlag,
            survey.CawiFlag,
            survey.CatiFlag,
            survey.CapiFlag,
            survey.MailStartDate,
            survey.MailStopDate,
            survey.CawiStartDate,
            survey.CawiStopDate,
            survey.CapiStartDate,
            survey.CapiStopDate,
            survey.CatiStartDate,
            survey.CatiStopDate,
            survey.CatiApp,
            survey.SurveyTitle,
            survey.SurveySubtitle,
            survey.FrequencyCode,
            survey.FrequencyDescription,
            survey.ProjectCode,
            survey.OmbNumber,
            survey.OmbExpires,
            survey.BaseMonth,
            survey.MarkedVersion,
            survey.SampleName,
            survey.ElmoSurveyId,
            survey.ElmoPeriodId,
            survey.ElmoMonth,
            survey.TotalRowCount);

    private static SurveyInstanceDetailResponse ToDetailResponse(
        SurveyInstance survey,
        IReadOnlyCollection<SurveyInstanceCheckin> checkins,
        DateTime referenceDate)
    {
        var receivedCount = checkins.Count(row => row.ResponseDate.HasValue);

        return new SurveyInstanceDetailResponse(
            SampleId: survey.SampleId,
            SampleName: survey.SampleName,
            SurveyId: survey.SurveyId,
            Title: survey.SurveyTitle,
            SubTitle: survey.SurveySubtitle,
            SurveyFrequency: survey.FrequencyDescription,
            Version: survey.MarkedVersion,
            SurveyDate: survey.SurveyDate?.Date,
            ReferenceDate: referenceDate,
            SurveyStartDate: survey.StartDate ?? FirstDate(
                survey.MailStartDate,
                survey.CawiStartDate,
                survey.CapiStartDate,
                survey.CatiStartDate),
            SurveyStopDate: survey.StopDate ?? LastDate(
                survey.MailStopDate,
                survey.CawiStopDate,
                survey.CapiStopDate,
                survey.CatiStopDate),
            HqReview: survey.HqReviewFlag == 1,
            ProjectCode: survey.ProjectCode,
            OmbNumber: survey.OmbNumber,
            OmbExpiration: survey.OmbExpires,
            ElmoSurveyId: survey.ElmoSurveyId,
            ElmoPeriodId: survey.ElmoPeriodId,
            ElmoMonth: survey.ElmoMonth,
            MarkedVersion: survey.MarkedVersion,
            BaseMonth: survey.BaseMonth,
            MailStartDate: survey.MailStartDate,
            MailStopDate: survey.MailStopDate,
            CawiStartDate: survey.CawiStartDate,
            CawiStopDate: survey.CawiStopDate,
            CapiStartDate: survey.CapiStartDate,
            CapiStopDate: survey.CapiStopDate,
            CatiStartDate: survey.CatiStartDate,
            CatiStopDate: survey.CatiStopDate,
            TotalSample: checkins.Count,
            TotalReceived: receivedCount,
            TotalDeleted: checkins.Count - receivedCount,
            CompleteCount: checkins.Count(row => row.ResponseDate.HasValue && row.ResponseCode == 1),
            RefusalCount: checkins.Count(row => row.ResponseDate.HasValue && row.ResponseCode == 2),
            InaccessibleCount: checkins.Count(row => row.ResponseDate.HasValue && row.ResponseCode == 3),
            OtherCompleteCount: checkins.Count(row =>
                row.ResponseDate.HasValue &&
                row.ResponseCode is not (1 or 2 or 3)),
            OfficeHoldCount: checkins.Count(row =>
                !row.ResponseDate.HasValue &&
                row.DcmsCodeId >= 900),
            ActiveNotCheckedInCount: checkins.Count(row =>
                !row.ResponseDate.HasValue &&
                row.DcmsCodeId < 900),
            MailReceivedCount: checkins.Count(row => row.ResponseDate.HasValue && row.DataCaptureCode == 1),
            CawiReceivedCount: checkins.Count(row => row.ResponseDate.HasValue && row.DataCaptureCode == 5),
            CapiReceivedCount: checkins.Count(row => row.ResponseDate.HasValue && row.DataCaptureCode == 6),
            ReadiReceivedCount: checkins.Count(row => row.ResponseDate.HasValue && row.DataCaptureCode == 10),
            OtherModeReceivedCount: checkins.Count(row =>
                row.ResponseDate.HasValue &&
                row.DataCaptureCode is not (1 or 5 or 6 or 10)));
    }

    private static DateTime? FirstDate(params DateTime?[] values)
        => values.Where(value => value.HasValue).Min();

    private static DateTime? LastDate(params DateTime?[] values)
        => values.Where(value => value.HasValue).Max();
}
