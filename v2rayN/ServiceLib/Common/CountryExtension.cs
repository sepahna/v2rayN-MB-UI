namespace ServiceLib.Common;

/// <summary>
/// One piece of a label that should be drawn as text or as a country flag.
/// </summary>
public abstract record FlagSegment
{
    public sealed record Text(string Value) : FlagSegment;

    public sealed record Flag(string CountryCode) : FlagSegment;
}

/// <summary>
/// Country-code helpers. Flag emoji are regional-indicator pairs, which Windows draws as two letters.
/// </summary>
public static class CountryExtension
{
    private static readonly HashSet<string> KnownCountryCodes = LoadCountryCodes();

    public static IReadOnlyCollection<string> KnownCodes => KnownCountryCodes;

    /// <summary>
    /// Converts a country code to a flag emoji.
    /// Example: "US" -> "🇺🇸", "UK" -> "🇬🇧".
    /// </summary>
    public static string? CountryToEmoji(this string? countryCode)
    {
        var code = NormalizeCountryCode(countryCode);
        if (code == null || !KnownCountryCodes.Contains(code))
        {
            return null;
        }

        return string.Concat(
            char.ConvertFromUtf32(0x1F1E6 + code[0] - 'A'),
            char.ConvertFromUtf32(0x1F1E6 + code[1] - 'A'));
    }

    public static string? NormalizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var code = countryCode.Trim();
        if (code.Equals("UK", StringComparison.OrdinalIgnoreCase))
        {
            code = "GB";
        }

        if (code.Length != 2 || !char.IsAsciiLetter(code[0]) || !char.IsAsciiLetter(code[1]))
        {
            return null;
        }

        return code.ToUpperInvariant();
    }

    /// <summary>
    /// Splits a server name or IP-info label into text and flag pieces.
    /// Recognizes flag emoji, a leading country code such as "US Washington", and wrapped codes such as "(US)".
    /// </summary>
    public static IReadOnlyList<FlagSegment> SplitFlags(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        var result = new List<FlagSegment>();
        var index = 0;
        var textStart = 0;

        var lead = index;
        while (lead < value.Length && value[lead] == ' ')
        {
            lead++;
        }

        if (TryReadLeadingCountry(value, lead, out var leadCode, out var leadNext))
        {
            result.Add(new FlagSegment.Flag(leadCode));
            index = leadNext;
            textStart = index;
        }

        while (index < value.Length)
        {
            if (TryReadRegionalFlag(value, index, out var emojiCode, out var emojiNext))
            {
                AddText(result, value, textStart, index);
                result.Add(new FlagSegment.Flag(emojiCode));
                index = SkipFlagSuffix(value, emojiNext, emojiCode);
                textStart = index;
                continue;
            }

            if (TryReadWrappedCountry(value, index, out var wrappedCode, out var wrappedNext))
            {
                AddText(result, value, textStart, index);
                result.Add(new FlagSegment.Flag(wrappedCode));
                index = SkipOneSeparator(value, wrappedNext);
                textStart = index;
                continue;
            }

            if (IsAfterSummaryMark(value, index) && TryReadAsciiCountry(value, index, out var summaryCode, out var summaryNext))
            {
                AddText(result, value, textStart, index);
                result.Add(new FlagSegment.Flag(summaryCode));
                index = summaryNext;
                textStart = index;
                continue;
            }

            index += Rune.GetRuneAt(value, index).Utf16SequenceLength;
        }

        AddText(result, value, textStart, value.Length);
        return result;
    }

    private static void AddText(List<FlagSegment> result, string value, int start, int end)
    {
        if (end > start)
        {
            result.Add(new FlagSegment.Text(value[start..end]));
        }
    }

    private static int SkipFlagSuffix(string text, int index, string code)
    {
        var duplicate = SkipDuplicateCode(text, index, code);
        if (duplicate != index)
        {
            return duplicate;
        }

        var wrapped = SkipWrappedSameCode(text, index, code);
        if (wrapped != index)
        {
            return wrapped;
        }

        return SkipOneSeparator(text, index);
    }

    private static int SkipDuplicateCode(string text, int index, string code)
    {
        var cursor = index;
        while (cursor < text.Length && text[cursor] == ' ')
        {
            cursor++;
        }

        if (!TryReadAsciiCountry(text, cursor, out var found, out var next) || !found.Equals(code, StringComparison.Ordinal))
        {
            return index;
        }

        return next;
    }

    private static int SkipWrappedSameCode(string text, int index, string code)
    {
        if (!TryReadWrappedCountry(text, index, out var found, out var next) || !found.Equals(code, StringComparison.Ordinal))
        {
            return index;
        }

        return SkipOneSeparator(text, next);
    }

    private static bool TryReadRegionalFlag(string text, int index, out string code, out int next)
    {
        code = string.Empty;
        next = index;
        if (!TryReadRegionalIndicator(text, index, out var first, out var afterFirst)
            || !TryReadRegionalIndicator(text, afterFirst, out var second, out var afterSecond))
        {
            return false;
        }

        code = string.Create(2, (first, second), static (span, state) =>
        {
            span[0] = state.first;
            span[1] = state.second;
        });
        if (!KnownCountryCodes.Contains(code))
        {
            return false;
        }

        next = afterSecond;
        if (next < text.Length && text[next] == '\uFE0F')
        {
            next++;
        }

        return true;
    }

    private static bool TryReadRegionalIndicator(string text, int index, out char letter, out int next)
    {
        letter = '\0';
        next = index;
        if (index >= text.Length)
        {
            return false;
        }

        var rune = Rune.GetRuneAt(text, index);
        if (rune.Value is < 0x1F1E6 or > 0x1F1FF)
        {
            return false;
        }

        letter = (char)('A' + rune.Value - 0x1F1E6);
        next = index + rune.Utf16SequenceLength;
        return true;
    }

    private static readonly Dictionary<string, string> LeadingAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USA"] = "US",
        ["UAE"] = "AE"
    };

    private static bool TryReadLeadingCountry(string text, int index, out string code, out int next)
    {
        if (TryReadAlias(text, index, out code, out next))
        {
            return true;
        }

        return TryReadAsciiCountry(text, index, out code, out next);
    }

    private static bool TryReadAlias(string text, int index, out string code, out int next)
    {
        code = string.Empty;
        next = index;
        foreach (var alias in LeadingAliases)
        {
            if (index + alias.Key.Length > text.Length)
            {
                continue;
            }

            if (!text.AsSpan(index, alias.Key.Length).Equals(alias.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var after = index + alias.Key.Length;
            if (after < text.Length && !IsPrefixSeparator(text[after]))
            {
                continue;
            }

            code = alias.Value;
            next = after < text.Length ? after + 1 : after;
            return true;
        }

        return false;
    }

    private static bool TryReadAsciiCountry(string text, int index, out string code, out int next)
    {
        code = string.Empty;
        next = index;
        if ((uint)index + 1 >= (uint)text.Length)
        {
            return false;
        }

        if (!char.IsAsciiLetter(text[index]) || !char.IsAsciiLetter(text[index + 1]))
        {
            return false;
        }

        if (index + 2 < text.Length && char.IsAsciiLetter(text[index + 2]))
        {
            return false;
        }

        var normalized = NormalizeCountryCode(text.Substring(index, 2));
        if (normalized == null || !KnownCountryCodes.Contains(normalized))
        {
            return false;
        }

        if (index + 2 < text.Length && !IsPrefixSeparator(text[index + 2]))
        {
            return false;
        }

        code = normalized;
        next = index + 2;
        if (next < text.Length && IsPrefixSeparator(text[next]))
        {
            next++;
        }

        return true;
    }

    private static bool TryReadWrappedCountry(string text, int index, out string code, out int next)
    {
        code = string.Empty;
        next = index;
        if (index >= text.Length || text[index] is not ('(' or '['))
        {
            return false;
        }

        var close = text[index] == '(' ? ')' : ']';
        if (index + 3 >= text.Length || text[index + 3] != close)
        {
            return false;
        }

        var normalized = NormalizeCountryCode(text.Substring(index + 1, 2));
        if (normalized == null || !KnownCountryCodes.Contains(normalized))
        {
            return false;
        }

        code = normalized;
        next = index + 4;
        return true;
    }

    private static int SkipOneSeparator(string text, int index)
    {
        if (index < text.Length && IsPrefixSeparator(text[index]))
        {
            return index + 1;
        }

        return index;
    }

    private static bool IsAfterSummaryMark(string text, int index)
    {
        var cursor = index - 1;
        while (cursor >= 0 && text[cursor] == ' ')
        {
            cursor--;
        }

        return cursor >= 0 && text[cursor] == ']';
    }

    private static bool IsPrefixSeparator(char value)
    {
        return value is ' ' or '\t' or '-' or '_' or '|' or '/' or ':' or '·';
    }

    private static HashSet<string> LoadCountryCodes()
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stream = typeof(CountryExtension).Assembly.GetManifestResourceStream("ServiceLib.Flags.codes.txt");
        if (stream == null)
        {
            return codes;
        }

        using (stream)
        using (var reader = new StreamReader(stream))
        {
            while (reader.ReadLine() is { } line)
            {
                var code = NormalizeCountryCode(line);
                if (code != null)
                {
                    codes.Add(code);
                }
            }
        }

        return codes;
    }
}
