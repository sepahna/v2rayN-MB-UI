using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ServiceLib.Common;

namespace v2rayN.Base;

/// <summary>
/// Draws country flags as images. Windows fonts show flag emoji as two letters.
/// </summary>
internal class FlagTextBlock : TextBlock
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BitmapImage?> Images = new(StringComparer.OrdinalIgnoreCase);

    public static readonly DependencyProperty FlagTextProperty = DependencyProperty.Register(
        nameof(FlagText),
        typeof(string),
        typeof(FlagTextBlock),
        new PropertyMetadata(string.Empty, (d, _) => ((FlagTextBlock)d).Rebuild()));

    public string? FlagText
    {
        get => (string?)GetValue(FlagTextProperty);
        set => SetValue(FlagTextProperty, value);
    }

    private void Rebuild()
    {
        Inlines.Clear();
        foreach (var segment in CountryExtension.SplitFlags(FlagText))
        {
            switch (segment)
            {
                case FlagSegment.Flag flag:
                    var image = GetImage(flag.CountryCode);
                    if (image == null)
                    {
                        Inlines.Add(new Run(flag.CountryCode.CountryToEmoji() ?? flag.CountryCode));
                        break;
                    }

                    Inlines.Add(new InlineUIContainer(new Image
                    {
                        Source = image,
                        Width = 20,
                        Height = 15,
                        Stretch = Stretch.Uniform,
                        SnapsToDevicePixels = true,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    })
                    {
                        BaselineAlignment = BaselineAlignment.Center
                    });
                    break;
                case FlagSegment.Text text when text.Value.Length > 0:
                    Inlines.Add(new Run(text.Value));
                    break;
            }
        }
    }

    private static BitmapImage? GetImage(string countryCode)
    {
        return Images.GetOrAdd(countryCode, static code =>
        {
            var bytes = CountryFlagCatalog.GetPng(code);
            if (bytes == null)
            {
                return null;
            }

            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        });
    }
}
