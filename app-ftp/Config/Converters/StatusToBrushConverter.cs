using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace app_ftp.Config.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = (value as string ?? string.Empty).ToUpperInvariant();
        var mode = (parameter as string ?? "Foreground").ToUpperInvariant();

        var isError = text.Contains("ERROR", StringComparison.Ordinal) || text.Contains("CANCEL", StringComparison.Ordinal);
        var isWarning = text.Contains("OMITIDO", StringComparison.Ordinal) || text.Contains("VALIDANDO", StringComparison.Ordinal);

        return mode switch
        {
            "BACKGROUND" => CreateBrush(isError ? "#3F1D24" : isWarning ? "#3F2A12" : "#0F3D2E"),
            "BORDER" => CreateBrush(isError ? "#B91C1C" : isWarning ? "#92400E" : "#14532D"),
            _ => CreateBrush(isError ? "#FCA5A5" : isWarning ? "#FCD34D" : "#86EFAC")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Brush CreateBrush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}
