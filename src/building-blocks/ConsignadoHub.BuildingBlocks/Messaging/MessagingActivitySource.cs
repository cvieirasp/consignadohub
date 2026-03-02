using System.Diagnostics;

namespace ConsignadoHub.BuildingBlocks.Messaging;

public static class MessagingActivitySource
{
    public const string SourceName = "ConsignadoHub.Messaging";

    internal static readonly ActivitySource Source = new(SourceName);
}
