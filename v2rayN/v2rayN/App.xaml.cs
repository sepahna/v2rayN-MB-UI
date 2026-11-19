using v2rayN.Manager;
using v2rayN.Views;

namespace v2rayN;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static EventWaitHandle ProgramStarted;
    private bool _startupShellReady;
    private bool _restoringShell;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Open only one process
    /// </summary>
    /// <param name="e"></param>
    protected override void OnStartup(StartupEventArgs e)
    {
        var exePathKey = Utils.GetMd5(Utils.GetExePath());

        var rebootas = e.Args.Any(t => t == Global.RebootAs);
        ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, exePathKey, out var bCreatedNew);
        if (!rebootas && !bCreatedNew)
        {
            ProgramStarted.Set();
            Environment.Exit(0);
            return;
        }

        BootLog("startup");
        if (!AppManager.Instance.InitApp())
        {
            UI.Show($"Loading GUI configuration file is abnormal,please restart the application{Environment.NewLine}加载GUI配置文件异常,请重启应用");
            Environment.Exit(0);
            return;
        }

        AppManager.Instance.WindowDialog = new WindowDialog();

        BootLog("config loaded");
        AppManager.Instance.InitComponents();
        BootLog("components ready");

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithWpf()
            .BuildApp();

        base.OnStartup(e);

        MainWindowViewModel mainWindowViewModel;
        try
        {
            BootLog("creating main window");
            mainWindowViewModel = new MainWindowViewModel();
        }
        catch (Exception ex)
        {
            BootLog(ex.ToString());
            MessageBox.Show(ex.ToString(), "v2rayN startup");
            Shutdown();
            return;
        }

        var viewFor = SimpleViewLocator.Instance.ResolveView(mainWindowViewModel);
        viewFor!.ViewModel = mainWindowViewModel;

        var mainWindow = (MainWindow)viewFor;
        MainWindow = mainWindow;
        try
        {
            PlaceOnPrimaryScreen(mainWindow);
            mainWindow.ShowInTaskbar = true;
            mainWindow.Topmost = true;
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
            mainWindow.Topmost = false;
            _startupShellReady = true;
            BootLog("window shown");
        }
        catch (Exception ex)
        {
            BootLog(ex.ToString());
            Logging.SaveLog("OnStartup", ex);
            ShowMainWindow();
            _startupShellReady = true;
        }
    }

    private static void BootLog(string message)
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guiLogs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "startup.txt"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // The startup trace must never block the window.
        }
    }

    private static void PlaceOnPrimaryScreen(Window window)
    {
        var area = SystemParameters.WorkArea;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = Math.Min(1100, Math.Max(800, area.Width - 80));
        window.Height = Math.Min(720, Math.Max(500, area.Height - 80));
        window.Left = area.Left + Math.Max(0, (area.Width - window.Width) / 2);
        window.Top = area.Top + Math.Max(0, (area.Height - window.Height) / 2);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logging.SaveLog("App_DispatcherUnhandledException", e.Exception);
        BootLog(e.Exception.ToString());
        e.Handled = true;
        if (!_startupShellReady)
        {
            ShowMainWindow();
            if (MainWindow is not { IsVisible: true })
            {
                MessageBox.Show(e.Exception.ToString(), "v2rayN startup");
            }
        }
    }

    private void ShowMainWindow()
    {
        if (_restoringShell || MainWindow is not Window window)
        {
            return;
        }

        _restoringShell = true;
        try
        {
            PlaceOnPrimaryScreen(window);
            window.ShowInTaskbar = true;
            window.Topmost = true;
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            if (!window.IsVisible)
            {
                window.Show();
            }

            window.Activate();
            window.Topmost = false;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("ShowMainWindow", ex);
        }
        finally
        {
            _restoringShell = false;
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject != null)
        {
            Logging.SaveLog("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logging.SaveLog("TaskScheduler_UnobservedTaskException", e.Exception);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logging.SaveLog("OnExit");
        base.OnExit(e);
        Process.GetCurrentProcess().Kill();
    }
}
