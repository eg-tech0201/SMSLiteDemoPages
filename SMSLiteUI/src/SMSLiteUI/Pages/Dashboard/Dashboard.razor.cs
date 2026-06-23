using Microsoft.AspNetCore.Components;

namespace SMSLiteUI.Pages.Dashboard;

public partial class Dashboard
{
    private string SearchText { get; set; } = "";
    private DateTime? SurveyDateFilter { get; set; }
    private DateTime? StartDateFilter { get; set; }
    private DateTime? StopDateFilter { get; set; }
    private bool MailFilter { get; set; }
    private bool CawiFilter { get; set; }
    private bool CatiFilter { get; set; }
    private bool CapiFilter { get; set; }
    private bool HqReviewFilter { get; set; }

    private void ClearFilters()
    {
        SurveyDateFilter = null;
        StartDateFilter = null;
        StopDateFilter = null;
        MailFilter = false;
        CawiFilter = false;
        CatiFilter = false;
        CapiFilter = false;
        HqReviewFilter = false;
    }

    private void OnDashboardSearchTextChanged(string value)
        => SearchText = value;

    private void OnSearchSubmit()
    {
        Navigation.NavigateTo($"surveys/results{BuildQuery()}");
    }

    private void ViewAll()
        => Navigation.NavigateTo($"surveys/results{BuildQuery(includeViewAll: true)}");

    private Task OnSurveyDateFilterChanged(DateTime? value) => SetAsync(() => SurveyDateFilter = value);
    private Task OnStartDateFilterChanged(DateTime? value) => SetAsync(() => StartDateFilter = value);
    private Task OnStopDateFilterChanged(DateTime? value) => SetAsync(() => StopDateFilter = value);
    private Task OnMailFilterChanged(bool value) => SetAsync(() => MailFilter = value);
    private Task OnCawiFilterChanged(bool value) => SetAsync(() => CawiFilter = value);
    private Task OnCatiFilterChanged(bool value) => SetAsync(() => CatiFilter = value);
    private Task OnCapiFilterChanged(bool value) => SetAsync(() => CapiFilter = value);
    private Task OnHqReviewFilterChanged(bool value) => SetAsync(() => HqReviewFilter = value);

    private static Task SetAsync(Action setValue)
    {
        setValue();
        return Task.CompletedTask;
    }

    private string BuildQuery(bool includeViewAll = false)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            queryParts.Add($"q={Uri.EscapeDataString(SearchText.Trim())}");
        }

        if (SurveyDateFilter.HasValue)
            queryParts.Add($"surveyDate={SurveyDateFilter.Value:yyyy-MM-dd}");
        if (StartDateFilter.HasValue)
            queryParts.Add($"start={StartDateFilter.Value:yyyy-MM-dd}");
        if (StopDateFilter.HasValue)
            queryParts.Add($"stop={StopDateFilter.Value:yyyy-MM-dd}");

        var modes = BuildModeQuery();
        if (!string.IsNullOrWhiteSpace(modes))
            queryParts.Add($"mode={Uri.EscapeDataString(modes)}");
        if (HqReviewFilter)
            queryParts.Add("hqReview=true");
        if (includeViewAll)
            queryParts.Add("viewAll=true");

        return queryParts.Count == 0 ? "" : $"?{string.Join("&", queryParts)}";
    }

    private string BuildModeQuery()
    {
        var modes = new List<string>();
        if (MailFilter)
            modes.Add("Mail");
        if (CawiFilter)
            modes.Add("CAWI");
        if (CatiFilter)
            modes.Add("CATI");
        if (CapiFilter)
            modes.Add("CAPI");

        return string.Join(",", modes);
    }

}
