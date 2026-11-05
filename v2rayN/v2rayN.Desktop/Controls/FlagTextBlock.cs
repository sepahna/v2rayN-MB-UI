using Avalonia.Controls.Documents;
using Avalonia.Layout;
using ServiceLib.Common;

namespace v2rayN.Desktop.Controls;

/// <summary>
/// Draws country flags as images so Windows does not fall back to two-letter glyphs.
/// </summary>
public class FlagTextBlock : TextBlock
{
    private static readonly Dictionary<string, Bitmap?> Images = new(StringComparer.OrdinalIgnoreCase);

    public static readonly StyledProperty<string?> FlagTextProperty =
        AvaloniaProperty.Register<FlagTextBlock, string?>(nameof(FlagText));

    public string? FlagText
    {
        get => GetValue(FlagTextProperty);
        set => SetValue(FlagTextProperty, value);
    }

    static FlagTextBlock()
    {
        FlagTextProperty.Changed.AddClassHandler<FlagTextBlock>((control, _) => control.Rebuild());
    }

    private void Rebuild()
    {
        Inlines?.Clear();
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

                    Inlines.Add(new InlineUIContainer
                    {
                        Child = new Image
                        {
                            Source = image,
                            Width = 20,
                            Height = 15,
                            Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 0, 6, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        BaselineAlignment = BaselineAlignment.Center
                    });
                    break;
                case FlagSegment.Text text when text.Value.Length > 0:
                    Inlines.Add(new Run(text.Value));
                    break;
            }
        }
    }

    private static Bitmap? GetImage(string countryCode)
    {
        lock (Images)
        {
            if (Images.TryGetValue(countryCode, out var cached))
            {
                return cached;
            }

            var bytes = CountryFlagCatalog.GetPng(countryCode);
            if (bytes == null)
            {
                Images[countryCode] = null;
                return null;
            }

            var bitmap = new Bitmap(new MemoryStream(bytes));
            Images[countryCode] = bitmap;
            return bitmap;
        }
    }
}
