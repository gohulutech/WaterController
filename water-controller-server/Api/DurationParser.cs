using System.Globalization;
using System.Text.RegularExpressions;

namespace Api;

public static partial class DurationParser
{
    public static bool TryParse(string value, out long seconds)
    {
        seconds = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = DurationRegex().Match(value.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        var multiplier = match.Groups[2].Value.ToLowerInvariant() switch
        {
            "s" => 1L,
            "m" => 60L,
            "h" => 3_600L,
            "d" => 86_400L,
            _ => 0L,
        };

        if (multiplier == 0)
        {
            return false;
        }

        try
        {
            seconds = checked(amount * multiplier);
        }
        catch (OverflowException)
        {
            return false;
        }

        return seconds > 0;
    }

    [GeneratedRegex(@"^(\d+)(s|m|h|d)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DurationRegex();
}
