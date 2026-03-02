using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace ConsignadoHub.BuildingBlocks.Auth;

/// <summary>
/// Transforms the Keycloak JWT principal by extracting realm roles from the
/// nested <c>realm_access.roles</c> JSON claim and mapping them to standard
/// <see cref="ClaimTypes.Role"/> claims that ASP.NET Core policy checks understand.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Keycloak emits: "realm_access": {"roles": ["consignado-admin", ...]}
        var realmAccessClaim = principal.FindFirst("realm_access");
        if (realmAccessClaim is null)
            return Task.FromResult(principal);

        JsonElement realmAccess;
        try
        {
            realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
        }
        catch (JsonException)
        {
            return Task.FromResult(principal);
        }

        if (!realmAccess.TryGetProperty("roles", out var roles))
            return Task.FromResult(principal);

        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var role in roles.EnumerateArray())
        {
            var roleName = role.GetString();
            if (!string.IsNullOrEmpty(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        }

        return Task.FromResult(principal);
    }
}
