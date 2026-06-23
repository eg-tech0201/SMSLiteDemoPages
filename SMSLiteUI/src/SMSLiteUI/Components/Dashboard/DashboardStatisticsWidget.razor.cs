namespace SMSLiteUI.Components.Dashboard;

public partial class DashboardStatisticsWidget
{
    private static readonly IReadOnlyList<DashboardTile> DashboardTiles =
    [
        new("Active Surveys", "0", "bi bi-clipboard-data", "kpi-green"),
        new("Records Available for Enumeration (CAPI & CATI)", "0", "bi bi-people", "kpi-blue"),
        new("SMS Records Checked-In Yesterday (All Modes)", "0", "bi bi-calendar-check", "kpi-gold"),
        new("SMS Records Checked-In Last 7 Days", "0", "bi bi-graph-up-arrow", "kpi-slate")
    ];

    private sealed record DashboardTile(string Title, string Value, string IconCss, string CssClass);
}
