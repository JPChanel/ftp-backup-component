using MaterialDesignThemes.Wpf;

namespace app_ftp.Presentacion.Shared.Controls.DataTable;

/// <summary>
/// Define un botÃ³n de acciÃ³n para una columna de acciones en el DataTable
/// </summary>
public class DataTableActionButton
{
    // Constantes para dimensiones comunes
    public const double DefaultButtonWidth = 30;
    public const double DefaultButtonHeight = 30;
    public const double DefaultIconSize = 18;
    public const string DefaultMargin = "2,0";

    /// <summary>
    /// Icono del botÃ³n (Material Design)
    /// </summary>
    public PackIconKind Icon { get; set; }

    /// <summary>
    /// Texto del tooltip
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;

    /// <summary>
    /// Comando a ejecutar cuando se hace clic
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Color del botÃ³n (opcional, null para usar el color predeterminado)
    /// </summary>
    public System.Windows.Media.Brush? Foreground { get; set; }

    /// <summary>
    /// Ancho del botÃ³n
    /// </summary>
    public double Width { get; set; } = DefaultButtonWidth;

    /// <summary>
    /// Alto del botÃ³n
    /// </summary>
    public double Height { get; set; } = DefaultButtonHeight;

    /// <summary>
    /// TamaÃ±o del icono
    /// </summary>
    public double IconSize { get; set; } = DefaultIconSize;

    /// <summary>
    /// Margen del botÃ³n
    /// </summary>
    public string Margin { get; set; } = DefaultMargin;

    /// <summary>
    /// Visibilidad condicional (funciÃ³n que determina si el botÃ³n debe mostrarse)
    /// </summary>
    public Func<object?, bool>? IsVisible { get; set; }
    /// <summary>
    /// FunciÃ³n que determina si el botÃ³n estÃ¡ deshabilitado
    /// Retorna true si el botÃ³n debe estar deshabilitado
    /// </summary>
    public Func<object?, bool>? Disabled { get; set; }

}

