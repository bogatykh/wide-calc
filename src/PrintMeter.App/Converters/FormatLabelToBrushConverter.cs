using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace PrintMeter.App.Converters;

public sealed class FormatLabelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string label)
        {
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x73, 0x78, 0x82));
        }

        return label switch
        {
            "A4" => new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0x80, 0x8C)),
            "A3" => new SolidColorBrush(Color.FromArgb(0xFF, 0x6F, 0x84, 0x96)),
            "A2" => new SolidColorBrush(Color.FromArgb(0xFF, 0x62, 0x87, 0xA2)),
            "A1" => new SolidColorBrush(Color.FromArgb(0xFF, 0x54, 0x8A, 0xAF)),
            "A0" => new SolidColorBrush(Color.FromArgb(0xFF, 0x46, 0x8C, 0xBD)),
            _ => new SolidColorBrush(Color.FromArgb(0xFF, 0x73, 0x78, 0x82)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
