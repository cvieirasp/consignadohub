namespace ConsignadoHub.BuildingBlocks.Guards;

/// <summary>
/// Provides guard clauses for validating method parameters and ensuring that they meet 
/// certain conditions before proceeding with the execution of the method. 
/// This helps to prevent errors and ensure that the application behaves as expected.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    public static string AgainstNullOrEmpty(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    public static decimal AgainstNegativeOrZero(decimal value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be greater than zero.");
        return value;
    }

    public static decimal AgainstOutOfRange(decimal value, string paramName, decimal min, decimal max)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be between {min} and {max}.");
        return value;
    }

    public static int AgainstOutOfRange(int value, string paramName, int min, int max)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be between {min} and {max}.");
        return value;
    }
}
