using System.Net.Http.Json;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Layout;

public partial class TopBar
{
    [Parameter] public string Title { get; set; } = "Dashboard";

    private string SearchText { get; set; } = "";
    private bool ShowFilters { get; set; }
    private ElementReference _filtersAnchor;
    private ElementReference _filtersPop;
    private DateTime? SelectedReferenceDate { get; set; }
    private int? ReferenceMonth { get; set; }
    private int? ReferenceDay { get; set; }
    private int? ReferenceYear { get; set; }
    private DateTime? StartDateFilter { get; set; }
    private DateTime? StopDateFilter { get; set; }
    private string ResultsSize { get; set; } = "";

    private readonly List<string> States = new()
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL",
        "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME",
        "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH",
        "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI",
        "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", "PR"
    };
    private readonly List<string> Modes = new() { "CAWI", "CATI", "CAPI", "Paper" };
    private readonly List<string> ResultsSizes = new() { "10", "20", "50", "All" };
    private readonly List<string> StatusOptions = new() { "Not started", "In progress", "Blocked", "Overdue" };

    private HashSet<string> StateSelections { get; } = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> ModeSelections { get; } = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> StatusSelections { get; } = new(StringComparer.OrdinalIgnoreCase);
    private IEnumerable<FilterChip> AppliedFilterChips => BuildFilterChips();
    private int FilterAppliedCount => AppliedFilterChips.Count();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ShowFilters)
            await Js.InvokeVoidAsync("smsPositionFilterPopover", _filtersAnchor, _filtersPop);
    }

    private Task CloseFilters()
    {
        ShowFilters = false;
        return Task.CompletedTask;
    }

    private Task ToggleFilters()
    {
        ShowFilters = !ShowFilters;
        return Task.CompletedTask;
    }

    private void ClearFilters()
    {
        StartDateFilter = null;
        StopDateFilter = null;
        ClearReferenceDateFilter();
        StateSelections.Clear();
        ModeSelections.Clear();
        ResultsSize = "";
        StatusSelections.Clear();
    }

    private IEnumerable<FilterChip> BuildFilterChips()
    {
        var chips = new List<FilterChip>();

        chips.AddRange(StateSelections.Select(s => new FilterChip($"State: {s}", () => StateSelections.Remove(s))));
        chips.AddRange(ModeSelections.Select(m => new FilterChip($"Mode: {m}", () => ModeSelections.Remove(m))));
        chips.AddRange(StatusSelections.Select(s => new FilterChip($"Status: {s}", () => StatusSelections.Remove(s))));

        if (StartDateFilter.HasValue)
            chips.Add(new FilterChip($"Start Date: {StartDateFilter.Value:yyyy-MM-dd}", () => StartDateFilter = null));

        if (StopDateFilter.HasValue)
            chips.Add(new FilterChip($"End Date: {StopDateFilter.Value:yyyy-MM-dd}", () => StopDateFilter = null));

        if (SelectedReferenceDate.HasValue)
            chips.Add(new FilterChip($"Reference Date: {SelectedReferenceDate.Value:yyyy-MM-dd}", ClearReferenceDateFilter));

        if (!string.IsNullOrWhiteSpace(ResultsSize))
            chips.Add(new FilterChip($"Results: {ResultsSize}", () => ResultsSize = ""));

        return chips;
    }

    private sealed record FilterChip(string Label, Action OnRemove);

    private void OnSearchSubmit()
    {
        var includeViewAll = string.IsNullOrWhiteSpace(SearchText);
        Navigation.NavigateTo($"surveys/results{BuildQuery(includeViewAll)}");
    }

    private static void ToggleSelection(HashSet<string> set, string value, ChangeEventArgs e)
    {
        if (e.Value is bool isChecked && isChecked)
        {
            set.Add(value);
        }
        else
        {
            set.Remove(value);
        }
    }

    private IEnumerable<int> ReferenceMonthOptions => Enumerable.Range(1, 12);

    private IEnumerable<int> ReferenceDayOptions
    {
        get
        {
            var year = ReferenceYear ?? DateTime.Today.Year;
            var month = ReferenceMonth ?? 1;
            var maxDays = DateTime.DaysInMonth(year, month);
            return Enumerable.Range(1, maxDays);
        }
    }

    private IEnumerable<int> ReferenceYearOptions =>
        new[] { DateTime.Today.Year + 1, DateTime.Today.Year, DateTime.Today.Year - 1 }
            .OrderByDescending(y => y);

    private Task OnReferenceDatePartChanged()
    {
        if (!ReferenceYear.HasValue || !ReferenceMonth.HasValue || !ReferenceDay.HasValue)
        {
            SelectedReferenceDate = null;
            return Task.CompletedTask;
        }

        var maxDays = DateTime.DaysInMonth(ReferenceYear.Value, ReferenceMonth.Value);
        if (ReferenceDay.Value > maxDays)
        {
            ReferenceDay = maxDays;
        }

        SelectedReferenceDate = new DateTime(ReferenceYear.Value, ReferenceMonth.Value, ReferenceDay.Value);
        return Task.CompletedTask;
    }

    private void ClearReferenceDateFilter()
    {
        SelectedReferenceDate = null;
        ReferenceMonth = null;
        ReferenceDay = null;
        ReferenceYear = null;
    }

    private string BuildQuery(bool includeViewAll)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            queryParts.Add($"q={Uri.EscapeDataString(SearchText.Trim())}");
        }

        if (StartDateFilter.HasValue)
        {
            queryParts.Add($"start={StartDateFilter.Value:yyyy-MM-dd}");
        }

        if (StopDateFilter.HasValue)
        {
            queryParts.Add($"stop={StopDateFilter.Value:yyyy-MM-dd}");
        }

        if (StateSelections.Count > 0)
        {
            var encoded = string.Join(",", StateSelections.Select(Uri.EscapeDataString));
            queryParts.Add($"state={encoded}");
        }

        if (ModeSelections.Count > 0)
        {
            var encoded = string.Join(",", ModeSelections.Select(Uri.EscapeDataString));
            queryParts.Add($"mode={encoded}");
        }

        if (!string.IsNullOrWhiteSpace(ResultsSize))
        {
            queryParts.Add($"size={Uri.EscapeDataString(ResultsSize)}");
        }

        if (StatusSelections.Count > 0)
        {
            var encoded = string.Join(",", StatusSelections.Select(Uri.EscapeDataString));
            queryParts.Add($"status={encoded}");
        }

        if (SelectedReferenceDate.HasValue)
        {
            queryParts.Add($"referenceDate={SelectedReferenceDate.Value:yyyy-MM-dd}");
        }

        if (includeViewAll)
        {
            queryParts.Add("viewAll=true");
        }

        return queryParts.Count == 0 ? "" : $"?{string.Join("&", queryParts)}";
    }
}
