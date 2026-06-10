namespace SMSLiteStaticDemo.Server.Logging;

public interface IUserFileLogger
{
    Task LogAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default);
}
