using System.Net.Http.Json;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Components.Shared;

public partial class AppBreadcrumbs
{
    protected override void OnInitialized()
        => Breadcrumbs.Changed += OnBreadcrumbsChanged;

    private void OnBreadcrumbsChanged()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
        => Breadcrumbs.Changed -= OnBreadcrumbsChanged;
}
