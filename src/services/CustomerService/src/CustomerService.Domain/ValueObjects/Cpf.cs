namespace CustomerService.Domain.ValueObjects;

public sealed class Cpf
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string raw)
    {
        var normalized = Normalize(raw);
        if (!IsValid(normalized))
            throw new ArgumentException($"'{raw}' is not a valid CPF.", nameof(raw));
        return new Cpf(normalized);
    }

    public static bool IsValid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var digits = Normalize(raw);
        if (digits.Length != 11)
            return false;

        // All same digits are invalid
        if (digits.Distinct().Count() == 1)
            return false;

        return ValidateCheckDigits(digits);
    }

    private static string Normalize(string raw) =>
        new string(raw.Where(char.IsDigit).ToArray());

    private static bool ValidateCheckDigits(string digits)
    {
        // First check digit
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * (10 - i);
        var remainder = sum % 11;
        var first = remainder < 2 ? 0 : 11 - remainder;
        if (first != digits[9] - '0') return false;

        // Second check digit
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (digits[i] - '0') * (11 - i);
        remainder = sum % 11;
        var second = remainder < 2 ? 0 : 11 - remainder;
        return second == digits[10] - '0';
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Cpf other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
