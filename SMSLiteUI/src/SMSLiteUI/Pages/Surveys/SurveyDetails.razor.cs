using System.Net.Http.Json;
using System.Drawing;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Hosting;
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
    private static readonly Color[] PiePalette =
    [
        Color.FromArgb(38, 117, 84),
        Color.FromArgb(196, 78, 82),
        Color.FromArgb(219, 149, 38),
        Color.FromArgb(46, 105, 170),
        Color.FromArgb(127, 85, 177),
        Color.FromArgb(30, 142, 160),
        Color.FromArgb(116, 97, 72)
    ];

    [Parameter] public int? SurveyId { get; set; }
    [SupplyParameterFromQuery(Name = "surveyId")] public int? SurveyIdQuery { get; set; }
    [SupplyParameterFromQuery(Name = "sampleId")] public string? SampleId { get; set; }
    [SupplyParameterFromQuery(Name = "referenceDate")] public string? ReferenceDate { get; set; }
    [SupplyParameterFromQuery(Name = "demo")] public bool? Demo { get; set; }
    [Inject] private IWebHostEnvironment Environment { get; set; } = default!;

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
        : BuildPieSlices(DataCollectionStatusChartItems);

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
        : BuildPieSlices(ReportsReceivedByModeChartItems);

    private double ReceivedPercent => GetPercent(Detail?.TotalReceived ?? 0, Detail?.TotalSample ?? 0);
    private double NotReceivedPercent => GetPercent(Detail?.TotalDeleted ?? 0, Detail?.TotalSample ?? 0);

    protected override async Task OnParametersSetAsync()
    {
        Detail = null;
        ErrorMessage = null;

        if (Demo == true)
        {
            if (!Environment.IsDevelopment())
            {
                ErrorMessage = "Demo survey details are only available in Development.";
                return;
            }

            Detail = BuildDemoDetail();
            return;
        }

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
            {
                ErrorMessage = "No survey detail found for the selected instance.";
            }
            else
            {
                await RecentSurveysCache.MarkOpenedAsync(
                    surveyId.Value,
                    SampleId,
                    referenceDate,
                    Detail.Title,
                    CancellationToken.None);
            }
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

    private static string FormatColor(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string FormatPercent(double value)
        => $"{value:0.0}%";

    private static SurveyInstanceDetailResponse BuildDemoDetail()
        => new(
            SampleId: 1204,
            SampleName: "Quarterly Crops Sample",
            SurveyId: 1042,
            Title: "Quarterly Agricultural Survey",
            SubTitle: "June 2026 Production and Stocks",
            SurveyFrequency: "Quarterly",
            Version: "2026.06",
            SurveyDate: new DateTime(2026, 6, 1),
            ReferenceDate: new DateTime(2026, 6, 1),
            SurveyStartDate: new DateTime(2026, 5, 15),
            SurveyStopDate: new DateTime(2026, 7, 12),
            HqReview: true,
            ProjectCode: "CROPS-QTR",
            OmbNumber: "0535-0213",
            OmbExpiration: new DateTime(2027, 12, 31),
            ElmoSurveyId: "ELMO-1042",
            ElmoPeriodId: "202606",
            ElmoMonth: "June",
            MarkedVersion: "A",
            BaseMonth: "June",
            MailStartDate: new DateTime(2026, 5, 15),
            MailStopDate: new DateTime(2026, 6, 30),
            CawiStartDate: new DateTime(2026, 5, 20),
            CawiStopDate: new DateTime(2026, 7, 5),
            CapiStartDate: new DateTime(2026, 6, 1),
            CapiStopDate: new DateTime(2026, 7, 12),
            CatiStartDate: new DateTime(2026, 6, 3),
            CatiStopDate: new DateTime(2026, 7, 10),
            TotalSample: 1280,
            TotalReceived: 884,
            TotalDeleted: 396,
            CompleteCount: 692,
            RefusalCount: 74,
            InaccessibleCount: 41,
            OtherCompleteCount: 77,
            OfficeHoldCount: 126,
            ActiveNotCheckedInCount: 270,
            MailReceivedCount: 214,
            CawiReceivedCount: 438,
            CapiReceivedCount: 146,
            ReadiReceivedCount: 58,
            OtherModeReceivedCount: 28);

    private static double GetPercent(int count, int total)
        => total <= 0 ? 0 : count * 100d / total;

    private static List<PieSlice> BuildPieSlices(IEnumerable<CountSlice> items)
    {
        var materializedItems = items.ToList();
        var total = materializedItems.Sum(item => Math.Max(item.Count, 0));

        return materializedItems
            .Select((item, index) =>
            {
                var percent = GetPercent(item.Count, total);
                return new PieSlice(
                    item.Label,
                    item.Count,
                    percent,
                    item.Count > 0,
                    PiePalette[index % PiePalette.Length]);
            })
            .ToList();
    }

    private static void CustomizePiePoint(ChartSeriesPointCustomizationSettings settings)
    {
        if (settings.Point.DataItems.OfType<PieSlice>().FirstOrDefault() is not { } slice)
        {
            settings.PointLabel.Visible = false;
            return;
        }

        settings.PointAppearance.Color = slice.Color;
        settings.PointLabel.Visible = slice.ShowCallout;
    }

    private sealed record CountSlice(string Label, int Count);
    private sealed record PieSlice(string Label, int Count, double Percent, bool ShowCallout, Color Color);
}
