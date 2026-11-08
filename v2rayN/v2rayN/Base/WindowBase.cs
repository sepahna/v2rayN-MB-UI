namespace v2rayN.Base;

public class WindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : class
{
    public WindowBase()
    {
        SourceInitialized += (_, _) => EnsureWindowOnScreen(applySavedSize: true);
        Loaded += OnLoaded;
    }

    protected virtual void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EnsureWindowOnScreen(applySavedSize: false);
    }

    /// <summary>
    /// Keeps the title bar (min/max/close) on the visible work area.
    /// Saved sizes from another monitor or DPI can otherwise push the caption off-screen.
    /// </summary>
    protected void EnsureWindowOnScreen(bool applySavedSize)
    {
        try
        {
            if (WindowState != WindowState.Normal)
            {
                return;
            }

            var work = SystemParameters.WorkArea;
            const double margin = 8;
            var maxWidth = Math.Max(MinWidth > 0 ? MinWidth : 320, work.Width - (margin * 2));
            var maxHeight = Math.Max(MinHeight > 0 ? MinHeight : 240, work.Height - (margin * 2));

            if (applySavedSize)
            {
                var sizeItem = ConfigHandler.GetWindowSizeItem(AppManager.Instance.Config, GetType().Name);
                if (sizeItem != null)
                {
                    Width = Math.Min(sizeItem.Width, maxWidth);
                    Height = Math.Min(sizeItem.Height, maxHeight);
                    Left = work.Left + ((work.Width - Width) / 2);
                    Top = work.Top + ((work.Height - Height) / 2);
                }
            }
            else
            {
                if (Width > maxWidth)
                {
                    Width = maxWidth;
                }

                if (Height > maxHeight)
                {
                    Height = maxHeight;
                }
            }

            if (double.IsNaN(Left) || double.IsNaN(Top))
            {
                Left = work.Left + ((work.Width - Width) / 2);
                Top = work.Top + ((work.Height - Height) / 2);
            }

            // Title-bar buttons must stay inside the work area.
            if (Left < work.Left)
            {
                Left = work.Left + margin;
            }

            if (Top < work.Top)
            {
                Top = work.Top + margin;
            }

            if (Left + Width > work.Right)
            {
                Left = Math.Max(work.Left + margin, work.Right - Width - margin);
            }

            if (Top + Height > work.Bottom)
            {
                Top = Math.Max(work.Top + margin, work.Bottom - Height - margin);
            }

            if (Top < work.Top)
            {
                Top = work.Top + margin;
            }
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
