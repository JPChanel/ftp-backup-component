using System.Globalization;
using System.Windows.Data;

namespace app_ftp.Presentacion.Shared.Controls.DataTable
{
    public class InvertBooleanFunctionConverter : IValueConverter
    {
        private readonly Func<object?, bool> _disabledFunc;

        public InvertBooleanFunctionConverter(Func<object?, bool> disabledFunc)
        {
            _disabledFunc = disabledFunc;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Disabled retorna true cuando debe estar deshabilitado
            // IsEnabled necesita true cuando debe estar habilitado
            // Por eso invertimos el resultado
            return !_disabledFunc(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
