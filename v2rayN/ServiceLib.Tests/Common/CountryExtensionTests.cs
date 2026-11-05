namespace ServiceLib.Tests.Common;

public class CountryExtensionTests
{
    [Test]
    public async Task CountryToEmoji_BuildsMongoliaAndUnitedKingdom()
    {
        var mongolia = "MN".CountryToEmoji();
        var britain = "UK".CountryToEmoji();

        await mongolia.Should().NotBeNull();
        await char.ConvertToUtf32(mongolia!, 0).Should().BeEqualTo(0x1F1F2);
        await char.ConvertToUtf32(mongolia, 2).Should().BeEqualTo(0x1F1F3);
        await britain.Should().BeEqualTo("GB".CountryToEmoji());
        await "not-a-country".CountryToEmoji().Should().BeNull();
    }

    [Test]
    public async Task SplitFlags_ReplacesFlagEmojiWithCountryImageToken()
    {
        var parts = CountryExtension.SplitFlags("🇲🇳 Mongolia");

        await parts.Count.Should().BeEqualTo(2);
        await ((FlagSegment.Flag)parts[0]).CountryCode.Should().BeEqualTo("MN");
        await ((FlagSegment.Text)parts[1]).Value.Should().BeEqualTo("Mongolia");
    }

    [Test]
    public async Task SplitFlags_ReplacesLeadingCountryCodeAndWrappedIpLabel()
    {
        var remarks = CountryExtension.SplitFlags("us Washington");
        var ipInfo = CountryExtension.SplitFlags("🇺🇸(US) 8.8.8.8");

        await ((FlagSegment.Flag)remarks[0]).CountryCode.Should().BeEqualTo("US");
        await ((FlagSegment.Text)remarks[1]).Value.Should().BeEqualTo("Washington");
        await ((FlagSegment.Flag)ipInfo[0]).CountryCode.Should().BeEqualTo("US");
        await ((FlagSegment.Text)ipInfo[1]).Value.Should().BeEqualTo("8.8.8.8");
    }

    [Test]
    public async Task SplitFlags_ReplacesCountryCodeInsideRunningServerSummary()
    {
        var parts = CountryExtension.SplitFlags("[VLESS] hr Croatia(example:443)");

        await ((FlagSegment.Text)parts[0]).Value.Should().BeEqualTo("[VLESS] ");
        await ((FlagSegment.Flag)parts[1]).CountryCode.Should().BeEqualTo("HR");
        await ((FlagSegment.Text)parts[2]).Value.Should().BeEqualTo("Croatia(example:443)");
    }

    [Test]
    public async Task SplitFlags_LeavesOrdinaryNamesUntouched()
    {
        var parts = CountryExtension.SplitFlags("gardi");

        await parts.Count.Should().BeEqualTo(1);
        await ((FlagSegment.Text)parts[0]).Value.Should().BeEqualTo("gardi");
    }

    [Test]
    public async Task FlagCatalog_ContainsPngForKnownCountries()
    {
        foreach (var code in CountryExtension.KnownCodes)
        {
            var png = CountryFlagCatalog.GetPng(code);
            await (png is { Length: > 8 } && png[0] == 0x89 && png[1] == 0x50).Should().BeTrue();
        }

        await CountryFlagCatalog.GetPng("UK")![0].Should().BeEqualTo((byte)0x89);
    }
}
