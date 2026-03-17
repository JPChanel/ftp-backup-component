using System.Collections.ObjectModel;

namespace app_ftp.Presentacion.Shared.Controls.WeightDisplay;

/// <summary>
/// Panel contenedor que renderiza dinÃ¡micamente displays de balanzas
/// segÃºn la configuraciÃ³n
/// </summary>
public partial class WeightDisplaysPanel : System.Windows.Controls.UserControl
{
    public WeightDisplaysPanel()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// ColecciÃ³n de informaciÃ³n de balanzas a mostrar
    /// </summary>
    public static readonly DependencyProperty BalanzasInfoProperty =
        DependencyProperty.Register(
            nameof(BalanzasInfo),
            typeof(ObservableCollection<BalanzaDisplayInfo>),
            typeof(WeightDisplaysPanel),
            new PropertyMetadata(null));

    public ObservableCollection<BalanzaDisplayInfo>? BalanzasInfo
    {
        get => (ObservableCollection<BalanzaDisplayInfo>?)GetValue(BalanzasInfoProperty);
        set => SetValue(BalanzasInfoProperty, value);
    }

    #endregion
}

