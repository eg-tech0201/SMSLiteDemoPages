namespace SMSLiteUI.Services;

public sealed class NoopNiceAccessTokenProvider : INiceAccessTokenProvider
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}
