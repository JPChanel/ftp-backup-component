using MaterialDesignThemes.Wpf;
using System.Globalization;
using System.Windows.Data;

namespace app_ftp.Presentacion.Shared.Controls.DataTable
{
    public class CellValueWithIconMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = values.Length > 1 && values[1] is PackIconKind kind ? kind : PackIconKind.None;
            var color = values.Length > 2 && values[2] is System.Windows.Media.Brush brush ? brush : null;
            return new CellValueWithIcon
            {
                Value = values.Length > 0 ? values[0] : null,
                Icon = icon,
                Color = color
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
