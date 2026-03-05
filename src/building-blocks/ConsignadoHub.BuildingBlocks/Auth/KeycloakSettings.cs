namespace ConsignadoHub.BuildingBlocks.Auth;

/// <summary>
/// Represents the configuration settings required to integrate with Keycloak for authentication.
/// </summary>
public class KeycloakSettings
{
    public const string SectionName = "Keycloak";
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = false;
}
