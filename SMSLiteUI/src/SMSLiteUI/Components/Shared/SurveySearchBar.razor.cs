using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SMSLiteUI.Components.Shared;

public partial class SurveySearchBar
{
    [Parameter] public string CssClass { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public EventCallback<string> SearchTextChanged { get; set; }
    [Parameter] public EventCallback SearchRequested { get; set; }
    [Parameter] public bool ShowViewAllButton { get; set; }
    [Parameter] public string ViewAllText { get; set; } = "View all";
    [Parameter] public EventCallback ViewAllRequested { get; set; }
    [Parameter] public string Placeholder { get; set; } = "Search by survey ID or title";
    [Parameter] public BindValueMode BindValueMode { get; set; } = BindValueMode.OnDelayedInput;
    [Parameter] public int InputDelay { get; set; } = 450;
    [Parameter] public DateTime? SurveyDate { get; set; }
    [Parameter] public EventCallback<DateTime?> SurveyDateChanged { get; set; }
    [Parameter] public DateTime? StartDate { get; set; }
    [Parameter] public EventCallback<DateTime?> StartDateChanged { get; set; }
    [Parameter] public DateTime? StopDate { get; set; }
    [Parameter] public EventCallback<DateTime?> StopDateChanged { get; set; }
    [Parameter] public bool Mail { get; set; }
    [Parameter] public EventCallback<bool> MailChanged { get; set; }
    [Parameter] public bool Cawi { get; set; }
    [Parameter] public EventCallback<bool> CawiChanged { get; set; }
    [Parameter] public bool Cati { get; set; }
    [Parameter] public EventCallback<bool> CatiChanged { get; set; }
    [Parameter] public bool Capi { get; set; }
    [Parameter] public EventCallback<bool> CapiChanged { get; set; }
    [Parameter] public bool HqReview { get; set; }
    [Parameter] public EventCallback<bool> HqReviewChanged { get; set; }
    [Parameter] public EventCallback ClearFiltersRequested { get; set; }

    private bool IsFiltersMenuOpen { get; set; }
    private ElementReference FiltersAnchor { get; set; }
    private ElementReference FiltersPopup { get; set; }

    private int ActiveFilterCount =>
        (SurveyDate.HasValue ? 1 : 0) +
        (StartDate.HasValue ? 1 : 0) +
        (StopDate.HasValue ? 1 : 0) +
        (Mail ? 1 : 0) +
        (Cawi ? 1 : 0) +
        (Capi ? 1 : 0) +
        (Cati ? 1 : 0) +
        (HqReview ? 1 : 0);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsFiltersMenuOpen)
            await Js.InvokeVoidAsync("smsPositionFilterPopover", FiltersAnchor, FiltersPopup);
    }

    private void ToggleFiltersMenu()
        => IsFiltersMenuOpen = !IsFiltersMenuOpen;

    private void CloseFiltersMenu()
        => IsFiltersMenuOpen = false;

    private async Task OnSearchTextChanged(string value)
    {
        SearchText = value;
        await SearchTextChanged.InvokeAsync(value);
    }

    private async Task SubmitSearchAsync()
        => await SearchRequested.InvokeAsync();

    private async Task SubmitViewAllAsync()
        => await ViewAllRequested.InvokeAsync();

    private async Task OnSurveyDateChanged(DateTime? value)
    {
        SurveyDate = value;
        await SurveyDateChanged.InvokeAsync(value);
    }

    private async Task OnStartDateChanged(DateTime? value)
    {
        StartDate = value;
        await StartDateChanged.InvokeAsync(value);
    }

    private async Task OnStopDateChanged(DateTime? value)
    {
        StopDate = value;
        await StopDateChanged.InvokeAsync(value);
    }

    private async Task OnMailChanged(bool value)
    {
        Mail = value;
        await MailChanged.InvokeAsync(value);
    }

    private async Task OnCawiChanged(bool value)
    {
        Cawi = value;
        await CawiChanged.InvokeAsync(value);
    }

    private async Task OnCatiChanged(bool value)
    {
        Cati = value;
        await CatiChanged.InvokeAsync(value);
    }

    private async Task OnCapiChanged(bool value)
    {
        Capi = value;
        await CapiChanged.InvokeAsync(value);
    }

    private async Task OnHqReviewChanged(bool value)
    {
        HqReview = value;
        await HqReviewChanged.InvokeAsync(value);
    }

    private async Task ClearFiltersAsync()
    {
        SurveyDate = null;
        StartDate = null;
        StopDate = null;
        Mail = false;
        Cawi = false;
        Cati = false;
        Capi = false;
        HqReview = false;
        await ClearFiltersRequested.InvokeAsync();
    }
}
