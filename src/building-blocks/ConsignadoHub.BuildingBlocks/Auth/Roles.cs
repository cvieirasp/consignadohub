namespace ConsignadoHub.BuildingBlocks.Auth;

public static class Roles
{
    public const string Admin   = "consignado-admin";
    public const string Analyst = "consignado-analyst";
}

public static class Policies
{
    public const string AdminOnly      = "AdminOnly";
    public const string AnalystOrAdmin = "AnalystOrAdmin";
}
