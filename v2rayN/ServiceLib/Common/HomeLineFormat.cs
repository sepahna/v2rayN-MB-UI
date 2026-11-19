using System.Globalization;

namespace ServiceLib.Common;

/// <summary>
/// Home-card text for the public line. Windows cannot draw flag emoji, so callers show the country as an image.
/// </summary>
public static class HomeLineFormat
{
    public static string MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return string.Empty;
        }

        var value = ip.Trim();
        var colon = value.IndexOf(':');
        var dot = value.IndexOf('.');
        if (dot > 0 && (colon < 0 || dot < colon))
        {
            return value[..dot] + ".***.***.***";
        }

        if (colon > 0)
        {
            return value[..colon] + ":****";
        }

        return value;
    }

    public static string? CountryName(string? country)
    {
        if (string.IsNullOrWhiteSpace(country) || country.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var code = CountryExtension.NormalizeCountryCode(country);
        if (code == null)
        {
            return country.Trim();
        }

        try
        {
            return new RegionInfo(code).DisplayName;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }
}
