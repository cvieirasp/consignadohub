namespace ConsignadoHub.BuildingBlocks.Auth;

public class KeycloakSettings
{
    public const string SectionName = "Keycloak";
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = false;
}
