using System.Collections.ObjectModel;
using System.Windows.Markup;
using Orientation = System.Windows.Controls.Orientation;
using RadioButton = System.Windows.Controls.RadioButton;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

/// <summary>
/// Control de grupo de RadioButtons reutilizable con binding a enum o string
/// </summary>
[ContentProperty(nameof(Options))]
public partial class FormRadioGroup : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormRadioGroup), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(FormRadioGroup),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(nameof(Options), typeof(ObservableCollection<RadioOption>), typeof(FormRadioGroup),
            new PropertyMetadata(null));

    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(FormRadioGroup),
                   new PropertyMetadata(null)); // Genera un nombre Ãºnico por defecto

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(FormRadioGroup),
            new PropertyMetadata(Orientation.Horizontal));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FormRadioGroup),
            new PropertyMetadata(12.0));

    public static readonly DependencyProperty ItemMarginProperty =
        DependencyProperty.Register(nameof(ItemMargin), typeof(Thickness), typeof(FormRadioGroup),
            new PropertyMetadata(new Thickness(0, 0, 15, 0)));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormRadioGroup),
            new PropertyMetadata(true));

    /// <summary>
    /// Etiqueta descriptiva del grupo (opcional)
    /// </summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Valor seleccionado actual (puede ser enum o string)
    /// </summary>
    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// ColecciÃ³n de opciones para los RadioButtons
    /// </summary>
    public ObservableCollection<RadioOption> Options
    {
        get => (ObservableCollection<RadioOption>)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    /// <summary>
    /// Nombre del grupo para agrupar los RadioButtons (autogenerado si no se especifica)
    /// </summary>
    public string GroupName
    {
        get => (string)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    /// <summary>
    /// OrientaciÃ³n del grupo: Horizontal (por defecto) o Vertical
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// TamaÃ±o de fuente de los RadioButtons (por defecto 12)
    /// </summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Margen entre cada RadioButton (por defecto "0,0,15,0")
    /// </summary>
    public Thickness ItemMargin
    {
        get => (Thickness)GetValue(ItemMarginProperty);
        set => SetValue(ItemMarginProperty, value);
    }

    /// <summary>
    /// Indica si el grupo estÃ¡ habilitado
    /// </summary>
    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public FormRadioGroup()
    {
        InitializeComponent();
        Options = new ObservableCollection<RadioOption>();
        if (string.IsNullOrEmpty(GroupName))
        {
            GroupName = Guid.NewGuid().ToString();
        }
        // Suscribirse al cambio de Value para actualizar los RadioButtons
        Loaded += FormRadioGroup_Loaded;
    }

    private void FormRadioGroup_Loaded(object sender, RoutedEventArgs e)
    {
        // Sincronizar el estado inicial de los RadioButtons con Value
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SyncRadioButtons();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormRadioGroup control)
        {
            control.SyncRadioButtons();
        }
    }
    private bool _isSyncing = false;
    /// <summary>
    /// Evento cuando un RadioButton es seleccionado
    /// </summary>
    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing)
            return;
        if (sender is RadioButton radioButton && radioButton.Tag != null)
        {
            // Actualizar el Value con el valor del RadioButton marcado
            Value = radioButton.Tag;
        }
    }

    /// <summary>
    /// Sincroniza el estado de los RadioButtons con el Value actual
    /// </summary>
    private void SyncRadioButtons()
    {
        if (RadioItemsControl == null) return;
        // Si el generador de contenedores no estÃ¡ listo, esperar a que lo estÃ©
        if (RadioItemsControl.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            // Suscribirse al evento StatusChanged para sincronizar cuando estÃ© listo
            EventHandler handler = null;
            handler = (s, e) =>
            {
                if (RadioItemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    RadioItemsControl.ItemContainerGenerator.StatusChanged -= handler;
                    SyncRadioButtonsInternal();
                }
            };
            RadioItemsControl.ItemContainerGenerator.StatusChanged += handler;
            return;
        }

        SyncRadioButtonsInternal();

    }
    private void SyncRadioButtonsInternal()
    {
        if (RadioItemsControl == null) return;

        _isSyncing = true;
        try
        {
            foreach (var item in RadioItemsControl.Items)
            {
                var container = RadioItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container is ContentPresenter presenter)
                {
                    var radioButton = FindVisualChild<RadioButton>(presenter);
                    if (radioButton != null && radioButton.Tag != null)
                    {
                        var isChecked = AreValuesEqual(radioButton.Tag, Value);
                        radioButton.IsChecked = isChecked;
                    }
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }
    /// <summary>
    /// Compara dos valores considerando enums y strings
    /// </summary>
    private bool AreValuesEqual(object value1, object value2)
    {
        if (value1 == null || value2 == null)
            return false;

        // Para enums, comparar por nombre
        if (value1 is Enum enum1 && value2 is Enum enum2)
        {
            return enum1.ToString() == enum2.ToString();
        }

        // Para objetos, comparar por igualdad
        return value1.Equals(value2) || value1.ToString() == value2.ToString();
    }

    /// <summary>
    /// Encuentra un hijo visual de un tipo especÃ­fico
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}

/// <summary>
/// Clase que representa una opciÃ³n de RadioButton
/// </summary>
public class RadioOption
{
    /// <summary>
    /// Texto visible del RadioButton
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Valor asociado (puede ser enum, string, int, etc.)
    /// </summary>
    public object Value { get; set; } = string.Empty;
}

