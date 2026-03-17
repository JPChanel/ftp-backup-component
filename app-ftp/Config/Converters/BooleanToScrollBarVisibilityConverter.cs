using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace app_ftp.Config.Converters;

public class BooleanToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enabled = value is bool flag && flag;
        return enabled ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ScrollBarVisibility visibility && visibility != ScrollBarVisibility.Disabled;
    }
}

