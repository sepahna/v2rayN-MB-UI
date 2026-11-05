using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace v2rayN.Desktop.Converters;

public class ActiveServerBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
        {
            return Brushes.Transparent;
        }

        return TryGetThemeBrush("SemiColorSecondaryLight", "SemiColorPrimaryLight")
            ?? new SolidColorBrush(Color.FromArgb(0x55, 0x21, 0x96, 0xF3));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    internal static IBrush? TryGetThemeBrush(params string[] keys)
    {
        var theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;
        foreach (var key in keys)
        {
            if (Application.Current?.TryGetResource(key, theme, out var resource) == true
                && resource is IBrush brush)
            {
                return brush;
            }
        }

        return null;
    }
}

public class ActiveServerFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.SemiBold : FontWeight.Normal;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class ActiveServerBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
        {
            return Brushes.Transparent;
        }

        return ActiveServerBackgroundConverter.TryGetThemeBrush("SemiColorPrimary")
            ?? new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class ActiveServerBorderThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new Thickness(4, 0, 0, 0) : new Thickness(0);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
