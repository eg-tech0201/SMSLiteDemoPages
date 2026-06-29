namespace SMSLiteUI.Services;

public interface INiceAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}
