using System.Net.Http.Json;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Pages.Surveys;

public partial class SurveyDetails
{
    [Parameter] public int? SurveyId { get; set; }
    [SupplyParameterFromQuery(Name = "surveyId")] public int? SurveyIdQuery { get; set; }
    [SupplyParameterFromQuery(Name = "sampleId")] public string? SampleId { get; set; }
    [SupplyParameterFromQuery(Name = "referenceDate")] public string? ReferenceDate { get; set; }

    private SurveyInstanceDetailResponse? Detail { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private List<CountSlice> DataCollectionStatusChartItems => Detail is null
        ? []
        :
        [
            new("Complete", Detail.CompleteCount),
            new("Refusal", Detail.RefusalCount),
            new("Inaccessible", Detail.InaccessibleCount),
            new("Other Complete", Detail.OtherCompleteCount),
            new("Office Hold", Detail.OfficeHoldCount),
            new("Active & Not Checked-In", Detail.ActiveNotCheckedInCount)
        ];

    private List<PieSlice> DataCollectionStatusPieItems => Detail is null
        ? []
        : DataCollectionStatusChartItems
            .Select(item => new PieSlice(item.Label, item.Count))
            .ToList();

    private List<CountSlice> ReportsReceivedByModeChartItems => Detail is null
        ? []
        :
        [
            new("Mail", Detail.MailReceivedCount),
            new("CAWI", Detail.CawiReceivedCount),
            new("CAPI", Detail.CapiReceivedCount),
            new("READI", Detail.ReadiReceivedCount),
            new("Other", Detail.OtherModeReceivedCount)
        ];

    private List<PieSlice> ReportsReceivedByModePieItems => Detail is null
        ? []
        : ReportsReceivedByModeChartItems
            .Select(item => new PieSlice(item.Label, item.Count))
            .ToList();

    private double ReceivedPercent => GetPercent(Detail?.TotalReceived ?? 0, Detail?.TotalSample ?? 0);
    private double NotReceivedPercent => GetPercent(Detail?.TotalDeleted ?? 0, Detail?.TotalSample ?? 0);

    protected override async Task OnParametersSetAsync()
    {
        Detail = null;
        ErrorMessage = null;

        var surveyId = SurveyIdQuery ?? SurveyId;
        if (!surveyId.HasValue ||
            string.IsNullOrWhiteSpace(SampleId) ||
            !DateTime.TryParse(ReferenceDate, out var referenceDate))
        {
            ErrorMessage = "Select Survey ID, Survey Date, and Sample ID from Survey Results.";
            return;
        }

        IsLoading = true;
        try
        {
            var url = $"api/survey-instances/detail?referenceDate={referenceDate:yyyy-MM-dd}&surveyId={surveyId.Value}&sampleId={Uri.EscapeDataString(SampleId)}";
            Detail = await Http.GetFromJsonAsync<SurveyInstanceDetailResponse>(url);
            if (Detail is null)
                ErrorMessage = "No survey detail found for the selected instance.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to load survey {SurveyId}, date {ReferenceDate}, sample {SampleId}.", surveyId, referenceDate, SampleId);
            ErrorMessage = "Unable to load survey details.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("MM/dd/yyyy") : DisplayValue.Missing;

    private static string FormatValue(object? value)
        => DisplayValue.Text(value);

    private static double GetPercent(int count, int total)
        => total <= 0 ? 0 : count * 100d / total;

    private sealed record CountSlice(string Label, int Count);
    private sealed record PieSlice(string Label, int Count);
}
