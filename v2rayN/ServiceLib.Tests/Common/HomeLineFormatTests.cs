using System.Globalization;

namespace ServiceLib.Tests.Common;

public class HomeLineFormatTests
{
    [Test]
    public async Task MaskIp_KeepsTheFirstGroup()
    {
        await HomeLineFormat.MaskIp("195.123.209.211").Should().BeEqualTo("195.***.***.***");
        await HomeLineFormat.MaskIp("8.8.8.8").Should().BeEqualTo("8.***.***.***");
        await HomeLineFormat.MaskIp("2001:db8::1").Should().BeEqualTo("2001:****");
    }

    [Test]
    public async Task CountryName_UsesTheCurrentLanguage()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            await HomeLineFormat.CountryName("DE").Should().BeEqualTo("Germany");
            await HomeLineFormat.CountryName("unknown").Should().BeNull();
            await HomeLineFormat.CountryName("Iran").Should().BeEqualTo("Iran");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
