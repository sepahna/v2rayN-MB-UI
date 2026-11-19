using Avalonia.Controls.Shapes;

namespace v2rayN.Desktop.Views;

public partial class HomeLineView : ReactiveUserControl<StatusBarViewModel>
{
    private static readonly Color DownloadColor = Color.FromRgb(0x1B, 0x8A, 0x4A);
    private static readonly Color UploadColor = Color.FromRgb(0x7C, 0x4D, 0xFF);

    public HomeLineView()
    {
        InitializeComponent();
        btnPower.Background = new SolidColorBrush(Color.FromRgb(0xC4, 0x38, 0x38));

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.HomeLineLabel, v => v.txtLabel.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeIpText, v => v.txtIp.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeFlagText, v => v.txtFlag.FlagText).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeHasFlag, v => v.txtFlag.IsVisible).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeHasFlag, v => v.txtGlobe.IsVisible, value => !value).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeDownloadText, v => v.txtDownload.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeUploadText, v => v.txtUpload.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeTrafficText, v => v.txtTraffic.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeDownloadText, v => v.txtDownload.Foreground, _ => new SolidColorBrush(DownloadColor)).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.HomeUploadText, v => v.txtUpload.Foreground, _ => new SolidColorBrush(UploadColor)).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleHomeConnectionCmd, v => v.btnPower).DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.HomeConnected)
                .Subscribe(on => btnPower.Background = new SolidColorBrush(on ? DownloadColor : Color.FromRgb(0xC4, 0x38, 0x38)))
                .DisposeWith(disposables);

            this.WhenAnyValue(v => v.ViewModel.HomeWaveVersion)
                .Subscribe(_ => DrawWave())
                .DisposeWith(disposables);
        });

        txtIp.Tapped += (_, e) =>
        {
            e.Handled = true;
            ViewModel?.ToggleHomeIpCmd.Execute(RxVoid.Default).Subscribe();
        };
        wave.SizeChanged += (_, _) => DrawWave();
    }

    private void DrawWave()
    {
        wave.Children.Clear();
        if (ViewModel == null || wave.Bounds.Width < 2)
        {
            return;
        }

        var width = wave.Bounds.Width;
        var height = wave.Bounds.Height;
        wave.Children.Add(new Line
        {
            StartPoint = new Point(0, height - 1),
            EndPoint = new Point(width, height - 1),
            Stroke = new SolidColorBrush(Color.FromArgb(0x40, 0x88, 0x88, 0x88)),
            StrokeThickness = 1
        });
        AddSeries(ViewModel.HomeDownloadWave, width, height, DownloadColor);
        AddSeries(ViewModel.HomeUploadWave, width, height, UploadColor);
    }

    private void AddSeries(double[] values, double width, double height, Color color)
    {
        if (values.Length < 2)
        {
            return;
        }

        var max = 1d;
        foreach (var value in values)
        {
            if (value > max)
            {
                max = value;
            }
        }

        var step = width / (values.Length - 1);
        var points = new Point[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var y = height - 1 - (values[i] / max) * (height - 2);
            points[i] = new Point(i * step, y);
        }

        var line = new PathFigure { StartPoint = points[0], IsClosed = false };
        var fill = new PathFigure { StartPoint = new Point(0, height - 1), IsClosed = true };
        fill.Segments.Add(new LineSegment { Point = points[0] });
        for (var i = 0; i < points.Length - 1; i++)
        {
            var midX = (points[i].X + points[i + 1].X) / 2;
            line.Segments.Add(Curve(points[i], points[i + 1], midX));
            fill.Segments.Add(Curve(points[i], points[i + 1], midX));
        }

        fill.Segments.Add(new LineSegment { Point = new Point(width, height - 1) });

        wave.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Fill = new SolidColorBrush(Color.FromArgb(0x29, color.R, color.G, color.B)),
            Data = GeometryOf(fill)
        });
        wave.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 1.6,
            Data = GeometryOf(line)
        });
    }

    private static BezierSegment Curve(Point start, Point end, double midX)
    {
        return new BezierSegment
        {
            Point1 = new Point(midX, start.Y),
            Point2 = new Point(midX, end.Y),
            Point3 = end
        };
    }

    private static PathGeometry GeometryOf(PathFigure figure)
    {
        var figures = new PathFigures { figure };
        return new PathGeometry { Figures = figures };
    }
}
