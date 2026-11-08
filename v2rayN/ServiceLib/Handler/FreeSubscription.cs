namespace ServiceLib.Handler;

/// <summary>
/// Built-in Free group fed from the public site feed. Users cannot delete or retarget it.
/// </summary>
public static class FreeSubscription
{
    public const string Remarks = "رایگان";

    public static bool IsFree(string? id) =>
        string.Equals(id, Global.FreeSubscriptionId, StringComparison.Ordinal);

    public static void Pin(SubItem item)
    {
        item.Id = Global.FreeSubscriptionId;
        item.Remarks = Remarks;
        item.Url = Global.FreeSubscriptionUrl;
        item.Enabled = true;
        item.AutoUpdateInterval = Math.Max(item.AutoUpdateInterval, 15);
        item.Sort = Math.Min(item.Sort <= 0 ? 1 : item.Sort, 1);
    }

    /// <summary>
    /// True only for a deliberate empty list: blank body, or base64 that decodes to blank.
    /// An error page must not wipe saved free configs.
    /// </summary>
    public static bool IsExplicitlyEmptyFeed(string? body)
    {
        if (body.IsNullOrEmpty())
        {
            return true;
        }

        var compact = Regex.Replace(body.Trim(), @"\s+", string.Empty);
        if (compact.Length == 0)
        {
            return true;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(compact));
            return decoded.IsNullOrEmpty();
        }
        catch
        {
            return false;
        }
    }

    public static async Task EnsureAsync()
    {
        var existing = await AppManager.Instance.GetSubItem(Global.FreeSubscriptionId);
        if (existing is null)
        {
            var item = new SubItem
            {
                Id = Global.FreeSubscriptionId,
                Sort = 1
            };
            Pin(item);

            // Push other groups down so Free stays first.
            var others = await AppManager.Instance.SubItems() ?? [];
            foreach (var other in others.Where(t => !IsFree(t.Id)))
            {
                if (other.Sort < 2)
                {
                    other.Sort += 1;
                    await SQLiteHelper.Instance.ReplaceAsync(other);
                }
            }

            await SQLiteHelper.Instance.ReplaceAsync(item);
            return;
        }

        var beforeRemarks = existing.Remarks;
        var beforeUrl = existing.Url;
        var beforeEnabled = existing.Enabled;
        var beforeInterval = existing.AutoUpdateInterval;
        Pin(existing);
        if (beforeRemarks != existing.Remarks
            || beforeUrl != existing.Url
            || beforeEnabled != existing.Enabled
            || beforeInterval != existing.AutoUpdateInterval
            || existing.Sort != 1)
        {
            existing.Sort = 1;
            await SQLiteHelper.Instance.ReplaceAsync(existing);
        }
    }
}
