using External.ELMA.Client.Configuration;
using Microsoft.Extensions.Options;

namespace External.ELMA.Client.Services;

public sealed class InMemoryElmaCircuitBreaker : IElmaCircuitBreaker
{
    private readonly object _sync = new();
    private readonly IOptionsMonitor<ElmaClientOptions> _options;
    private int _consecutiveFailures;
    private DateTimeOffset? _openUntilUtc;

    public InMemoryElmaCircuitBreaker(IOptionsMonitor<ElmaClientOptions> options)
    {
        _options = options;
    }

    public bool IsOpen()
    {
        lock (_sync)
        {
            if (_openUntilUtc is null)
                return false;

            if (_openUntilUtc.Value > DateTimeOffset.UtcNow)
                return true;

            _openUntilUtc = null;
            _consecutiveFailures = 0;
            return false;
        }
    }

    public void RecordSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            _openUntilUtc = null;
        }
    }

    public void RecordFailure()
    {
        var options = _options.CurrentValue;

        lock (_sync)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= Math.Max(1, options.CircuitBreakerFailureThreshold))
            {
                _openUntilUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, options.CircuitBreakerBreakSeconds));
            }
        }
    }
}
