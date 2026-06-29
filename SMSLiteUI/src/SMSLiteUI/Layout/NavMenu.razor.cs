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

public partial class NavMenu
{
    [Inject] private UserStateService UserStateService { get; set; } = default!;

    private bool ShowNotifications { get; set; }
    private bool ShowUserMenu { get; set; }
    private string GlobalSearchText { get; set; } = string.Empty;
    private UserState CurrentUser { get; set; } = new(false, null, null, null, []);

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await UserStateService.GetCurrentUserAsync();
    }

    private void ToggleNotifications()
    {
        ShowNotifications = !ShowNotifications;
        ShowUserMenu = false;
    }

    private void ToggleUserMenu()
    {
        ShowUserMenu = !ShowUserMenu;
        ShowNotifications = false;
    }

    private void CloseMenus()
    {
        ShowNotifications = false;
        ShowUserMenu = false;
    }

    private void SubmitGlobalSearch()
    {
        var target = string.IsNullOrWhiteSpace(GlobalSearchText)
            ? "search"
            : $"search?q={Uri.EscapeDataString(GlobalSearchText.Trim())}";
        Navigation.NavigateTo(target);
    }
}
