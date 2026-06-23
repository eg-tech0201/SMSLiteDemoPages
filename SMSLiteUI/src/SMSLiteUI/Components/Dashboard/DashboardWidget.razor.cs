using Microsoft.AspNetCore.Components;

namespace SMSLiteUI.Components.Dashboard;

public partial class DashboardWidget
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string CssClass { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
