namespace SMSLiteUI.Services;

public sealed class NiceCacheOptions
{
    public const string SectionName = "CacheApi";

    public string BaseUri { get; set; } = "";
    public string ApiKeyHeaderName { get; set; } = "x-api-header";
    public string ApiKey { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string RecentSurveysCategory { get; set; } = "SMSLite.RecentSurveys";
    public string RecentSurveysKeyPrefix { get; set; } = "recent-surveys";
    public string GetItemPathTemplate { get; set; } = "/v1/Caching/Redis/GetItemsFromCache/{category}/{key}/{accessKey}";
    public string CreateOrUpdateItemPath { get; set; } = "/v1/Caching/Redis/CreateCacheItem";
    public string DeleteItemPathTemplate { get; set; } = "/v1/Caching/Redis/DeleteItemFromCache/{category}/{key}/{accessKey}";
    public int AbsoluteExpiration { get; set; } = 60;
    public int SlidingExpiration { get; set; } = 10;
    public bool Encrypted { get; set; } = true;
}
