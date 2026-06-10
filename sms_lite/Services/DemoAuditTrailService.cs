namespace sms_lite.Services;

public sealed class DemoAuditTrailService
{
    private readonly List<AuditTrailEntry> _entries = [];

    public IReadOnlyList<AuditTrailEntry> Entries => _entries
        .OrderByDescending(e => e.Timestamp)
        .ToList();

    public void Log(string action, string artifactType, string artifactId, string fileName, string performedBy, string details)
    {
        _entries.Add(new AuditTrailEntry(
            DateTimeOffset.Now,
            action,
            artifactType,
            artifactId,
            fileName,
            performedBy,
            details));
    }
}

public sealed record AuditTrailEntry(
    DateTimeOffset Timestamp,
    string Action,
    string ArtifactType,
    string ArtifactId,
    string FileName,
    string PerformedBy,
    string Details
);
