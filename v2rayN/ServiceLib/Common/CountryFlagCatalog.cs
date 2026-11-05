namespace ServiceLib.Common;

/// <summary>
/// Embedded flag images. Windows cannot draw flag emoji, so the UI paints these instead.
/// </summary>
public static class CountryFlagCatalog
{
    private static readonly Assembly FlagAssembly = typeof(CountryFlagCatalog).Assembly;
    private static readonly ConcurrentDictionary<string, byte[]?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[]? GetPng(string? countryCode)
    {
        var code = CountryExtension.NormalizeCountryCode(countryCode);
        if (code == null)
        {
            return null;
        }

        return Cache.GetOrAdd(code, static normalized =>
        {
            using var stream = FlagAssembly.GetManifestResourceStream($"ServiceLib.Flags.{normalized.ToLowerInvariant()}.png");
            if (stream == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        });
    }
}
