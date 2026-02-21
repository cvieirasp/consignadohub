namespace ConsignadoHub.BuildingBlocks.Results;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public bool IsNone => Code == string.Empty;

    public override string ToString() => IsNone ? "None" : $"{Code}: {Message}";
}
