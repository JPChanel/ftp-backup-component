using app_ftp.Presentacion.Shared.Controls.Form;
using MaterialDesignThemes.Wpf;

namespace app_ftp.Presentacion.Shared.Controls.DataTable;

/// <summary>
/// Define un botÃ³n de acciÃ³n para el header del DataTable
/// </summary>
public class HeaderActionDef
{
    /// <summary>
    /// Texto del botÃ³n
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Icono de Material Design
    /// </summary>
    public PackIconKind Icon { get; set; }

    /// <summary>
    /// Comando a ejecutar
    /// </summary>
    public ICommand Command { get; set; } = null!;

    /// <summary>
    /// Tooltip
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Variante del botÃ³n (Primary, Success, Warning, Danger, Custom, etc.)
    /// </summary>
    public ButtonVariant Variant { get; set; } = ButtonVariant.Custom;

    /// <summary>
    /// Si es true, usa estilo Outlined; si es false, usa estilo Filled/Raised
    /// </summary>
    public bool IsOutlined { get; set; } = false;

    /// <summary>
    /// Color de fondo personalizado (solo para Variant = Custom)
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Altura del botÃ³n
    /// </summary>
    public double Height { get; set; } = 36;

    /// <summary>
    /// Margen del botÃ³n
    /// </summary>
    public string Margin { get; set; } = "0,0,8,0";

    /// <summary>
    /// Si es true, muestra solo el icono sin fondo ni texto (estilo icono)
    /// </summary>
    public bool IsIconButton { get; set; } = false;

    /// <summary>
    /// FunciÃ³n que determina si el botÃ³n estÃ¡ deshabilitado
    /// </summary>
    public Func<bool>? IsDisabled { get; set; }
}

