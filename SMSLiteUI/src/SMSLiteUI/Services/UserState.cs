namespace SMSLiteUI.Services;

public sealed record UserState(
    bool IsAuthenticated,
    string? UserKey,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles)
{
    public string DisplayEmail => string.IsNullOrWhiteSpace(Email) ? "Not signed in" : Email;
}
