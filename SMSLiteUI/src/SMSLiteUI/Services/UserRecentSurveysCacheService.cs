using Microsoft.Extensions.Options;

namespace SMSLiteUI.Services;

public sealed class UserRecentSurveysCacheService(
    NiceCacheClient cacheClient,
    UserStateService userStateService,
    IOptionsMonitor<NiceCacheOptions> options)
{
    private const int MaxRecentSurveys = 10;

    public async Task<IReadOnlyList<UserRecentSurveyCacheItem>> GetRecentSurveysAsync(CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync();
        if (user?.UserKey is null)
            return [];

        return await cacheClient.GetItemAsync<List<UserRecentSurveyCacheItem>>(
            BuildCacheKey(user.UserKey),
            cancellationToken) ?? [];
    }

    public async Task MarkOpenedAsync(
        int surveyId,
        string sampleId,
        DateTime referenceDate,
        string? surveyTitle,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync();
        if (user?.UserKey is null)
            return;

        var cacheKey = BuildCacheKey(user.UserKey);
        var recentSurveys = await cacheClient.GetItemAsync<List<UserRecentSurveyCacheItem>>(cacheKey, cancellationToken) ?? [];
        var opened = new UserRecentSurveyCacheItem(
            surveyId,
            sampleId,
            referenceDate.Date,
            surveyTitle,
            DateTime.UtcNow);

        var updated = recentSurveys
            .Where(item => item.SurveyId != opened.SurveyId ||
                           !string.Equals(item.SampleId, opened.SampleId, StringComparison.OrdinalIgnoreCase) ||
                           item.ReferenceDate.Date != opened.ReferenceDate.Date)
            .Prepend(opened)
            .OrderByDescending(item => item.LastOpenedAtUtc)
            .Take(MaxRecentSurveys)
            .ToList();

        await cacheClient.SetItemAsync(cacheKey, updated, cancellationToken);
    }

    public async Task ClearRecentSurveysAsync(CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync();
        if (user?.UserKey is null)
            return;

        await cacheClient.DeleteItemAsync(BuildCacheKey(user.UserKey), cancellationToken);
    }

    private async Task<UserState?> GetAuthenticatedUserAsync()
    {
        var user = await userStateService.GetCurrentUserAsync();
        return user.IsAuthenticated ? user : null;
    }

    private string BuildCacheKey(string userKey)
        => $"{options.CurrentValue.RecentSurveysKeyPrefix}:{NormalizeKeyPart(userKey)}";

    private static string NormalizeKeyPart(string value)
        => Uri.EscapeDataString(value.Trim().ToLowerInvariant());
}
