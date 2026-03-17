using MaterialDesignThemes.Wpf;
using Brush = System.Windows.Media.Brush;


namespace app_ftp.Presentacion.Shared.Controls.DataTable;

public static class ColumnMetadata
{
    // 1. Propiedad Adjunta para el Icono (PackIconKind)
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached(
            "Icon",
            typeof(PackIconKind),
            typeof(ColumnMetadata),
            new PropertyMetadata(PackIconKind.None));

    public static PackIconKind GetIcon(DependencyObject obj)
    {
        return (PackIconKind)obj.GetValue(IconProperty);
    }

    public static void SetIcon(DependencyObject obj, PackIconKind value)
    {
        obj.SetValue(IconProperty, value);
    }

    // 2. Propiedad Adjunta para el Color (string, ej: "#FBC02D")
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.RegisterAttached(
            "Color",
            typeof(Brush),
            typeof(ColumnMetadata),
            new PropertyMetadata(null));

    public static Brush? GetColor(DependencyObject obj)
    {
        return (Brush?)obj.GetValue(ColorProperty);
    }

    public static void SetColor(DependencyObject obj, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                // Si el valor ya es un color hexadecimal válido, asegúrate de que tenga el #
                var colorString = value.Trim();
                if (!colorString.StartsWith("#"))
                    colorString = "#" + colorString;
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);
                obj.SetValue(ColorProperty, new SolidColorBrush(color));
            }
            catch
            {
                obj.SetValue(ColorProperty, new SolidColorBrush(Colors.Gray));
            }
        }
        else
        {
            obj.SetValue(ColorProperty, new SolidColorBrush(Colors.Gray));
        }
    }
}
