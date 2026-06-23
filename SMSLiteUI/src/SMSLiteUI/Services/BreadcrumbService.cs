using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace SMSLiteUI.Services;

public sealed class BreadcrumbService : IDisposable
{
    private readonly NavigationManager _navigation;

    public BreadcrumbService(NavigationManager navigation)
    {
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
        Items = Build(_navigation.Uri);
    }

    public IReadOnlyList<BreadcrumbItem> Items { get; private set; }

    public event Action? Changed;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        Items = Build(args.Location);
        Changed?.Invoke();
    }

    private IReadOnlyList<BreadcrumbItem> Build(string location)
    {
        var uri = _navigation.ToAbsoluteUri(location);
        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        var query = QueryHelpers.ParseQuery(uri.Query);
        var resultsUrl = LocalUrl(QueryValue(query, "resultsUrl"), "/surveys/results");
        var surveyId = QueryValue(query, "surveyId");
        var sampleId = QueryValue(query, "sampleId");
        var referenceDate = QueryValue(query, "referenceDate");
        var surveyTitle = QueryValue(query, "surveyTitle");
        var sKey = QueryValue(query, "skey");
        var poid = QueryValue(query, "poid");

        return path switch
        {
            "" => [],
            "/surveys" =>
            [
                Current("Surveys")
            ],
            "/surveys/results" =>
            [
                Link("Surveys", "/surveys"),
                Current("Survey Results")
            ],
            var detailsPath when detailsPath == "/surveys/details" || detailsPath.StartsWith("/surveys/details/") =>
            [
                Link("Surveys", "/surveys"),
                Link("Survey Results", resultsUrl),
                Current(SurveyLabel(surveyTitle, surveyId))
            ],
            var recordPath when recordPath == "/surveys/record" || recordPath.StartsWith("/surveys/record/") =>
            [
                Link("Surveys", "/surveys"),
                Link("Survey Results", resultsUrl),
                Link(SurveyLabel(surveyTitle, surveyId), BuildSurveyDetailsUrl(
                    surveyId,
                    sampleId,
                    referenceDate,
                    surveyTitle,
                    resultsUrl)),
                Current(string.IsNullOrWhiteSpace(sKey) ? "Survey Record" : $"Record {sKey}")
            ],
            "/respondents/details" =>
            [
                Link("Surveys", "/surveys"),
                Link("Survey Results", resultsUrl),
                Link(SurveyLabel(surveyTitle, surveyId), BuildSurveyDetailsUrl(
                    surveyId,
                    sampleId,
                    referenceDate,
                    surveyTitle,
                    resultsUrl)),
                Link(string.IsNullOrWhiteSpace(sKey) ? "Survey Record" : $"Record {sKey}",
                    BuildSurveyRecordUrl(surveyId, sampleId, referenceDate, surveyTitle, sKey, resultsUrl)),
                Current(string.IsNullOrWhiteSpace(poid) ? "Respondent Details" : $"Respondent {poid}")
            ],
            "/surveys/document-viewer" =>
            [
                Link("Surveys", "/surveys"),
                Link("Survey Results", resultsUrl),
                Current(QueryValue(query, "title") is { Length: > 0 } title ? title : "Document Viewer")
            ],
            "/records" =>
            [
                Current("Records")
            ],
            var recordDetails when recordDetails.StartsWith("/records/details/") =>
            [
                Link("Records", "/records"),
                Current("Record Details")
            ],
            "/search" =>
            [
                Current("Global Search")
            ],
            "/assignments" =>
            [
                Current("Assignments")
            ],
            _ => []
        };
    }

    private static string BuildSurveyDetailsUrl(
        string? surveyId,
        string? sampleId,
        string? referenceDate,
        string? surveyTitle,
        string resultsUrl)
        => QueryHelpers.AddQueryString("/surveys/details", Parameters(
            ("surveyId", surveyId),
            ("sampleId", sampleId),
            ("referenceDate", referenceDate),
            ("surveyTitle", surveyTitle),
            ("resultsUrl", resultsUrl)));

    private static string BuildSurveyRecordUrl(
        string? surveyId,
        string? sampleId,
        string? referenceDate,
        string? surveyTitle,
        string? sKey,
        string resultsUrl)
        => QueryHelpers.AddQueryString("/surveys/record", Parameters(
            ("surveyId", surveyId),
            ("sampleId", sampleId),
            ("referenceDate", referenceDate),
            ("surveyTitle", surveyTitle),
            ("skey", sKey),
            ("resultsUrl", resultsUrl)));

    private static Dictionary<string, string?> Parameters(params (string Name, string? Value)[] values)
        => values
            .Where(value => !string.IsNullOrWhiteSpace(value.Value))
            .ToDictionary(value => value.Name, value => value.Value);

    private static string? QueryValue(
        IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> query,
        string key)
        => query.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

    private static string SurveyLabel(string? title, string? surveyId)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return string.IsNullOrWhiteSpace(surveyId) ? "Survey Details" : $"Survey {surveyId}";
    }

    private static string LocalUrl(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var normalized = value.StartsWith('/') ? value : $"/{value}";
        return normalized.StartsWith("//", StringComparison.Ordinal) ? fallback : normalized;
    }

    private static BreadcrumbItem Link(string text, string url) => new(text, url, false);
    private static BreadcrumbItem Current(string text) => new(text, null, true);

    public void Dispose()
        => _navigation.LocationChanged -= OnLocationChanged;
}

public sealed record BreadcrumbItem(string Text, string? Url, bool IsCurrent);
