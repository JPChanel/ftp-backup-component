using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.WeightDisplay;

/// <summary>
/// UserControl para mostrar el peso de una balanza individual
/// </summary>
public partial class WeightDisplayControl : UserControl
{
    public WeightDisplayControl()
    {
        InitializeComponent();
        FontSize = 36;
        Focusable = true;
        FocusVisualStyle = null;
    }

    #region Dependency Properties

    /// <summary>
    /// Nombre de la balanza (ej: "B1-A", "B2-A")
    /// </summary>
    public static readonly DependencyProperty BalanzaNombreProperty =
        DependencyProperty.Register(
            nameof(BalanzaNombre),
            typeof(string),
            typeof(WeightDisplayControl),
            new PropertyMetadata("B1-A"));

    public string BalanzaNombre
    {
        get => (string)GetValue(BalanzaNombreProperty);
        set => SetValue(BalanzaNombreProperty, value);
    }

    /// <summary>
    /// Peso actual de la balanza
    /// </summary>
    public static readonly DependencyProperty PesoActualProperty =
        DependencyProperty.Register(
            nameof(PesoActual),
            typeof(decimal?),
            typeof(WeightDisplayControl),
            new PropertyMetadata(null));

    public decimal? PesoActual
    {
        get => (decimal?)GetValue(PesoActualProperty);
        set => SetValue(PesoActualProperty, value);
    }

    /// <summary>
    /// Comando para capturar el peso
    /// </summary>
    public static readonly DependencyProperty CapturarCommandProperty =
        DependencyProperty.Register(
            nameof(CapturarCommand),
            typeof(ICommand),
            typeof(WeightDisplayControl),
            new PropertyMetadata(null));

    public ICommand? CapturarCommand
    {
        get => (ICommand?)GetValue(CapturarCommandProperty);
        set => SetValue(CapturarCommandProperty, value);
    }

    /// <summary>
    /// Color del borde (formato hex: "#4F46E5")
    /// </summary>
    public static readonly DependencyProperty ColorBordeProperty =
        DependencyProperty.Register(
            nameof(ColorBorde),
            typeof(string),
            typeof(WeightDisplayControl),
            new PropertyMetadata("#4F46E5"));

    public string ColorBorde
    {
        get => (string)GetValue(ColorBordeProperty);
        set => SetValue(ColorBordeProperty, value);
    }

    /// <summary>
    /// Indica si la balanza estÃ¡ conectada
    /// </summary>
    public static readonly DependencyProperty ConectadaProperty =
        DependencyProperty.Register(
            nameof(Conectada),
            typeof(bool),
            typeof(WeightDisplayControl),
            new PropertyMetadata(true));

    public bool Conectada
    {
        get => (bool)GetValue(ConectadaProperty);
        set => SetValue(ConectadaProperty, value);
    }

    /// <summary>
    /// Indica si se debe mostrar el botÃ³n de captura
    /// </summary>
    public static readonly DependencyProperty MostrarBotonCapturaProperty =
        DependencyProperty.Register(
            nameof(MostrarBotonCaptura),
            typeof(bool),
            typeof(WeightDisplayControl),
            new PropertyMetadata(true));

    public bool MostrarBotonCaptura
    {
        get => (bool)GetValue(MostrarBotonCapturaProperty);
        set => SetValue(MostrarBotonCapturaProperty, value);
    }

    /// <summary>
    /// Indica si el peso actual es estable (no estÃ¡ cambiando)
    /// </summary>
    public static readonly DependencyProperty EsEstableProperty =
        DependencyProperty.Register(
            nameof(EsEstable),
            typeof(bool),
            typeof(WeightDisplayControl),
            new PropertyMetadata(false));

    public bool EsEstable
    {
        get => (bool)GetValue(EsEstableProperty);
        set => SetValue(EsEstableProperty, value);
    }

    #endregion
}

