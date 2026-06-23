using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SMSLiteModels.Entities.Integrations;
using External.ELMA.Client.Configuration;

namespace SMSLiteCommandAPI.Common.SMSLiteAuthentication;

public sealed class ElmaClientAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<ElmaClientOptions> _elmaOptions;

    public ElmaClientAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<ElmaClientOptions> elmaOptions)
        : base(options, logger, encoder)
    {
        _elmaOptions = elmaOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = _elmaOptions.CurrentValue;

        if (options.AuthenticationMode == ElmaAuthenticationMode.None)
            return Task.FromResult(Success("anonymous-elma-client"));

        if (options.AuthenticationMode == ElmaAuthenticationMode.ApiKey)
        {
            if (!Request.Headers.TryGetValue(options.ApiKeyHeaderName, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
                return Task.FromResult(AuthenticateResult.Fail($"Missing API key header '{options.ApiKeyHeaderName}'."));

            if (!string.Equals(apiKey.ToString(), options.ApiKey, StringComparison.Ordinal))
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

            return Task.FromResult(Success("api-key-client"));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));

        var token = authorizationHeader.ToString();
        const string prefix = "Bearer ";
        if (!token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Authorization header must use Bearer token format."));

        var value = token[prefix.Length..].Trim();
        if (!string.Equals(value, options.BearerToken, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid bearer token."));

        return Task.FromResult(Success("bearer-client"));
    }

    private AuthenticateResult Success(string subject)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Name, subject)
        ], Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
