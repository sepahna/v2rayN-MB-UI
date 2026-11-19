using System.Windows.Media;
using System.Windows.Shapes;
using ShapePath = System.Windows.Shapes.Path;

namespace v2rayN.Views;

public partial class HomeLineView
{
    private static readonly Color DownloadColor = Color.FromRgb(0x1B, 0x8A, 0x4A);
    private static readonly Color UploadColor = Color.FromRgb(0x7C, 0x4D, 0xFF);

    public HomeLineView()
    {
        InitializeComponent();
        btnPower.Background = new SolidColorBrush(Color.FromRgb(0xC4, 0x38, 0x38));

        this.WhenActivated(disposables =>
        {
            try
            {
                this.OneWayBind(ViewModel, vm => vm.HomeLineLabel, v => v.txtLabel.Text).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeIpText, v => v.txtIp.Text).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeFlagText, v => v.txtFlag.FlagText).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeHasFlag, v => v.txtFlag.Visibility, value => value ? Visibility.Visible : Visibility.Collapsed).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeHasFlag, v => v.txtGlobe.Visibility, value => value ? Visibility.Collapsed : Visibility.Visible).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeDownloadText, v => v.txtDownload.Text).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeUploadText, v => v.txtUpload.Text).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeTrafficText, v => v.txtTraffic.Text).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeDownloadText, v => v.txtDownload.Foreground, _ => new SolidColorBrush(DownloadColor)).DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.HomeUploadText, v => v.txtUpload.Foreground, _ => new SolidColorBrush(UploadColor)).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.ToggleHomeConnectionCmd, v => v.btnPower).DisposeWith(disposables);
                txtIp.MouseLeftButtonUp += OnIpClick;

                IDisposable? connectedSub = null;
                IDisposable? waveSub = null;
                Disposable.Create(() =>
                {
                    connectedSub?.Dispose();
                    waveSub?.Dispose();
                }).DisposeWith(disposables);

                this.WhenAnyValue(v => v.ViewModel)
                    .Subscribe(vm =>
                    {
                        connectedSub?.Dispose();
                        waveSub?.Dispose();
                        connectedSub = null;
                        waveSub = null;
                        if (vm == null)
                        {
                            return;
                        }

                        connectedSub = vm.WhenAnyValue(x => x.HomeConnected).Subscribe(on =>
                        {
                            try
                            {
                                btnPower.Background = new SolidColorBrush(on ? DownloadColor : Color.FromRgb(0xC4, 0x38, 0x38));
                            }
                            catch (Exception ex)
                            {
                                Logging.SaveLog("HomeLineView", ex);
                            }
                        });
                        waveSub = vm.WhenAnyValue(x => x.HomeWaveVersion).Subscribe(_ => DrawWave());
                    })
                    .DisposeWith(disposables);
            }
            catch (Exception ex)
            {
                Logging.SaveLog("HomeLineView", ex);
            }
        });

        wave.SizeChanged += (_, _) => DrawWave();
    }

    private void OnIpClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ViewModel?.ToggleHomeIpCmd.Execute(RxVoid.Default).Subscribe();
    }

    private void DrawWave()
    {
        try
        {
            DrawWaveCore();
        }
        catch (Exception ex)
        {
            Logging.SaveLog("HomeLineView", ex);
        }
    }

    private void DrawWaveCore()
    {
        wave.Children.Clear();
        if (ViewModel == null || wave.ActualWidth < 2)
        {
            return;
        }

        var width = wave.ActualWidth;
        var height = wave.ActualHeight;
        wave.Children.Add(new Line
        {
            X1 = 0,
            X2 = width,
            Y1 = height - 1,
            Y2 = height - 1,
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

        var line = new PathFigure { StartPoint = points[0] };
        var fill = new PathFigure { StartPoint = new Point(0, height - 1) };
        fill.Segments.Add(new LineSegment(points[0], true));
        for (var i = 0; i < points.Length - 1; i++)
        {
            var midX = (points[i].X + points[i + 1].X) / 2;
            line.Segments.Add(new BezierSegment(new Point(midX, points[i].Y), new Point(midX, points[i + 1].Y), points[i + 1], true));
            fill.Segments.Add(new BezierSegment(new Point(midX, points[i].Y), new Point(midX, points[i + 1].Y), points[i + 1], true));
        }

        fill.Segments.Add(new LineSegment(new Point(width, height - 1), true));
        fill.IsClosed = true;

        wave.Children.Add(new ShapePath
        {
            Fill = new SolidColorBrush(Color.FromArgb(0x29, color.R, color.G, color.B)),
            Data = new PathGeometry(new[] { fill })
        });
        wave.Children.Add(new ShapePath
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 1.6,
            Data = new PathGeometry(new[] { line })
        });
    }
}
