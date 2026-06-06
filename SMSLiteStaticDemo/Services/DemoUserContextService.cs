namespace SMSLiteStaticDemo.Services;

public sealed class DemoUserContextService
{
    public string CurrentRole { get; private set; } = "Survey Manager";

    public bool CanExport => CurrentRole.Equals("SMS Administrator", StringComparison.OrdinalIgnoreCase);

    public void SetRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return;

        CurrentRole = role.Trim();
    }
}
