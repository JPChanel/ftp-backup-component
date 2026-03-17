using CommunityToolkit.Mvvm.ComponentModel;

namespace app_ftp.Presentacion.Shared.Controls.WeightDisplay;

/// <summary>
/// Modelo de informaciÃ³n para un display de balanza individual
/// Usado para binding dinÃ¡mico en WeightDisplayControl
/// </summary>
public partial class BalanzaDisplayInfo : ObservableObject
{
    /// <summary>
    /// Nombre de la balanza (ej: "B1-A", "B2-A")
    /// </summary>
    [ObservableProperty]
    private string nombre = string.Empty;

    /// <summary>
    /// Puerto COM de la balanza (ej: "COM3", "COM4")
    /// </summary>
    [ObservableProperty]
    private string puerto = string.Empty;

    /// <summary>
    /// Peso actual leÃ­do de la balanza
    /// </summary>
    [ObservableProperty]
    private decimal? pesoActual;

    /// <summary>
    /// Indica si la balanza estÃ¡ conectada
    /// </summary>
    [ObservableProperty]
    private bool conectada;

    /// <summary>
    /// Color del borde del display (para diferenciar visualmente)
    /// Valores sugeridos: "#4F46E5" (Ã­ndigo), "#10B981" (verde), "#F59E0B" (Ã¡mbar)
    /// </summary>
    [ObservableProperty]
    private string colorBorde = "#4F46E5";

    /// <summary>
    /// Comando para capturar el peso actual
    /// </summary>
    [ObservableProperty]
    private ICommand? capturarCommand;

    /// <summary>
    /// Indica si se debe mostrar el botÃ³n de captura
    /// </summary>
    [ObservableProperty]
    private bool mostrarBotonCaptura = true;

    /// <summary>
    /// Indica si el peso actual es estable (no estÃ¡ cambiando)
    /// </summary>
    [ObservableProperty]
    private bool esEstable = false;
}

