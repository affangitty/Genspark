namespace BusBooking.Application.Common;

public static class PhoneNormalizer
{
    /// <summary>Normalizes phone for lookup/storage (matches client: optional + then digits).</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        var s = raw.Trim();
        if (s.StartsWith("+", StringComparison.Ordinal))
            return "+" + new string(s[1..].Where(char.IsDigit).ToArray());
        return new string(s.Where(char.IsDigit).ToArray());
    }
}
