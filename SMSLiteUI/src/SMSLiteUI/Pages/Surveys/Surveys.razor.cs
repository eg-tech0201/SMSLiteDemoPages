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

public partial class Surveys
{
    [SupplyParameterFromQuery(Name = "q")] public string? SearchQuery { get; set; }
    [SupplyParameterFromQuery(Name = "page")] public int? PageQuery { get; set; }
    [SupplyParameterFromQuery(Name = "pageSize")] public int? PageSizeQuery { get; set; }
    [SupplyParameterFromQuery(Name = "size")] public string? LegacyPageSizeQuery { get; set; }
    [SupplyParameterFromQuery(Name = "surveyDate")] public DateTime? SurveyDateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "referenceDate")] public DateTime? ReferenceDateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "start")] public DateTime? StartDateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "stop")] public DateTime? StopDateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "mode")] public string? ModeQuery { get; set; }
    [SupplyParameterFromQuery(Name = "hqReview")] public bool? HqReviewQuery { get; set; }
    [SupplyParameterFromQuery(Name = "state")] public string? StateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public string? StatusQuery { get; set; }
    [SupplyParameterFromQuery(Name = "viewAll")] public bool? ViewAllQuery { get; set; }

    private const string SampleNameColumn = "SampleName";
    private const string ElmoSurveyIdColumn = "ElmoSurveyId";
    private const string OmbNumberColumn = "OmbNumber";
    private const string SurveyGridLayoutStorageKey = "sms.surveys.results.dxgrid.layout.v4";

    private IGrid? SurveyGrid { get; set; }
    private List<SurveyGridRow> Rows { get; set; } = new();
    private int[] PageSizeOptions { get; } = [10, 20, 50, 100];
    private int GridPageSize { get; set; } = 20;
    private int PageIndex { get; set; }
    private int TotalRowCount { get; set; }
    private string? SurveySearch { get; set; }
    private DateTime? SurveyDateFilter { get; set; }
    private DateTime? StartDateFilter { get; set; }
    private DateTime? StopDateFilter { get; set; }
    private bool MailFilter { get; set; }
    private bool CawiFilter { get; set; }
    private bool CatiFilter { get; set; }
    private bool CapiFilter { get; set; }
    private bool HqReviewFilter { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsGridInteractionLoading { get; set; }
    private string? LoadError { get; set; }
    private int TotalPageCount => TotalRowCount == 0
        ? 1
        : Math.Max(1, (int)Math.Ceiling(TotalRowCount / (double)GridPageSize));
    private string ResultsSummary => TotalRowCount == 0
        ? string.Empty
        : $"Showing {(PageIndex * GridPageSize) + 1:N0}-{Math.Min((PageIndex + 1) * GridPageSize, TotalRowCount):N0} of {TotalRowCount:N0} survey results";
    private static readonly IReadOnlyList<GridColumnSpec> SurveyColumns =
    [
        new(nameof(SurveyGridRow.SurveyId), "Survey ID", 120, null, true),
        new(nameof(SurveyGridRow.SurveyTitle), "Survey Title", 240, null, true),
        new(nameof(SurveyGridRow.SurveyDate), "Survey Date", 140, "MM/dd/yyyy", true),
        new(nameof(SurveyGridRow.SampleId), "Sample ID", 140, null, true),
        new(nameof(SurveyGridRow.SampleName), "Sample Name", 180, null, true),
        new(nameof(SurveyGridRow.ElmoSurveyId), "ELMO Survey ID", 170, null, true),
        new(nameof(SurveyGridRow.OmbNumber), "OMB Number", 160, null, true),
        new(nameof(SurveyGridRow.PeriodId), "Period ID", 120),
        new(nameof(SurveyGridRow.StartDate), "Start Date", 140, "MM/dd/yyyy"),
        new(nameof(SurveyGridRow.StopDate), "Stop Date", 140, "MM/dd/yyyy"),
        new(nameof(SurveyGridRow.HqReviewFlag), "In HQ Review", 150),
        new(nameof(SurveyGridRow.MailFlag), "Mail", 100),
        new(nameof(SurveyGridRow.CawiFlag), "CAWI", 100),
        new(nameof(SurveyGridRow.CapiFlag), "CAPI", 100),
        new(nameof(SurveyGridRow.CatiFlag), "CATI", 100),
        new(nameof(SurveyGridRow.FrequencyDescription), "Frequency", 140),
        new(nameof(SurveyGridRow.ProjectCode), "Project Code", 140),
        new(nameof(SurveyGridRow.MarkedVersion), "Version", 120),
        new(nameof(SurveyGridRow.PopulatedRows), "Populated Rows", 150)
    ];

    protected override async Task OnInitializedAsync()
    {
        SurveySearch = SearchQuery;
        PageIndex = Math.Max(0, (PageQuery ?? 1) - 1);
        var requestedPageSize = PageSizeQuery;
        if (!requestedPageSize.HasValue && int.TryParse(LegacyPageSizeQuery, out var legacyPageSize))
            requestedPageSize = legacyPageSize;
        if (requestedPageSize.HasValue && PageSizeOptions.Contains(requestedPageSize.Value))
            GridPageSize = requestedPageSize.Value;
        SurveyDateFilter = SurveyDateQuery ?? ReferenceDateQuery;
        StartDateFilter = StartDateQuery;
        StopDateFilter = StopDateQuery;
        var modes = (ModeQuery ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        MailFilter = modes.Contains("Mail");
        CawiFilter = modes.Contains("CAWI");
        CatiFilter = modes.Contains("CATI");
        CapiFilter = modes.Contains("CAPI");
        HqReviewFilter = HqReviewQuery == true;

        await LoadSurveyPageAsync(showOverlay: false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || SurveyGrid is null)
            return;

        try
        {
            var layout = await Js.InvokeAsync<GridPersistentLayout?>("smsLoadJson", SurveyGridLayoutStorageKey);
            if (layout is not null)
            {
                SurveyGrid.LoadLayout(layout);
            }
        }
        catch (InvalidOperationException)
        {
            // JS interop is unavailable during prerender/static rendering.
        }
    }

    private void ShowColumnChooser()
    {
        if (SurveyGrid is not null)
        {
            SurveyGrid.ShowColumnChooser("#survey-column-chooser-button");
        }
    }

    private async Task LoadSurveyPageAsync(bool showOverlay = true)
    {
        if (showOverlay)
        {
            IsGridInteractionLoading = true;
            StateHasChanged();
        }

        try
        {
            LoadError = null;
            var page = await Http.GetFromJsonAsync<SurveyInstancePageResponse>(BuildSurveyPageEndpoint());
            Rows = page?.Rows.Take(GridPageSize).ToList() ?? [];
            TotalRowCount = page?.TotalRowCount ?? 0;
        }
        catch (Exception ex)
        {
            Rows = [];
            TotalRowCount = 0;
            LoadError = $"Survey results could not be loaded from the server endpoint. {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsGridInteractionLoading = false;
        }
    }

    private string BuildSurveyPageEndpoint()
    {
        var query = new List<string>
        {
            $"rowLimit={GridPageSize}",
            $"rowOffset={PageIndex * GridPageSize}"
        };

        if (!string.IsNullOrWhiteSpace(SurveySearch))
            query.Add($"surveySearch={Uri.EscapeDataString(SurveySearch.Trim())}");
        if (SurveyDateFilter.HasValue)
            query.Add($"surveyDate={SurveyDateFilter.Value:yyyy-MM-dd}");
        if (StartDateFilter.HasValue)
            query.Add($"startDate={StartDateFilter.Value:yyyy-MM-dd}");
        if (StopDateFilter.HasValue)
            query.Add($"stopDate={StopDateFilter.Value:yyyy-MM-dd}");
        if (MailFilter)
            query.Add("mail=true");
        if (CawiFilter)
            query.Add("cawi=true");
        if (CatiFilter)
            query.Add("cati=true");
        if (CapiFilter)
            query.Add("capi=true");
        if (HqReviewFilter)
            query.Add("hqReview=true");

        return $"api/survey-instances/page?{string.Join("&", query)}";
    }

    private async Task ReloadFromFirstPageAsync(bool syncUrl = true)
    {
        PageIndex = 0;
        if (syncUrl)
            SyncResultsUrl();
        await LoadSurveyPageAsync();
    }

    private async Task OnSurveySearchChanged(string value)
    {
        SurveySearch = value;
        await ReloadFromFirstPageAsync(syncUrl: false);
    }

    private async Task OnSurveyDateFilterChanged(DateTime? value)
    {
        SurveyDateFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnStartDateFilterChanged(DateTime? value)
    {
        StartDateFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnStopDateFilterChanged(DateTime? value)
    {
        StopDateFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnMailFilterChanged(bool value)
    {
        MailFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnCawiFilterChanged(bool value)
    {
        CawiFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnCatiFilterChanged(bool value)
    {
        CatiFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnCapiFilterChanged(bool value)
    {
        CapiFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnHqReviewFilterChanged(bool value)
    {
        HqReviewFilter = value;
        await ReloadFromFirstPageAsync();
    }

    private async Task ClearServerFiltersAsync()
    {
        SurveySearch = null;
        SurveyDateFilter = null;
        StartDateFilter = null;
        StopDateFilter = null;
        MailFilter = false;
        CawiFilter = false;
        CatiFilter = false;
        CapiFilter = false;
        HqReviewFilter = false;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnGridPageIndexChanged(int pageIndex)
    {
        var boundedPageIndex = Math.Clamp(pageIndex, 0, TotalPageCount - 1);
        if (boundedPageIndex == PageIndex)
            return;

        PageIndex = boundedPageIndex;
        SyncResultsUrl();
        await LoadSurveyPageAsync();
    }

    private async Task OnGridPageSizeChanged(int pageSize)
    {
        if (!PageSizeOptions.Contains(pageSize) || pageSize == GridPageSize)
            return;

        GridPageSize = pageSize;
        await ReloadFromFirstPageAsync();
    }

    private async Task SaveGridViewAsync()
    {
        if (SurveyGrid is null)
            return;

        var layout = SurveyGrid.SaveLayout();
        await Js.InvokeVoidAsync("smsSaveJson", SurveyGridLayoutStorageKey, layout);
    }

    private async Task ResetGridViewAsync()
    {
        await Js.InvokeVoidAsync("smsRemoveStorage", SurveyGridLayoutStorageKey);
        if (SurveyGrid is not null)
        {
            SurveyGrid.LoadLayout(new GridPersistentLayout());
        }
    }

    private static string? ResolveDisplayFormat(GridColumnSpec column)
    {
        if (!string.IsNullOrWhiteSpace(column.DisplayFormat))
            return column.DisplayFormat;

        var property = typeof(SurveyGridRow).GetProperty(column.FieldName);
        var propertyType = Nullable.GetUnderlyingType(property?.PropertyType ?? typeof(string))
            ?? property?.PropertyType;

        if (propertyType is null)
            return null;

        if (propertyType == typeof(decimal) || propertyType == typeof(double) || propertyType == typeof(float))
            return "0.################";

        if (propertyType == typeof(byte) ||
            propertyType == typeof(short) ||
            propertyType == typeof(int) ||
            propertyType == typeof(long) ||
            propertyType == typeof(sbyte) ||
            propertyType == typeof(ushort) ||
            propertyType == typeof(uint) ||
            propertyType == typeof(ulong))
        {
            return "0";
        }

        return null;
    }

    private string BuildDetailHref(SurveyGridRow row)
    {
        if (!row.SurveyId.HasValue || !row.SurveyDate.HasValue || !row.SampleId.HasValue)
            return "surveys/results";

        return $"surveys/details?referenceDate={row.SurveyDate.Value:yyyy-MM-dd}" +
               $"&surveyId={row.SurveyId.Value}" +
               $"&sampleId={Uri.EscapeDataString(row.SampleId.Value.ToString())}" +
               $"&surveyTitle={Uri.EscapeDataString(row.SurveyTitle ?? string.Empty)}" +
               $"&resultsUrl={Uri.EscapeDataString(BuildResultsUrl())}";
    }

    private void OnSurveyRowClick(GridRowClickEventArgs e)
    {
        if (e.Column is DxGridSelectionColumn)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is SurveyGridRow row)
            Navigation.NavigateTo(BuildDetailHref(row));
    }

    private void SyncResultsUrl()
        => Navigation.NavigateTo(BuildResultsUrl(), replace: true);

    private string BuildResultsUrl()
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(SurveySearch))
            query.Add($"q={Uri.EscapeDataString(SurveySearch.Trim())}");
        if (PageIndex > 0)
            query.Add($"page={PageIndex + 1}");
        if (GridPageSize != 20)
            query.Add($"pageSize={GridPageSize}");
        if (SurveyDateFilter.HasValue)
            query.Add($"surveyDate={SurveyDateFilter.Value:yyyy-MM-dd}");
        if (StartDateFilter.HasValue)
            query.Add($"start={StartDateFilter.Value:yyyy-MM-dd}");
        if (StopDateFilter.HasValue)
            query.Add($"stop={StopDateFilter.Value:yyyy-MM-dd}");

        var modes = new List<string>();
        if (MailFilter) modes.Add("Mail");
        if (CawiFilter) modes.Add("CAWI");
        if (CatiFilter) modes.Add("CATI");
        if (CapiFilter) modes.Add("CAPI");
        if (modes.Count > 0)
            query.Add($"mode={Uri.EscapeDataString(string.Join(',', modes))}");
        if (HqReviewFilter)
            query.Add("hqReview=true");
        if (!string.IsNullOrWhiteSpace(StateQuery))
            query.Add($"state={Uri.EscapeDataString(StateQuery)}");
        if (!string.IsNullOrWhiteSpace(StatusQuery))
            query.Add($"status={Uri.EscapeDataString(StatusQuery)}");
        if (ViewAllQuery == true)
            query.Add("viewAll=true");

        return $"/surveys/results{(query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}")}";
    }

    private static string FormatCellValue(SurveyGridRow row, string fieldName)
    {
        var value = typeof(SurveyGridRow).GetProperty(fieldName)?.GetValue(row);
        if (value is DateTime date)
            return date.ToString("MM/dd/yyyy");

        return DisplayValue.Text(value);
    }

    private sealed record GridColumnSpec(string FieldName, string Caption, int MinWidth, string? DisplayFormat = null, bool VisibleByDefault = false);
}
