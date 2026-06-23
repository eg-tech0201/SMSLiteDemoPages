using System.Net.Http.Json;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMSLiteModels.Entities.Dtos;
using SMSLiteModels.Entities;
using SMSLiteUI.Services;

namespace SMSLiteUI.Pages.Surveys;

public partial class SurveyRecord
{
    [Parameter] public int? SurveyId { get; set; }
    [SupplyParameterFromQuery(Name = "surveyId")] public int? SurveyIdQuery { get; set; }
    [SupplyParameterFromQuery(Name = "sampleId")] public string? SampleId { get; set; }
    [SupplyParameterFromQuery(Name = "referenceDate")] public string? ReferenceDate { get; set; }
    [SupplyParameterFromQuery(Name = "skey")] public string? SKey { get; set; }

    private IGrid? RecordGrid { get; set; }
    private string SearchText { get; set; } = string.Empty;
    private static IReadOnlyList<RespondentGridRow> Rows { get; } = Array.Empty<RespondentGridRow>();
    private int? EffectiveSurveyId => SurveyIdQuery ?? SurveyId;

    private void ShowColumnChooserAsync()
    {
        RecordGrid?.ShowColumnChooser();
    }

    private static string DisplayText(object? value)
        => string.IsNullOrWhiteSpace(value?.ToString()) ? "—" : value.ToString()!;

    private sealed record RespondentGridRow(
        string Poid,
        string WholeName,
        string StateId,
        DateTime? SurveyDate,
        string EnumeratorName,
        string Notes);
}
