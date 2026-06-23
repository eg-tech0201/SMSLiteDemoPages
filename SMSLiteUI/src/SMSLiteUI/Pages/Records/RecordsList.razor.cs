using System.Net.Http.Json;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Pages.Records;

public partial class RecordsList
{
    private IGrid? RecordsGrid { get; set; }
    private string SearchText { get; set; } = string.Empty;
    private static IReadOnlyList<RecordGridRow> Rows { get; } = Array.Empty<RecordGridRow>();

    private void ShowColumnChooserAsync()
    {
        RecordsGrid?.ShowColumnChooser();
    }

    private sealed record RecordGridRow(
        int RecordId,
        string RespondentId,
        string County,
        string State,
        string Status,
        DateTime? LastUpdated,
        string Survey);
}
