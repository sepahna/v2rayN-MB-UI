using System.Windows.Media;

namespace v2rayN.Base;

public class WindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : class
{
    private const uint MonitorDefaultToNearest = 2;
    private const double EdgeMargin = 8;
    private bool _placed;
    private bool _fitting;

    public WindowBase()
    {
        SourceInitialized += (_, _) => EnsureWindowOnScreen(applySavedSize: true);
        ContentRendered += (_, _) => EnsureWindowOnScreen(applySavedSize: false);
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Normal)
            {
                Dispatcher.BeginInvoke(() => EnsureWindowOnScreen(applySavedSize: false));
            }
        };
        Loaded += OnLoaded;
    }

    protected virtual void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EnsureWindowOnScreen(applySavedSize: false);
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => EnsureWindowOnScreen(applySavedSize: false));
    }

    /// <summary>
    /// Keeps the title-bar buttons inside the monitor work area.
    /// A default 800px height on a short or scaled display otherwise opens above the screen.
    /// </summary>
    protected void EnsureWindowOnScreen(bool applySavedSize)
    {
        if (_fitting || WindowState != WindowState.Normal)
        {
            return;
        }

        try
        {
            _fitting = true;
            if (!TryGetWorkArea(out var workLeft, out var workTop, out var workWidth, out var workHeight))
            {
                var area = SystemParameters.WorkArea;
                workLeft = area.Left;
                workTop = area.Top;
                workWidth = area.Width;
                workHeight = area.Height;
            }

            if (workWidth < 200 || workHeight < 160)
            {
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;

            var maxWidth = Math.Max(320, workWidth - (EdgeMargin * 2));
            var maxHeight = Math.Max(240, workHeight - (EdgeMargin * 2));
            if (MinWidth > maxWidth)
            {
                MinWidth = maxWidth;
            }

            if (MinHeight > maxHeight)
            {
                MinHeight = maxHeight;
            }

            var width = Width;
            var height = Height;
            if (applySavedSize || !_placed)
            {
                var sizeItem = ConfigHandler.GetWindowSizeItem(AppManager.Instance.Config, GetType().Name);
                if (sizeItem != null && sizeItem.Width > 0 && sizeItem.Height > 0)
                {
                    width = sizeItem.Width;
                    height = sizeItem.Height;
                }
            }

            if (double.IsNaN(width) || width <= 0)
            {
                width = Math.Min(1100, maxWidth);
            }

            if (double.IsNaN(height) || height <= 0)
            {
                height = Math.Min(680, maxHeight);
            }

            width = Math.Min(width, maxWidth);
            height = Math.Min(height, maxHeight);
            Width = width;
            Height = height;

            var needsPlace = !_placed || applySavedSize || double.IsNaN(Left) || double.IsNaN(Top);
            if (needsPlace)
            {
                Left = workLeft + Math.Max(EdgeMargin, (workWidth - width) / 2);
                Top = workTop + Math.Max(EdgeMargin, (workHeight - height) / 2);
                _placed = true;
            }

            if (Left < workLeft)
            {
                Left = workLeft + EdgeMargin;
            }

            if (Top < workTop)
            {
                Top = workTop + EdgeMargin;
            }

            if (Left + Width > workLeft + workWidth)
            {
                Left = Math.Max(workLeft, workLeft + workWidth - Width);
            }

            if (Top + Height > workTop + workHeight)
            {
                Top = Math.Max(workTop, workTop + workHeight - Height);
            }

            // The caption must stay on the visible work area even when the window is almost as tall as the screen.
            if (Top < workTop)
            {
                Top = workTop;
            }

            if (Top + 32 > workTop + workHeight)
            {
                Top = workTop;
            }
        }
        catch
        {
            // Ignore layout failures during early window creation.
        }
        finally
        {
            _fitting = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        try
        {
            if (WindowState == WindowState.Normal)
            {
                ConfigHandler.SaveWindowSizeItem(AppManager.Instance.Config, GetType().Name, Width, Height);
            }
        }
        catch { }
    }

    private bool TryGetWorkArea(out double left, out double top, out double width, out double height)
    {
        left = top = width = height = 0;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var monitor = NativeMonitor.MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var info = new NativeMonitor.Info { cbSize = Marshal.SizeOf<NativeMonitor.Info>() };
        if (!NativeMonitor.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        var source = PresentationSource.FromVisual(this);
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var scaleX = Math.Abs(toDevice.M11) < 0.01 ? 1 : toDevice.M11;
        var scaleY = Math.Abs(toDevice.M22) < 0.01 ? 1 : toDevice.M22;

        left = info.rcWork.Left / scaleX;
        top = info.rcWork.Top / scaleY;
        width = (info.rcWork.Right - info.rcWork.Left) / scaleX;
        height = (info.rcWork.Bottom - info.rcWork.Top) / scaleY;
        return width > 1 && height > 1;
    }
}

internal static class NativeMonitor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct Info
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref Info lpmi);
}
