using MaterialDesignThemes.Wpf;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

public enum ButtonVariant
{
    Primary,      // BotÃ³n principal azul
    Success,      // Verde - para acciones positivas (guardar, agregar)
    Warning,      // Naranja - para advertencias
    Danger,       // Rojo - para acciones destructivas
    Info,         // Cyan - para informaciÃ³n
    Secondary,    // Outlined/secundario
    Add,          // Variante especÃ­fica para agregar
    Save,         // Variante especÃ­fica para guardar
    Search,       // Variante especÃ­fica para buscar
    Close,        // Variante especÃ­fica para cerrar
    Delete,       // Variante especÃ­fica para eliminar
    Edit,         // Variante especÃ­fica para editar
    Print,        // Variante especÃ­fica para imprimir
    Custom        // Personalizado
}

public partial class CustomButton : UserControl
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(CustomButton), new PropertyMetadata("Button"));

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind), typeof(CustomButton), new PropertyMetadata(PackIconKind.Check));

    public static readonly DependencyProperty ButtonStyleProperty =
        DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty TextColorProperty =
        DependencyProperty.Register(nameof(TextColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register(nameof(BorderColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty BorderThicknessValueProperty =
        DependencyProperty.Register(nameof(BorderThicknessValue), typeof(Thickness), typeof(CustomButton), new PropertyMetadata(new Thickness(0)));

    public static readonly DependencyProperty HeightProperty =
        DependencyProperty.Register(nameof(Height), typeof(double), typeof(CustomButton), new PropertyMetadata(36.0));

    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(CustomButton), new PropertyMetadata(new Thickness(16, 8, 16, 8)));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(CustomButton), new PropertyMetadata(5.0));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(CustomButton), new PropertyMetadata(18.0));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(CustomButton), new PropertyMetadata(13.0));

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(CustomButton), new PropertyMetadata(FontWeights.Medium));

    public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(CustomButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(ButtonVariant), typeof(CustomButton),
            new PropertyMetadata(ButtonVariant.Primary, OnVariantChanged));

    public static readonly DependencyProperty IsOutlinedProperty =
        DependencyProperty.Register(nameof(IsOutlined), typeof(bool), typeof(CustomButton),
            new PropertyMetadata(false, OnIsOutlinedChanged));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public Style ButtonStyle
    {
        get => (Style)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public Brush BackgroundColor
    {
        get => (Brush)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public Brush TextColor
    {
        get => (Brush)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public Brush BorderColor
    {
        get => (Brush)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public Thickness BorderThicknessValue
    {
        get => (Thickness)GetValue(BorderThicknessValueProperty);
        set => SetValue(BorderThicknessValueProperty, value);
    }

    public new double Height
    {
        get => (double)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public new Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public new double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public string ToolTipText
    {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsOutlined
    {
        get => (bool)GetValue(IsOutlinedProperty);
        set => SetValue(IsOutlinedProperty, value);
    }

    public CustomButton()
    {
        InitializeComponent();
        ApplyVariant(Variant);
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomButton button)
        {
            button.ApplyVariant((ButtonVariant)e.NewValue);
        }
    }

    private static void OnIsOutlinedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomButton button)
        {
            button.ApplyVariant(button.Variant);
        }
    }

    private void ApplyVariant(ButtonVariant variant)
    {
        var primaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
        var successColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        var warningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
        var dangerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
        var infoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ACC1"));
        var defaultColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c757d"));
        var themeRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

        switch (variant)
        {
            case ButtonVariant.Primary:
                SetVariantDefaults("Aceptar", PackIconKind.Check, primaryColor, true);
                break;

            case ButtonVariant.Success:
                SetVariantDefaults("Guardar", PackIconKind.ContentSave, successColor, true);
                break;

            case ButtonVariant.Warning:
                SetVariantDefaults("Advertencia", PackIconKind.Alert, warningColor, true);
                break;

            case ButtonVariant.Danger:
                SetVariantDefaults("Eliminar", PackIconKind.Delete, dangerColor, true);
                break;

            case ButtonVariant.Info:
                SetVariantDefaults("InformaciÃ³n", PackIconKind.Information, infoColor, true);
                break;

            case ButtonVariant.Secondary:
                SetVariantDefaults("Cancelar", PackIconKind.Close, null, false);
                if (ButtonStyle == null)
                {
                    ButtonStyle = Application.Current.TryFindResource("MaterialDesignOutlinedButton") as Style;
                }
                break;

            case ButtonVariant.Add:
                SetVariantDefaults("Nuevo", PackIconKind.Plus, themeRed, true);
                break;

            case ButtonVariant.Save:
                SetVariantDefaults("Guardar", PackIconKind.ContentSave, primaryColor, true);
                break;

            case ButtonVariant.Search:
                SetVariantDefaults("Buscar", PackIconKind.Filter, primaryColor, true);
                break;

            case ButtonVariant.Close:
                SetVariantDefaults("Cerrar", PackIconKind.Close, dangerColor, true);
                break;

            case ButtonVariant.Delete:
                SetVariantDefaults("Eliminar", PackIconKind.Delete, dangerColor, true);
                break;

            case ButtonVariant.Edit:
                SetVariantDefaults("Editar", PackIconKind.Pencil, infoColor, true);
                break;

            case ButtonVariant.Print:
                SetVariantDefaults("Imprimir", PackIconKind.Printer, null, false);
                if (ButtonStyle == null)
                {
                    ButtonStyle = Application.Current.TryFindResource("MaterialDesignOutlinedButton") as Style;
                }
                break;

            case ButtonVariant.Custom:
                // Para Custom, siempre reaplicar el estilo segÃºn IsOutlined
                var customStyleName = IsOutlined ? "MaterialDesignOutlinedButton" : "MaterialDesignRaisedButton";
                ButtonStyle = Application.Current.TryFindResource(customStyleName) as Style;

                // Si es outlined y hay un color de fondo definido, usar ese color para borde/texto
                if (IsOutlined)
                {
                    // Buscar el color original en las propiedades
                    var bgColor = GetValue(BackgroundColorProperty) as Brush;
                    if (bgColor != null && bgColor != Brushes.Transparent)
                    {
                        TextColor = bgColor;
                        IconColor = bgColor;
                        BorderColor = bgColor;
                        BorderThicknessValue = new Thickness(1);
                        BackgroundColor = Brushes.Transparent;
                    }
                    else
                    {

                        TextColor = defaultColor;
                        IconColor = defaultColor;
                        BorderColor = defaultColor;
                        BorderThicknessValue = new Thickness(1);
                        BackgroundColor = Brushes.Transparent;
                    }

                    // Forzar el background transparent directamente en el botÃ³n despuÃ©s del estilo
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ButtonControl != null)
                            ButtonControl.Background = Brushes.Transparent;
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
                else
                {
                    // Para raised, sin borde visible pero con texto/icono blancos
                    BorderThicknessValue = new Thickness(0);
                    TextColor = Brushes.White;
                    IconColor = Brushes.White;
                }
                break;
        }
    }

    private void SetVariantDefaults(string text, PackIconKind icon, Brush backgroundColor, bool isRaised)
    {
        // Solo aplicar texto si no se ha sobrescrito manualmente
        if (string.IsNullOrEmpty(Text) || Text == "Button")
            Text = text;

        // Siempre aplicar el Ã­cono de la variante
        IconKind = icon;

        // Siempre reaplicar el estilo segÃºn IsOutlined PRIMERO
        var styleName = IsOutlined ? "MaterialDesignOutlinedButton" : (isRaised ? "MaterialDesignRaisedButton" : "MaterialDesignOutlinedButton");
        ButtonStyle = Application.Current.TryFindResource(styleName) as Style;

        // Ajustar colores segÃºn si es outlined o raised DESPUÃ‰S del estilo
        if (backgroundColor != null)
        {
            if (IsOutlined)
            {
                // Para outlined: el color de fondo se usa para texto/icono/borde
                TextColor = backgroundColor;
                IconColor = backgroundColor;
                BorderColor = backgroundColor;
                BorderThicknessValue = new Thickness(1);
                BackgroundColor = Brushes.Transparent;

                // Forzar el background transparent directamente en el botÃ³n despuÃ©s del estilo
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (ButtonControl != null)
                        ButtonControl.Background = Brushes.Transparent;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                // Para raised: color de fondo y texto/icono blancos, sin borde
                BackgroundColor = backgroundColor;
                BorderThicknessValue = new Thickness(0);
                TextColor = Brushes.White;
                IconColor = Brushes.White;
            }
        }
    }
}

