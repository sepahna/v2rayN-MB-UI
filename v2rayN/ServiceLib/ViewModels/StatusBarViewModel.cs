namespace ServiceLib.ViewModels;

public partial class StatusBarViewModel : MyReactiveObject
{
    public Interaction<string, RxVoid> SetClipboardDataInteraction { get; } = new();
    public Interaction<RxVoid, string?> PasswordInputInteraction { get; } = new();
    public Interaction<RxVoid, RxVoid> DispatcherRefreshIconInteraction { get; } = new();
    public EventChannel<bool> SubscriptionsUpdateRequested { get; } = new();
    public EventChannel<bool?> ShowHideWindowRequested { get; } = new();

    private static readonly Lazy<StatusBarViewModel> _instance = new(() => new());
    public static StatusBarViewModel Instance => _instance.Value;

    public EventChannel<string> SetDefaultServerRequested { get; } = new();
    public EventChannel<RxVoid> ReloadRequested { get; } = new();
    public EventChannel<RxVoid> AddServerViaScanRequested { get; } = new();
    public EventChannel<RxVoid> AddServerViaClipboardRequested { get; } = new();

    #region ObservableCollection

    public BulkObservableCollection<RoutingItem> RoutingItems { get; } = [];

    public BulkObservableCollection<ComboItem> Servers { get; } = [];

    [Reactive]
    public partial RoutingItem SelectedRouting { get; set; }

    [Reactive]
    public partial ComboItem SelectedServer { get; set; }

    [Reactive]
    public partial bool BlServers { get; set; }

    #endregion ObservableCollection

    public ReactiveCommand<RxVoid, RxVoid> AddServerViaClipboardCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> AddServerViaScanCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SubUpdateCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SubUpdateViaProxyCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> CopyProxyCmdToClipboardCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> NotifyLeftClickCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ShowWindowCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> HideWindowCmd { get; }

    #region System Proxy

    [Reactive]
    public partial bool BlSystemProxyClear { get; set; }

    [Reactive]
    public partial bool BlSystemProxySet { get; set; }

    [Reactive]
    public partial bool BlSystemProxyNothing { get; set; }

    [Reactive]
    public partial bool BlSystemProxyPac { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> SystemProxyClearCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SystemProxySetCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SystemProxyNothingCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SystemProxyPacCmd { get; }

    [Reactive]
    public partial bool BlRouting { get; set; }

    [Reactive]
    public partial int SystemProxySelected { get; set; }

    [Reactive]
    public partial bool BlSystemProxyPacVisible { get; set; }

    #endregion System Proxy

    #region UI

    [Reactive]
    public partial string InboundDisplay { get; set; }

    [Reactive]
    public partial string InboundLanDisplay { get; set; }

    [Reactive]
    public partial string RunningServerDisplay { get; set; }

    [Reactive]
    public partial string RunningServerToolTipText { get; set; }

    [Reactive]
    public partial string RunningInfoDisplay { get; set; }

    [Reactive]
    public partial string SpeedProxyDisplay { get; set; }

    [Reactive]
    public partial string SpeedDirectDisplay { get; set; }

    [Reactive]
    public partial bool EnableTun { get; set; }

    [Reactive]
    public partial bool BlIsNonWindows { get; set; }

    [Reactive]
    public partial bool HomeConnected { get; set; }

    [Reactive]
    public partial bool HomeHasFlag { get; set; }

    [Reactive]
    public partial string HomeLineLabel { get; set; }

    [Reactive]
    public partial string HomeIpText { get; set; }

    [Reactive]
    public partial string HomeFlagText { get; set; }

    [Reactive]
    public partial string HomeDownloadText { get; set; }

    [Reactive]
    public partial string HomeUploadText { get; set; }

    [Reactive]
    public partial string HomeTrafficText { get; set; }

    [Reactive]
    public partial int HomeWaveVersion { get; set; }

    public double[] HomeDownloadWave { get; private set; } = [];

    public double[] HomeUploadWave { get; private set; } = [];

    public ReactiveCommand<RxVoid, RxVoid> ToggleHomeConnectionCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> ToggleHomeIpCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshHomeLineCmd { get; }

    private bool _homeReady;
    private bool _checking;
    private bool _showFullIp;
    private int _lineGeneration;
    private string? _rawIp;
    private string? _country;
    private long _lastDown;
    private long _lastUp;
    private long _sessionUpKb;
    private long _sessionDownKb;
    private readonly List<double> _downWave = [];
    private readonly List<double> _upWave = [];

    #endregion UI

    public StatusBarViewModel()
    {
        _config = AppManager.Instance.Config;
        SelectedRouting = new();
        SelectedServer = new();
        RunningServerToolTipText = GetRunningServerToolTipText("-");
        BlSystemProxyPacVisible = Utils.IsWindows();
        BlIsNonWindows = Utils.IsNonWindows();

        if (_config.TunModeItem.EnableTun && AllowEnableTun())
        {
            EnableTun = true;
        }
        else
        {
            _config.TunModeItem.EnableTun = EnableTun = false;
        }

        #region WhenAnyValue && ReactiveCommand

        this.WhenAnyValue(x => x.SelectedRouting)
            .Where(y => y != null && !y.Remarks.IsNullOrEmpty())
            .SubscribeAsync(async _ => await RoutingSelectedChangedAsync());

        this.WhenAnyValue(x => x.SelectedServer)
            .Where(y => y != null && !y.Text.IsNullOrEmpty())
            .Subscribe(_ => ServerSelectedChanged());

        SystemProxySelected = (int)_config.SystemProxyItem.SysProxyType;
        this.WhenAnyValue(x => x.SystemProxySelected)
            .Where(y => y >= 0)
            .SubscribeAsync(async _ => await DoSystemProxySelected());

        this.WhenAnyValue(x => x.EnableTun)
            .SubscribeAsync(async _ => await DoEnableTun());

        CopyProxyCmdToClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await CopyProxyCmdToClipboard();
        });

        NotifyLeftClickCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            ShowHideWindowRequested.Publish(null);
            await Task.CompletedTask;
        });
        ShowWindowCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            ShowHideWindowRequested.Publish(true);
            await Task.CompletedTask;
        });
        HideWindowCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            ShowHideWindowRequested.Publish(false);
            await Task.CompletedTask;
        });

        AddServerViaClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                await AddServerViaClipboard();
            });
        AddServerViaScanCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerViaScan();
        });
        SubUpdateCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess(false);
        });
        SubUpdateViaProxyCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess(true);
        });

        //System proxy
        SystemProxyClearCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SetListenerType(ESysProxyType.ForcedClear);
        });
        SystemProxySetCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SetListenerType(ESysProxyType.ForcedChange);
        });
        SystemProxyNothingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SetListenerType(ESysProxyType.Unchanged);
        });
        SystemProxyPacCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SetListenerType(ESysProxyType.Pac);
        });

        HomeLineLabel = ResUI.HomeLineDirect ?? "Your line";
        HomeIpText = ResUI.HomeLineUnknown ?? "Checking IP…";
        HomeFlagText = string.Empty;
        HomeDownloadText = "↓ 0.0 B/s";
        HomeUploadText = "↑ 0.0 B/s";
        HomeTrafficText = FormatHomeText(ResUI.HomeTrafficSession, "0.0 B");
        ToggleHomeConnectionCmd = ReactiveCommand.CreateFromTask(ToggleHomeConnection);
        ToggleHomeIpCmd = ReactiveCommand.Create(ToggleHomeIp);
        RefreshHomeLineCmd = ReactiveCommand.CreateFromTask(RefreshHomeLineAsync);
        this.WhenAnyValue(x => x.BlSystemProxySet, x => x.EnableTun, (proxy, tun) => proxy || tun)
            .DistinctUntilChanged()
            .Skip(1)
            .SubscribeAsync(async _ =>
            {
                if (_homeReady)
                {
                    await ResetHomeConnectionAsync();
                }
            });

        #endregion WhenAnyValue && ReactiveCommand

        #region AppEvents

        AppEvents.DispatcherStatisticsRequested
            .AsObservable()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .SubscribeAsync(async result => await UpdateStatistics(result));

        AppEvents.SysProxyChangeRequested
            .AsObservable()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .SubscribeAsync(async result => await SetListenerType(result));

        #endregion AppEvents

        _ = Init();
    }

    private async Task Init()
    {
        await ConfigHandler.InitBuiltinRouting(_config);
        await RefreshRoutingsMenu();
        await InboundDisplayStatus();
        // The view refreshes its icon on initialization; interaction handlers are not registered yet.
        await ChangeSystemProxyAsync(_config.SystemProxyItem.SysProxyType, false);

        BlRouting = true;
        _homeReady = true;
        // Let the main window and tray icon finish loading before the first IP request.
        _ = RefreshHomeLineAfterStartupAsync();
    }

    private async Task RefreshHomeLineAfterStartupAsync()
    {
        try
        {
            await Task.Delay(1500);
            await RefreshHomeLineAsync();
        }
        catch (Exception ex)
        {
            Logging.SaveLog("HomeLine", ex);
        }
    }

    private async Task CopyProxyCmdToClipboard()
    {
        var cmd = Utils.IsWindows() ? "set" : "export";
        var address = $"{Global.Loopback}:{AppManager.Instance.GetLocalPort(EInboundProtocol.socks)}";

        var sb = new StringBuilder();
        sb.AppendLine($"{cmd} http_proxy={Global.HttpProtocol}{address}");
        sb.AppendLine($"{cmd} https_proxy={Global.HttpProtocol}{address}");
        sb.AppendLine($"{cmd} all_proxy={Global.Socks5Protocol}{address}");
        sb.AppendLine("");
        sb.AppendLine($"{cmd} HTTP_PROXY={Global.HttpProtocol}{address}");
        sb.AppendLine($"{cmd} HTTPS_PROXY={Global.HttpProtocol}{address}");
        sb.AppendLine($"{cmd} ALL_PROXY={Global.Socks5Protocol}{address}");

        await SetClipboardDataInteraction.HandleSafe(sb.ToString());
    }

    private async Task AddServerViaClipboard()
    {
        AddServerViaClipboardRequested.Publish();
        await Task.Delay(1000);
    }

    private async Task AddServerViaScan()
    {
        AddServerViaScanRequested.Publish();
        await Task.Delay(1000);
    }

    private async Task UpdateSubscriptionProcess(bool blProxy)
    {
        SubscriptionsUpdateRequested.Publish(blProxy);
        await Task.Delay(1000);
    }

    public async Task RefreshServersBiz()
    {
        await RefreshServersMenu();

        //display running server
        var running = await ConfigHandler.GetDefaultServer(_config);
        if (running != null)
        {
            RunningServerDisplay = running.GetSummary();
            RunningServerToolTipText = GetRunningServerToolTipText(RunningServerDisplay);
        }
        else
        {
            RunningServerDisplay = ResUI.CheckServerSettings;
            RunningServerToolTipText = GetRunningServerToolTipText(RunningServerDisplay);
        }
    }

    private string GetRunningServerToolTipText(string serverInfo)
    {
        return Utils.IsLinux() ? Global.AppName : serverInfo;
    }

    private async Task RefreshServersMenu()
    {
        var lstModel = await AppManager.Instance.ProfileModels(_config.SubIndexId, "");

        if (lstModel?.Count > _config.GuiItem.TrayMenuServersLimit)
        {
            BlServers = false;
            return;
        }

        var models = lstModel.Select(it => new ComboItem { ID = it.IndexId, Text = it.GetSummary() }).ToList();

        BlServers = true;
        Servers.ReplaceRange(models);

        // Update the ItemsSource before SelectedItem so a collection reset does not clear the tray selection.
        SelectedServer = models.FirstOrDefault(it => it.ID == _config.IndexId) ?? new();
    }

    private void ServerSelectedChanged()
    {
        if (SelectedServer == null)
        {
            return;
        }
        if (SelectedServer.ID.IsNullOrEmpty())
        {
            return;
        }
        SetDefaultServerRequested.Publish(SelectedServer.ID);
    }

    public async Task<AvailabilityCheckResult?> TestServerAvailability()
    {
        var item = await ConfigHandler.GetDefaultServer(_config);
        if (item == null)
        {
            return null;
        }

        await TestServerAvailabilitySub(ResUI.Speedtesting);

        var result = await Task.Run(ConnectionHandler.RunAvailabilityCheck);
        var msg = string.Format(ResUI.TestMeOutput, result.Time, result.Ip);

        var ip = result.GetValidIp();
        if (ip.IsNotEmpty())
        {
            ProfileExManager.Instance.SetTestIpInfo(item.IndexId, ip);
        }
        if (result.Time > 0)
        {
            ProfileExManager.Instance.SetTestDelay(item.IndexId, result.Time);
        }

        NoticeManager.Instance.SendMessageEx(msg);
        await TestServerAvailabilitySub(msg);
        return result;
    }

    private async Task TestServerAvailabilitySub(string msg)
    {
        RxSchedulers.MainThreadScheduler.Schedule(() =>
        {
            _ = TestServerAvailabilityResult(msg);
        });
        await Task.CompletedTask;
    }

    public async Task TestServerAvailabilityResult(string msg)
    {
        RunningInfoDisplay = msg;
        await Task.CompletedTask;
    }

    #region System proxy and Routings

    private async Task SetListenerType(ESysProxyType type)
    {
        if (_config.SystemProxyItem.SysProxyType == type)
        {
            return;
        }
        _config.SystemProxyItem.SysProxyType = type;
        await ChangeSystemProxyAsync(type, true);
        NoticeManager.Instance.SendMessageEx($"{ResUI.TipChangeSystemProxy} - {_config.SystemProxyItem.SysProxyType}");

        SystemProxySelected = (int)_config.SystemProxyItem.SysProxyType;
        await ConfigHandler.SaveConfig(_config);
    }

    public async Task ChangeSystemProxyAsync(ESysProxyType type, bool blChange)
    {
        await SysProxyHandler.UpdateSysProxy(_config, false);

        BlSystemProxyClear = type == ESysProxyType.ForcedClear;
        BlSystemProxySet = type == ESysProxyType.ForcedChange;
        BlSystemProxyNothing = type == ESysProxyType.Unchanged;
        BlSystemProxyPac = type == ESysProxyType.Pac;

        if (blChange)
        {
            await DispatcherRefreshIconInteraction.HandleSafe(RxVoid.Default);
        }
    }

    public async Task RefreshRoutingsMenu()
    {
        var routings = await AppManager.Instance.RoutingItems();

        RoutingItems.ReplaceRange(routings);

        SelectedRouting = routings.FirstOrDefault(t => t.IsActive == true);
    }

    private async Task RoutingSelectedChangedAsync()
    {
        if (SelectedRouting == null)
        {
            return;
        }

        var item = await AppManager.Instance.GetRoutingItem(SelectedRouting?.Id);
        if (item is null)
        {
            return;
        }

        if (await ConfigHandler.SetDefaultRouting(_config, item) == 0)
        {
            NoticeManager.Instance.SendMessageEx(ResUI.TipChangeRouting);
            ReloadRequested.Publish();
            await DispatcherRefreshIconInteraction.HandleSafe(RxVoid.Default);
        }
    }

    private async Task DoSystemProxySelected()
    {
        if (_config.SystemProxyItem.SysProxyType == (ESysProxyType)SystemProxySelected)
        {
            return;
        }
        await SetListenerType((ESysProxyType)SystemProxySelected);
    }

    private async Task DoEnableTun()
    {
        if (_config.TunModeItem.EnableTun == EnableTun)
        {
            return;
        }

        _config.TunModeItem.EnableTun = EnableTun;

        if (EnableTun && AllowEnableTun() == false)
        {
            // When running as a non-administrator, reboot to administrator mode
            if (Utils.IsWindows())
            {
                _config.TunModeItem.EnableTun = false;
                await AppManager.Instance.RebootAsAdmin();
                return;
            }
            else
            {
                var password = await PasswordInputInteraction.HandleSafe(RxVoid.Default);
                if (password.IsNullOrEmpty())
                {
                    _config.TunModeItem.EnableTun = false;
                    return;
                }
            }
        }

        await ConfigHandler.SaveConfig(_config);
        ReloadRequested.Publish();
    }

    private bool AllowEnableTun()
    {
        if (Utils.IsWindows())
        {
            return Utils.IsAdministrator();
        }
        else if (Utils.IsLinux())
        {
            return AppManager.Instance.LinuxSudoPwd.IsNotEmpty();
        }
        else if (Utils.IsMacOS())
        {
            return AppManager.Instance.LinuxSudoPwd.IsNotEmpty();
        }
        return false;
    }

    #endregion System proxy and Routings

    #region UI

    public async Task InboundDisplayStatus()
    {
        StringBuilder sb = new();
        sb.Append($"[{EInboundProtocol.mixed}:{AppManager.Instance.GetLocalPort(EInboundProtocol.socks)}");
        if (_config.Inbound.First().SecondLocalPortEnabled)
        {
            sb.Append($",{AppManager.Instance.GetLocalPort(EInboundProtocol.socks2)}");
        }
        sb.Append(']');
        InboundDisplay = $"{ResUI.LabLocal}:{sb}";

        if (_config.Inbound.First().AllowLANConn)
        {
            var lan = _config.Inbound.First().NewPort4LAN
                ? $"[{EInboundProtocol.mixed}:{AppManager.Instance.GetLocalPort(EInboundProtocol.socks3)}]"
                : $"[{EInboundProtocol.mixed}:{AppManager.Instance.GetLocalPort(EInboundProtocol.socks)}]";
            InboundLanDisplay = $"{ResUI.LabLAN}:{lan}";
        }
        else
        {
            InboundLanDisplay = $"{ResUI.LabLAN}:{Global.None}";
        }
        await Task.CompletedTask;
    }

    private bool IsHomeConnected() => BlSystemProxySet || EnableTun;

    private async Task ResetHomeConnectionAsync()
    {
        _showFullIp = false;
        _rawIp = null;
        _country = null;
        _lastDown = 0;
        _lastUp = 0;
        _sessionUpKb = 0;
        _sessionDownKb = 0;
        _downWave.Clear();
        _upWave.Clear();
        HomeDownloadWave = [];
        HomeUploadWave = [];
        HomeWaveVersion++;
        await RefreshHomeLineAsync();
    }

    private async Task RefreshHomeLineAsync()
    {
        var generation = ++_lineGeneration;
        var connected = IsHomeConnected();
        HomeConnected = connected;
        _checking = true;
        PublishHomeLine(checking: true);

        try
        {
            if (connected)
            {
                await Task.Delay(700);
                if (generation != _lineGeneration || IsHomeConnected() != connected)
                {
                    return;
                }
            }

            IWebProxy? proxy = null;
            if (connected)
            {
                var port = AppManager.Instance.GetLocalPort(EInboundProtocol.socks);
                proxy = new WebProxy($"socks5://{Global.Loopback}:{port}");
            }

            var info = await ConnectionHandler.GetIPInfo(proxy);
            if (generation != _lineGeneration || IsHomeConnected() != connected)
            {
                return;
            }

            _rawIp = info?.Ip;
            _country = info?.Country;
        }
        finally
        {
            if (generation == _lineGeneration)
            {
                _checking = false;
                PublishHomeLine(checking: false);
            }
        }
    }

    private void ToggleHomeIp()
    {
        if (string.IsNullOrEmpty(_rawIp) || !IsHomeConnected())
        {
            _ = RefreshHomeLineAsync();
            return;
        }

        _showFullIp = !_showFullIp;
        PublishHomeLine(checking: false);
    }

    private async Task ToggleHomeConnection()
    {
        if (EnableTun)
        {
            EnableTun = false;
            return;
        }

        await SetListenerType(BlSystemProxySet ? ESysProxyType.ForcedClear : ESysProxyType.ForcedChange);
    }

    private void PublishHomeLine(bool checking)
    {
        var connected = HomeConnected;
        var name = HomeLineFormat.CountryName(_country);
        HomeLineLabel = connected
            ? (string.IsNullOrEmpty(name) ? ResUI.HomeLineVpn : FormatHomeText(ResUI.HomeLineVpnCountry, name))
            : ResUI.HomeLineDirect;
        HomeIpText = string.IsNullOrEmpty(_rawIp)
            ? (checking ? ResUI.HomeLineUnknown : ResUI.HomeLineUnavailable)
            : (connected && !_showFullIp ? HomeLineFormat.MaskIp(_rawIp) : _rawIp!);
        var code = CountryExtension.NormalizeCountryCode(_country);
        HomeFlagText = code ?? string.Empty;
        HomeHasFlag = code != null;
        HomeDownloadText = $"↓ {Utils.HumanFy(_lastDown)}/s";
        HomeUploadText = $"↑ {Utils.HumanFy(_lastUp)}/s";
        HomeTrafficText = FormatHomeText(ResUI.HomeTrafficSession, Utils.HumanFy(_sessionDownKb + _sessionUpKb));
    }

    private static string FormatHomeText(string? template, string value)
    {
        if (string.IsNullOrEmpty(template))
        {
            return value;
        }

        try
        {
            return string.Format(template, value);
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private void ApplyHomeSpeed(ServerSpeedItem update)
    {
        if (!IsHomeConnected())
        {
            return;
        }

        _lastDown = Math.Max(0, update.ProxyDown);
        _lastUp = Math.Max(0, update.ProxyUp);
        _sessionDownKb += _lastDown;
        _sessionUpKb += _lastUp;
        AppendWave(_downWave, _lastDown);
        AppendWave(_upWave, _lastUp);
        HomeDownloadWave = _downWave.ToArray();
        HomeUploadWave = _upWave.ToArray();
        HomeWaveVersion++;
        PublishHomeLine(checking: _checking);
    }

    private static void AppendWave(List<double> samples, long value)
    {
        samples.Add(value);
        if (samples.Count > 40)
        {
            samples.RemoveAt(0);
        }
    }

    public async Task UpdateStatistics(ServerSpeedItem update)
    {
        ApplyHomeSpeed(update);
        if (!_config.GuiItem.DisplayRealTimeSpeed)
        {
            return;
        }

        try
        {
            if (AppManager.Instance.IsRunningCore(ECoreType.sing_box))
            {
                SpeedProxyDisplay = string.Format(ResUI.SpeedDisplayText, EInboundProtocol.mixed, Utils.HumanFy(update.ProxyUp), Utils.HumanFy(update.ProxyDown));
                SpeedDirectDisplay = string.Empty;
            }
            else
            {
                SpeedProxyDisplay = string.Format(ResUI.SpeedDisplayText, Global.ProxyTag, Utils.HumanFy(update.ProxyUp), Utils.HumanFy(update.ProxyDown));
                SpeedDirectDisplay = string.Format(ResUI.SpeedDisplayText, Global.DirectTag, Utils.HumanFy(update.DirectUp), Utils.HumanFy(update.DirectDown));
            }
        }
        catch
        {
        }
        await Task.CompletedTask;
    }

    #endregion UI
}
