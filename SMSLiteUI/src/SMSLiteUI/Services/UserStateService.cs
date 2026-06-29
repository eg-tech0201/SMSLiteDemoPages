using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SMSLiteUI.Services;

public sealed class UserStateService(AuthenticationStateProvider authenticationStateProvider)
{
    private static readonly string[] EmailClaimTypes =
    [
        ClaimTypes.Email,
        "email",
        "preferred_username",
        "upn"
    ];

    private static readonly string[] UserKeyClaimTypes =
    [
        "oid",
        ClaimTypes.NameIdentifier,
        "sub",
        "upn",
        ClaimTypes.Email,
        "email",
        "preferred_username"
    ];

    private static readonly string[] DisplayNameClaimTypes =
    [
        ClaimTypes.Name,
        "name",
        "preferred_username",
        ClaimTypes.Email
    ];

    private static readonly string[] RoleClaimTypes =
    [
        ClaimTypes.Role,
        "roles",
        "role",
        "groups"
    ];

    public async Task<UserState> GetCurrentUserAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var principal = authState.User;

        if (principal.Identity?.IsAuthenticated != true)
            return new UserState(false, null, null, null, []);

        var email = FirstClaimValue(principal, EmailClaimTypes);
        var userKey = FirstClaimValue(principal, UserKeyClaimTypes) ?? principal.Identity.Name;
        var displayName = FirstClaimValue(principal, DisplayNameClaimTypes) ?? email ?? principal.Identity.Name;
        var roles = principal.Claims
            .Where(claim => RoleClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        return new UserState(true, userKey, email, displayName, roles);
    }

    private static string? FirstClaimValue(ClaimsPrincipal principal, IReadOnlyCollection<string> claimTypes)
        => principal.Claims
            .FirstOrDefault(claim => claimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase) &&
                                     !string.IsNullOrWhiteSpace(claim.Value))
            ?.Value;
}
