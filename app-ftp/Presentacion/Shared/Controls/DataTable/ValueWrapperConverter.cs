using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace app_ftp.Presentacion.Shared.Controls.DataTable
{
    /// <summary>
    /// Convierte un valor simple en un objeto con propiedad Value para facilitar el binding genÃ©rico en DataTemplates.
    /// </summary>
    public class ValueWrapperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // parameter debe ser un Binding que indica la propiedad a obtener
            if (parameter is Binding binding && value != null)
            {
                var propertyName = binding.Path?.Path;
                if (!string.IsNullOrEmpty(propertyName))
                {
                    var prop = value.GetType().GetProperty(propertyName);
                    if (prop != null)
                    {
                        var propValue = prop.GetValue(value);
                        return new { Value = propValue };
                    }
                }
            }
            // Si no se puede envolver, devolver el valor tal cual
            return new { Value = value };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

