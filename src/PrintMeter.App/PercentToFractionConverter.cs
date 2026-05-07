using System.Globalization;
using Microsoft.Maui.Controls;

namespace PrintMeter.App;

public sealed class PercentToFractionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return Math.Clamp(d / 100.0, 0, 1);
        }

        return 0d;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
