using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PrintMeter.App.Converters;

public sealed class ZeroCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (targetType != typeof(Visibility) && targetType != typeof(object))
        {
            throw new ArgumentException("ZeroCountToVisibilityConverter can only convert to Visibility.", nameof(targetType));
        }

        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        throw new ArgumentException("ZeroCountToVisibilityConverter expects an int input value.", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
