using System.Globalization;
using System.Text;
using System.Text.Json;
using SMS.Integration.SurveyReview.Configuration;
using Microsoft.Extensions.Options;
using sms_lite.Models.Integrations;

namespace SMS.Integration.SurveyReview.Services;

public sealed class HttpSurveyReviewDownstreamClient : ISurveyReviewDownstreamClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SurveyReviewClientOptions> _options;

    public HttpSurveyReviewDownstreamClient(HttpClient httpClient, IOptionsMonitor<SurveyReviewClientOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<SurveyReviewSKeyInstance>> GetSKeyInstancesAsync(
        int surveyId,
        DateTime surveyDate,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var path = options.SKeyInstancePathTemplate
            .Replace("{surveyId}", surveyId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{surveyDate}", surveyDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        using var response = await _httpClient.GetAsync(BuildUri(options.BaseUrl, path), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SurveyReviewGatewayException("SURVEY_REVIEW_FAILURE", $"Survey Review returned {(int)response.StatusCode} while querying SKey instances.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParseSKeyInstances(document.RootElement);
    }

    public async Task<SurveyReviewOutputPayload?> GetInstanceOutputAsync(
        string sKey,
        int publishedState,
        int outputType,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var path = options.InstanceOutputPathTemplate
            .Replace("{skey}", Uri.EscapeDataString(sKey), StringComparison.OrdinalIgnoreCase)
            .Replace("{publishedState}", publishedState.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{outputType}", outputType.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        var uri = BuildUri(options.TrainingBaseUrl, path);

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SurveyReviewGatewayException("SURVEY_REVIEW_FAILURE", $"Survey Review returned {(int)response.StatusCode} while retrieving instance output.");

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var expectedContentType = SurveyReviewOutputTypes.GetContentType(outputType);

        if (IsBinaryContent(contentType))
            return BuildPayload(sKey, publishedState, outputType, bytes, contentType ?? expectedContentType, null);

        var text = Encoding.UTF8.GetString(bytes);
        if (outputType == 3)
            return BuildPayload(sKey, publishedState, outputType, Encoding.UTF8.GetBytes(text), "application/json", text);

        var extracted = TryExtractBase64Payload(text);
        if (extracted is not null)
            return BuildPayload(sKey, publishedState, outputType, extracted, expectedContentType, null);

        return BuildPayload(sKey, publishedState, outputType, bytes, contentType ?? "application/json", text);
    }

    private static SurveyReviewOutputPayload BuildPayload(
        string sKey,
        int publishedState,
        int outputType,
        byte[] content,
        string contentType,
        string? previewText)
    {
        var outputName = SurveyReviewOutputTypes.GetName(outputType);
        var extension = SurveyReviewOutputTypes.GetExtension(outputType);
        return new SurveyReviewOutputPayload(
            sKey,
            publishedState,
            outputType,
            outputName,
            $"questionnaire-{sKey}-{outputName.Replace(' ', '-').ToLowerInvariant()}.{extension}",
            contentType,
            content,
            null,
            previewText);
    }

    private static IReadOnlyList<SurveyReviewSKeyInstance> ParseSKeyInstances(JsonElement root)
    {
        IEnumerable<JsonElement> items = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().ToList(),
            JsonValueKind.Object when root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array => data.EnumerateArray().ToList(),
            _ => Array.Empty<JsonElement>()
        };

        return items
            .Select(ParseSKeyInstance)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private static SurveyReviewSKeyInstance? ParseSKeyInstance(JsonElement item)
    {
        var sKey = GetString(item, "skey", "sKey", "instanceId", "instance_id", "skeyInstance");
        if (string.IsNullOrWhiteSpace(sKey))
            return null;

        return new SurveyReviewSKeyInstance(
            GetDate(item, DateTime.MinValue, "survey_date", "surveyDate"),
            GetInt(item, 0, "survey_id", "surveyId"),
            GetInt(item, 0, "page_count", "pageCount"),
            sKey,
            GetInt(item, 400, "status_code", "statusCode", "published_state", "publishedState"));
    }

    private static byte[]? TryExtractBase64Payload(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            using var document = JsonDocument.Parse(text);
            return FindBase64Value(document.RootElement);
        }
        catch (JsonException)
        {
            return TryFromBase64(text);
        }
    }

    private static byte[]? FindBase64Value(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return TryFromBase64(element.GetString());

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var value = FindBase64Value(item);
                if (value is not null)
                    return value;
            }
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var preferredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "content",
            "data",
            "output",
            "payload",
            "blob",
            "file",
            "document",
            "instance_output",
            "output_data"
        };

        foreach (var property in element.EnumerateObject())
        {
            if (!preferredNames.Contains(property.Name))
                continue;

            var value = FindBase64Value(property.Value);
            if (value is not null)
                return value;
        }

        foreach (var property in element.EnumerateObject())
        {
            var value = FindBase64Value(property.Value);
            if (value is not null)
                return value;
        }

        return null;
    }

    private static byte[]? TryFromBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        var commaIndex = normalized.IndexOf(',');
        if (normalized.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            normalized = normalized[(commaIndex + 1)..];

        if (normalized.Length < 32)
            return null;

        try
        {
            return Convert.FromBase64String(normalized);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (!item.TryGetProperty(name, out var property))
                continue;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                _ => null
            };
        }

        return null;
    }

    private static int GetInt(JsonElement item, int fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (!item.TryGetProperty(name, out var property))
                continue;

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
                return value;

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
                return value;
        }

        return fallback;
    }

    private static DateTime GetDate(JsonElement item, DateTime fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value))
            {
                return value.Date;
            }
        }

        return fallback;
    }

    private static bool IsBinaryContent(string? contentType)
        => contentType is not null &&
           !contentType.Contains("json", StringComparison.OrdinalIgnoreCase) &&
           !contentType.Contains("text", StringComparison.OrdinalIgnoreCase);

    private static Uri BuildUri(string baseUrl, string path)
        => new(new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/"), path);
}
