using System.Security.Claims;
using System.Text.Encodings.Web;
using ConsignadoHub.BuildingBlocks.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerService.IntegrationTests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read role from Authorization header: "Test consignado-analyst"
        // Default to admin if no role is specified.
        var role = Roles.Admin;
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Test ", StringComparison.Ordinal) == true)
        {
            var extracted = authHeader["Test ".Length..].Trim();
            if (!string.IsNullOrEmpty(extracted))
                role = extracted;
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, role),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
