namespace ServiceLib.Tests.Handler;

public class FreeSubscriptionTests
{
    [Test]
    public async Task IsFree_RecognizesBuiltInId()
    {
        await FreeSubscription.IsFree(Global.FreeSubscriptionId).Should().BeTrue();
        await FreeSubscription.IsFree("other").Should().BeFalse();
        await FreeSubscription.IsFree(null).Should().BeFalse();
    }

    [Test]
    public async Task Pin_LocksRemarksAndUrl()
    {
        var item = new SubItem
        {
            Id = Global.FreeSubscriptionId,
            Remarks = "changed",
            Url = "https://example.com",
            Enabled = false,
            AutoUpdateInterval = 0,
            Sort = 9
        };

        FreeSubscription.Pin(item);

        await item.Remarks.Should().BeEqualTo(FreeSubscription.Remarks);
        await item.Url.Should().BeEqualTo(Global.FreeSubscriptionUrl);
        await item.Enabled.Should().BeTrue();
        await item.AutoUpdateInterval.Should().BeGreaterThanOrEqualTo(15);
        await item.Sort.Should().BeEqualTo(1);
    }

    [Test]
    public async Task IsExplicitlyEmptyFeed_AcceptsBlankAndRejectsFailureAndErrorHtml()
    {
        await FreeSubscription.IsExplicitlyEmptyFeed(null).Should().BeFalse();
        await FreeSubscription.IsExplicitlyEmptyFeed("").Should().BeTrue();
        await FreeSubscription.IsExplicitlyEmptyFeed("   ").Should().BeTrue();
        await FreeSubscription.IsExplicitlyEmptyFeed(Convert.ToBase64String(Encoding.UTF8.GetBytes("  "))).Should().BeTrue();
        await FreeSubscription.IsExplicitlyEmptyFeed("<html>error</html>").Should().BeFalse();
        await FreeSubscription.IsExplicitlyEmptyFeed("vmess://abc").Should().BeFalse();
    }
}
