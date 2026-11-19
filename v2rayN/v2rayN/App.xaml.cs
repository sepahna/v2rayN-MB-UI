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

        if (!AppManager.Instance.InitApp())
        {
            UI.Show($"Loading GUI configuration file is abnormal,please restart the application{Environment.NewLine}加载GUI配置文件异常,请重启应用");
            Environment.Exit(0);
            return;
        }

        AppManager.Instance.WindowDialog = new WindowDialog();

        AppManager.Instance.InitComponents();

        RxAppBuilder.CreateReactiveUIBuilder()
            .WithWpf()
            .BuildApp();

        base.OnStartup(e);

        var mainWindowViewModel = new MainWindowViewModel();
        var viewFor = SimpleViewLocator.Instance.ResolveView(mainWindowViewModel);
        viewFor!.ViewModel = mainWindowViewModel;

        var mainWindow = (MainWindow)viewFor;
        MainWindow = mainWindow;
        try
        {
            // Show the shell before any home-card work. A later UI exception must not
            // leave the process running with neither a window nor a taskbar button.
            mainWindow.ShowInTaskbar = true;
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Activate();
            _startupShellReady = true;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("OnStartup", ex);
            ShowMainWindow();
            _startupShellReady = true;
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logging.SaveLog("App_DispatcherUnhandledException", e.Exception);
        e.Handled = true;
        if (!_startupShellReady)
        {
            ShowMainWindow();
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
            window.ShowInTaskbar = true;
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            if (!window.IsVisible)
            {
                window.Show();
            }

            window.Activate();
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
