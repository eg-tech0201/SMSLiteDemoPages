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

public partial class RecordDetails
{
    [Parameter] public int? RecordId { get; set; }

    private static IReadOnlyList<FieldGridRow> Fields { get; } = Array.Empty<FieldGridRow>();
    private static IReadOnlyList<AuditGridRow> AuditRows { get; } = Array.Empty<AuditGridRow>();

    private static string DisplayText(int? value) => value?.ToString() ?? "—";

    private sealed record FieldGridRow(string Field, string Value);
    private sealed record AuditGridRow(DateTime? EventDate, string Action, string User);
}
