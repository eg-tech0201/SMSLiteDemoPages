using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SMSLiteUI.Services;

public sealed class NiceCacheClient(
    HttpClient httpClient,
    IOptionsMonitor<NiceCacheOptions> options,
    INiceAccessTokenProvider accessTokenProvider)
{
    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        var request = await CreateRequestAsync(
            HttpMethod.Get,
            BuildItemUri(currentOptions.GetItemPathTemplate, currentOptions.RecentSurveysCategory, key, currentOptions.AccessKey),
            cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return default;

        using var document = JsonDocument.Parse(content);

        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("value", out var valueElement))
        {
            return DeserializeCacheValue<T>(valueElement);
        }

        return DeserializeCacheValue<T>(document.RootElement);
    }

    public async Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        var request = await CreateRequestAsync(HttpMethod.Post, currentOptions.CreateOrUpdateItemPath, cancellationToken);
        request.Content = JsonContent.Create(new NiceCacheSetItemRequest(
            Category: currentOptions.RecentSurveysCategory,
            Key: key,
            Value: JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            SlidingExpiration: currentOptions.SlidingExpiration,
            AbsoluteExpiration: currentOptions.AbsoluteExpiration,
            AccessKey: currentOptions.AccessKey,
            Encrypted: currentOptions.Encrypted));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteItemAsync(string key, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        var request = await CreateRequestAsync(
            HttpMethod.Delete,
            BuildItemUri(currentOptions.DeleteItemPathTemplate, currentOptions.RecentSurveysCategory, key, currentOptions.AccessKey),
            cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(currentOptions.BaseUri))
            throw new InvalidOperationException($"{NiceCacheOptions.SectionName}:BaseUri is required before using NICE Cache.");

        if (string.IsNullOrWhiteSpace(currentOptions.AccessKey))
            throw new InvalidOperationException($"{NiceCacheOptions.SectionName}:AccessKey is required before using NICE Cache.");

        var request = new HttpRequestMessage(method, new Uri(new Uri(currentOptions.BaseUri), path));

        if (!string.IsNullOrWhiteSpace(currentOptions.ApiKey))
            request.Headers.TryAddWithoutValidation(currentOptions.ApiKeyHeaderName, currentOptions.ApiKey);

        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return request;
    }

    private static string BuildItemUri(string template, string category, string key, string accessKey)
        => template
            .Replace("{category}", Uri.EscapeDataString(category), StringComparison.OrdinalIgnoreCase)
            .Replace("{key}", Uri.EscapeDataString(key), StringComparison.OrdinalIgnoreCase)
            .Replace("{accessKey}", Uri.EscapeDataString(accessKey), StringComparison.OrdinalIgnoreCase);

    private static T? DeserializeCacheValue<T>(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? default
                : JsonSerializer.Deserialize<T>(value, JsonSerializerOptions.Web);
        }

        return element.Deserialize<T>(JsonSerializerOptions.Web);
    }

    private sealed record NiceCacheSetItemRequest(
        string Category,
        string Key,
        string Value,
        int SlidingExpiration,
        int AbsoluteExpiration,
        string AccessKey,
        bool Encrypted);
}
