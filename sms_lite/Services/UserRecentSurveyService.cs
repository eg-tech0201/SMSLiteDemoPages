namespace SMSLiteStaticDemo.Services;

public sealed class UserRecentSurveyService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<RecentSurveyActivity>> _activityByUser = new(StringComparer.OrdinalIgnoreCase);

    public void Track(string userId, DateTime referenceDate, int surveyId, string sampleId, string action)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sampleId))
            return;

        lock (_sync)
        {
            if (!_activityByUser.TryGetValue(userId, out var activities))
            {
                activities = new List<RecentSurveyActivity>();
                _activityByUser[userId] = activities;
            }

            var existingIndex = activities.FindIndex(a =>
                a.ReferenceDate.Date == referenceDate.Date &&
                a.SurveyId == surveyId &&
                a.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));

            var updated = new RecentSurveyActivity(
                referenceDate.Date,
                surveyId,
                sampleId,
                action,
                DateTime.UtcNow);

            if (existingIndex >= 0)
            {
                activities[existingIndex] = updated;
            }
            else
            {
                activities.Add(updated);
            }

            activities.Sort((a, b) => b.LastTouchedUtc.CompareTo(a.LastTouchedUtc));
            if (activities.Count > 100)
            {
                activities.RemoveRange(100, activities.Count - 100);
            }
        }
    }

    public IReadOnlyList<SurveyInstance> GetRecentInstances(string userId, SurveyInstanceService surveyInstanceService, int max = 10)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<SurveyInstance>();

        List<RecentSurveyActivity> snapshot;
        lock (_sync)
        {
            if (!_activityByUser.TryGetValue(userId, out var activities) || activities.Count == 0)
                return Array.Empty<SurveyInstance>();

            snapshot = activities
                .OrderByDescending(a => a.LastTouchedUtc)
                .Take(Math.Max(1, max))
                .ToList();
        }

        var byKey = surveyInstanceService.Instances.ToDictionary(
            i => BuildKey(i.ReferenceDate, i.SurveyId, i.SampleId),
            i => i,
            StringComparer.OrdinalIgnoreCase);

        var rows = new List<SurveyInstance>();
        foreach (var item in snapshot)
        {
            if (byKey.TryGetValue(BuildKey(item.ReferenceDate, item.SurveyId, item.SampleId), out var instance))
            {
                rows.Add(instance);
            }
        }

        return rows;
    }

    private static string BuildKey(DateTime referenceDate, int surveyId, string sampleId)
        => $"{referenceDate:yyyy-MM-dd}|{surveyId}|{sampleId}";
}

public sealed record RecentSurveyActivity(
    DateTime ReferenceDate,
    int SurveyId,
    string SampleId,
    string Action,
    DateTime LastTouchedUtc
);
