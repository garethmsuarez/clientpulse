namespace ClientPulse.Api;

public static class EnumNames
{
    // Accepts member names only (case-insensitive, surrounding whitespace
    // ignored). Enum.TryParse would also accept numbers. Null when no match.
    public static T? Parse<T>(string? value) where T : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (string.Equals(candidate.ToString(), value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
