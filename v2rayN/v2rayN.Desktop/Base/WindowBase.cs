namespace v2rayN.Desktop.Base;

public class WindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : class
{
    public WindowBase()
    {
        Loaded += OnLoaded;
        Loaded += (s, e) =>
        {
            if (Owner != null && !ShowInTaskbar)
            {
                CanMinimize = false;
            }
        };
    }

    protected virtual void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EnsureWindowOnScreen(applySavedSize: true);
    }

    /// <summary>
    /// Keeps the title bar on the visible work area across monitors and DPI scales.
    /// </summary>
    protected void EnsureWindowOnScreen(bool applySavedSize)
    {
        try
        {
            if (WindowState != WindowState.Normal)
            {
                return;
            }

            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen is null)
            {
                return;
            }

            var scaling = screen.Scaling > 0 ? screen.Scaling : 1.0;
            var workingArea = screen.WorkingArea;
            const double margin = 8;
            var maxWidth = Math.Max(320, (workingArea.Width / scaling) - (margin * 2));
            var maxHeight = Math.Max(240, (workingArea.Height / scaling) - (margin * 2));

            double width = Width;
            double height = Height;

            if (applySavedSize)
            {
                var sizeItem = ConfigHandler.GetWindowSizeItem(AppManager.Instance.Config, GetType().Name);
                if (sizeItem is not null)
                {
                    width = Math.Min(sizeItem.Width, maxWidth);
                    height = Math.Min(sizeItem.Height, maxHeight);
                }
            }
            else
            {
                width = Math.Min(width, maxWidth);
                height = Math.Min(height, maxHeight);
            }

            Width = width;
            Height = height;

            var frameDiff = (FrameSize ?? ClientSize) - ClientSize;
            var totalWidth = (width + frameDiff.Width) * scaling;
            var totalHeight = (height + frameDiff.Height) * scaling;

            var x = workingArea.X + ((workingArea.Width - totalWidth) / 2);
            var y = workingArea.Y + ((workingArea.Height - totalHeight) / 2);

            if (x < workingArea.X + margin)
            {
                x = workingArea.X + margin;
            }

            if (y < workingArea.Y + margin)
            {
                y = workingArea.Y + margin;
            }

            if (x + totalWidth > workingArea.X + workingArea.Width - margin)
            {
                x = Math.Max(workingArea.X + margin, workingArea.X + workingArea.Width - totalWidth - margin);
            }

            if (y + totalHeight > workingArea.Y + workingArea.Height - margin)
            {
                y = Math.Max(workingArea.Y + margin, workingArea.Y + workingArea.Height - totalHeight - margin);
            }

            if (y < workingArea.Y + margin)
            {
                y = workingArea.Y + margin;
            }

            Position = new PixelPoint((int)Math.Round(x), (int)Math.Round(y));
        }
        catch
        {
            // Ignore layout failures during early window creation.
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
}
