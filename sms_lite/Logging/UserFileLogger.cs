using Microsoft.Extensions.Options;
using sms_lite.Server.Configuration;
using System.Text;

namespace sms_lite.Server.Logging;

public sealed class UserFileLogger(IOptions<SmsLiteDatabaseOptions> options) : IUserFileLogger
{
    private readonly SmsLiteDatabaseOptions _options = options.Value;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task LogAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var logPath = GetLogPath();
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var entry = new StringBuilder()
            .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))
            .Append(" | ")
            .AppendLine(message);

        if (exception is not null)
        {
            entry.AppendLine(exception.ToString());
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(logPath, entry.ToString(), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string GetLogPath()
    {
        var directory = _options.LogDirectory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            directory = string.IsNullOrWhiteSpace(userProfile)
                ? AppContext.BaseDirectory
                : Path.Combine(userProfile, "SMSLiteLogs");
        }

        return Path.Combine(directory, "smslite-server.log");
    }
}
