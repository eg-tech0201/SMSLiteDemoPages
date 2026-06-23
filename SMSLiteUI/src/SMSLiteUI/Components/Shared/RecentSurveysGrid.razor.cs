using System.Net.Http.Json;
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
            var rows = await Http.GetFromJsonAsync<List<SurveyGridRow>>("api/survey-instances") ?? [];
            Rows = BuildUniqueInstanceRows(rows).Take(PageSize).ToList();
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

    private static List<SurveyGridRow> BuildUniqueInstanceRows(IEnumerable<SurveyGridRow> rows)
        => rows
            .Where(row => row.SurveyId.HasValue && row.SurveyDate.HasValue && row.SampleId.HasValue)
            .GroupBy(row => new { row.SurveyId, SurveyDate = row.SurveyDate!.Value.Date, row.SampleId })
            .Select(group => group.First())
            .OrderByDescending(row => row.SurveyDate)
            .ThenBy(row => row.SurveyId)
            .ThenBy(row => row.SampleId)
            .ToList();

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
