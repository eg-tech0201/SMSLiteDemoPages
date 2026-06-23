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

public partial class LoadingSwirl
{
    [Parameter] public string Text { get; set; } = "Loading";
    [Parameter] public bool FullPage { get; set; }

    private string CssClass => FullPage ? "app-loading-shell" : "grid-loading";
}
