using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Components.Shared;

public partial class RecentSurveysGrid
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private UserRecentSurveysCacheService RecentSurveysCache { get; set; } = default!;

    [Parameter] public string SectionCssClass { get; set; } = "recent-surveys-panel";
    [Parameter] public string Title { get; set; } = "My Recent Surveys";
    [Parameter] public int PageSize { get; set; } = 10;

    private List<SurveyGridRow> Rows { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private string? Error { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var rows = await RecentSurveysCache.GetRecentSurveysAsync(CancellationToken.None);
            Rows = rows.Take(PageSize).Select(ToGridRow).ToList();
        }
        catch (Exception ex)
        {
            Error = $"Recent surveys could not be loaded. {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static SurveyGridRow ToGridRow(UserRecentSurveyCacheItem item)
        => new(
            SurveyId: item.SurveyId,
            SurveyDate: item.ReferenceDate,
            SampleId: int.TryParse(item.SampleId, out var sampleId) ? sampleId : null,
            PeriodId: null,
            ActiveFlag: null,
            StartDate: null,
            StopDate: null,
            SampleMonth: null,
            HqReviewFlag: null,
            PopulatedRows: null,
            MailFlag: null,
            CawiFlag: null,
            CatiFlag: null,
            CapiFlag: null,
            MailStartDate: null,
            MailStopDate: null,
            CawiStartDate: null,
            CawiStopDate: null,
            CapiStartDate: null,
            CapiStopDate: null,
            CatiStartDate: null,
            CatiStopDate: null,
            CatiApp: null,
            SurveyTitle: item.SurveyTitle,
            SurveySubtitle: null,
            FrequencyCode: null,
            FrequencyDescription: null,
            ProjectCode: null,
            OmbNumber: null,
            OmbExpires: null,
            BaseMonth: null,
            MarkedVersion: null,
            SampleName: null,
            ElmoSurveyId: null,
            ElmoPeriodId: null,
            ElmoMonth: null,
            TotalRowCount: null);

    private static string BuildDetailHref(SurveyGridRow row)
    {
        if (!row.SurveyId.HasValue || !row.SurveyDate.HasValue || !row.SampleId.HasValue)
            return "surveys/results";

        return $"surveys/details?referenceDate={row.SurveyDate.Value:yyyy-MM-dd}" +
               $"&surveyId={row.SurveyId.Value}" +
               $"&sampleId={Uri.EscapeDataString(row.SampleId.Value.ToString())}" +
               $"&surveyTitle={Uri.EscapeDataString(row.SurveyTitle ?? string.Empty)}" +
               $"&resultsUrl={Uri.EscapeDataString("/surveys/results")}";
    }

    private static string FormatValue(object? value)
        => DisplayValue.Text(value);

    private void OnRowClick(GridRowClickEventArgs e)
    {
        if (e.Grid.GetDataItem(e.VisibleIndex) is SurveyGridRow row)
            Navigation.NavigateTo(BuildDetailHref(row));
    }
}
