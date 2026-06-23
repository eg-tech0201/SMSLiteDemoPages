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

public partial class RespondentDetails
{
    [SupplyParameterFromQuery(Name = "stateId")] public string? StateId { get; set; }
    [SupplyParameterFromQuery(Name = "poid")] public string? Poid { get; set; }
    [SupplyParameterFromQuery(Name = "surveyId")] public string? SurveyId { get; set; }
    [SupplyParameterFromQuery(Name = "sampleId")] public string? SampleId { get; set; }
    [SupplyParameterFromQuery(Name = "referenceDate")] public string? ReferenceDate { get; set; }
    [SupplyParameterFromQuery(Name = "skey")] public string? SKey { get; set; }
    [SupplyParameterFromQuery(Name = "surveyTitle")] public string? SurveyTitle { get; set; }
    [SupplyParameterFromQuery(Name = "resultsUrl")] public string? ResultsUrl { get; set; }

    private static IReadOnlyList<TimelineGridRow> TimelineRows { get; } = Array.Empty<TimelineGridRow>();

    private string BackToRecordUrl =>
        $"surveys/record?referenceDate={Escape(ReferenceDate)}" +
        $"&surveyId={Escape(SurveyId)}" +
        $"&sampleId={Escape(SampleId)}" +
        $"&skey={Escape(SKey)}" +
        $"&surveyTitle={Escape(SurveyTitle)}" +
        $"&resultsUrl={Escape(ResultsUrl ?? "/surveys/results")}";

    private string SurveyDetailsUrl =>
        $"surveys/details?referenceDate={Escape(ReferenceDate)}" +
        $"&surveyId={Escape(SurveyId)}" +
        $"&sampleId={Escape(SampleId)}" +
        $"&surveyTitle={Escape(SurveyTitle)}" +
        $"&resultsUrl={Escape(ResultsUrl ?? "/surveys/results")}";

    private static string Escape(string? value) => Uri.EscapeDataString(value ?? string.Empty);

    private static string DisplayText(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private sealed record TimelineGridRow(
        DateTime? EventDate,
        string EventType,
        string Actor,
        string Summary);
}
