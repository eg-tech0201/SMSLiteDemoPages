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

public partial class SurveyDocumentViewer
{
    [SupplyParameterFromQuery(Name = "title")] public string? Title { get; set; }
    [SupplyParameterFromQuery(Name = "mode")] public string? Mode { get; set; }
    [SupplyParameterFromQuery(Name = "version")] public string? Version { get; set; }
    [SupplyParameterFromQuery(Name = "format")] public string? Format { get; set; }
    [SupplyParameterFromQuery(Name = "documentId")] public string? DocumentId { get; set; }
    [SupplyParameterFromQuery(Name = "source")] public string? Source { get; set; }
    [SupplyParameterFromQuery(Name = "fileName")] public string? FileName { get; set; }
    [SupplyParameterFromQuery(Name = "materialType")] public string? MaterialType { get; set; }
    [SupplyParameterFromQuery(Name = "skey")] public string? SKey { get; set; }
    [SupplyParameterFromQuery(Name = "publishedState")] public int? PublishedState { get; set; }
    [SupplyParameterFromQuery(Name = "outputType")] public int? OutputType { get; set; }

    private bool IsLoading { get; set; } = true;
    private string DocumentPreview { get; set; } = string.Empty;

    private string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "Survey Document" : Title;
    private string DisplayFileName => IsSurveyReviewDocument
        ? $"questionnaire-{SKey}-{GetOutputName(EffectiveOutputType).Replace(' ', '-').ToLowerInvariant()}.{GetOutputExtension(EffectiveOutputType)}"
        : string.IsNullOrWhiteSpace(FileName) ? $"{DisplayTitle.Replace(' ', '-')}.{(Format ?? "pdf").ToLowerInvariant()}" : FileName;
    private string DisplaySubtitle => $"{MaterialType ?? "Questionnaire"} • {Mode ?? "EDR"} • {Version ?? "v1.0"}";
    private bool IsSurveyReviewDocument => !string.IsNullOrWhiteSpace(SKey);
    private int EffectivePublishedState => PublishedState.GetValueOrDefault(400);
    private int EffectiveOutputType => OutputType.GetValueOrDefault(2);
    private bool IsPdfOutput => EffectiveOutputType is 2 or 4;
    private string SurveyReviewDocumentUrl => BuildSurveyReviewOutputUrl(EffectiveOutputType);

    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;
        await Task.Delay(IsSurveyReviewDocument ? 50 : 350);
        DocumentPreview = BuildDocumentPreview();
        IsLoading = false;
    }

    private string BuildSurveyReviewOutputUrl(int outputType)
    {
        if (string.IsNullOrWhiteSpace(SKey))
            return string.Empty;

        return $"https://capitrainingbeta.nass.usda.gov/services/getinstanceoutput/{Uri.EscapeDataString(SKey)}/{EffectivePublishedState}/{outputType}";
    }

    private string BuildDocumentPreview()
    {
        if (IsSurveyReviewDocument)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Survey Review Questionnaire",
                string.Empty,
                $"SKey: {SKey}",
                $"Published State: {EffectivePublishedState}",
                $"Output Type: {EffectiveOutputType} ({GetOutputName(EffectiveOutputType)})",
                $"Source: {SurveyReviewDocumentUrl}",
                string.Empty,
                "Use External Actions to open RTF, PDF, JSON, or booklet output from Survey Review."
            });
        }

        return string.Join(Environment.NewLine, new[]
        {
            "USDA Survey Management System",
            "Document Viewer",
            string.Empty,
            $"Title: {DisplayTitle}",
            $"Type: {MaterialType ?? "Questionnaire"}",
            $"Collection Mode: {Mode ?? "EDR"}",
            $"Version: {Version ?? "v1.0"}",
            $"Format: {Format ?? "PDF"}",
            $"Document Id: {DisplayValue.Text(DocumentId)}",
            string.Empty,
            "The source file content is not yet wired to SMS Lite.",
            "Replace this placeholder with the real Survey Review payload or browser-native/PDF viewer integration."
        });
    }

    private async Task DownloadAsync()
    {
        await Js.InvokeVoidAsync("smsDownloadText", DisplayFileName, DocumentPreview, "text/plain;charset=utf-8");
        Logger.LogInformation(
            "Downloaded {MaterialType} document {DocumentId} as {FileName}.",
            MaterialType ?? "Questionnaire",
            DocumentId ?? DisplayTitle,
            DisplayFileName);
    }

    private static string GetOutputName(int outputType)
        => outputType switch
        {
            1 => "RTF",
            2 => "PDF",
            3 => "JSON",
            4 => "PDF Booklet",
            _ => "Output"
        };

    private static string GetOutputExtension(int outputType)
        => outputType switch
        {
            1 => "rtf",
            2 => "pdf",
            3 => "json",
            4 => "pdf",
            _ => "bin"
        };
}
