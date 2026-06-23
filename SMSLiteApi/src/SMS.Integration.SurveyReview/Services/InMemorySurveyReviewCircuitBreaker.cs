using SMS.Integration.SurveyReview.Configuration;
using Microsoft.Extensions.Options;

namespace SMS.Integration.SurveyReview.Services;

public sealed class InMemorySurveyReviewCircuitBreaker : ISurveyReviewCircuitBreaker
{
    private readonly IOptionsMonitor<SurveyReviewClientOptions> _options;
    private readonly object _gate = new();
    private int _failureCount;
    private DateTimeOffset? _openedAt;

    public InMemorySurveyReviewCircuitBreaker(IOptionsMonitor<SurveyReviewClientOptions> options)
    {
        _options = options;
    }

    public bool IsOpen()
    {
        lock (_gate)
        {
            if (_openedAt is null)
                return false;

            var breakWindow = TimeSpan.FromSeconds(Math.Max(1, _options.CurrentValue.CircuitBreakerBreakSeconds));
            if (DateTimeOffset.UtcNow - _openedAt.Value < breakWindow)
                return true;

            _openedAt = null;
            _failureCount = 0;
            return false;
        }
    }

    public void RecordSuccess()
    {
        lock (_gate)
        {
            _failureCount = 0;
            _openedAt = null;
        }
    }

    public void RecordFailure()
    {
        lock (_gate)
        {
            _failureCount++;
            if (_failureCount >= Math.Max(1, _options.CurrentValue.CircuitBreakerFailureThreshold))
                _openedAt = DateTimeOffset.UtcNow;
        }
    }
}
