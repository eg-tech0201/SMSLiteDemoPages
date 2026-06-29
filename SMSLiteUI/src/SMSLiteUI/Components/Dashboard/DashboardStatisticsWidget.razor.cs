namespace SMSLiteUI.Components.Dashboard;

public partial class DashboardStatisticsWidget
{
    private static readonly IReadOnlyList<DashboardTile> DashboardTiles =
    [
        new("Active Surveys", "0", "kpi-green", "metric-title-large"),
        new("Records Available for Enumeration (CAPI & CATI)", "0", "kpi-blue", "metric-title-small"),
        new("SMS Records Checked-In Yesterday (All Modes)", "0", "kpi-gold", "metric-title-medium"),
        new("SMS Records Checked-In Last 7 Days", "0", "kpi-slate", "metric-title-medium")
    ];

    private sealed record DashboardTile(string Title, string Value, string CssClass, string LabelSizeClass);
}
