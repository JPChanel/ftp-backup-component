using System.Globalization;
using System.Windows.Data;

namespace app_ftp.Config.Converters;

public class ComboBoxValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
}

