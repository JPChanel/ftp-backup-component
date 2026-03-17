using System.Text.RegularExpressions;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

public enum FieldVariant
{
    Text,
    Number,
    Decimal,
    Email,
    Password,
    TextArea
}

public partial class FormField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormField),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(FormField),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty RequiredProperty =
        DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormField),
            new PropertyMetadata(false, OnRequiredChanged));

    public static readonly DependencyProperty DisplayLabelProperty =
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(FieldVariant), typeof(FormField),
            new PropertyMetadata(FieldVariant.Text, OnVariantChanged));

    public static readonly DependencyProperty HelperTextProperty =
        DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormField), new PropertyMetadata(true));

    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(FormField), new PropertyMetadata(0));

    public static readonly DependencyProperty CustomStyleProperty =
        DependencyProperty.Register(nameof(CustomStyle), typeof(Style), typeof(FormField),
            new PropertyMetadata(null, OnCustomStyleChanged));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(FormField),
            new PropertyMetadata(TextWrapping.NoWrap));

    public static readonly DependencyProperty AcceptsReturnProperty =
        DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static readonly DependencyProperty MinHeightProperty =
        DependencyProperty.Register(nameof(MinHeight), typeof(double), typeof(FormField), new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static readonly DependencyProperty ClearOnFocusProperty =
        DependencyProperty.Register(nameof(ClearOnFocus), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public string DisplayLabel
    {
        get => (string)GetValue(DisplayLabelProperty);
        private set => SetValue(DisplayLabelProperty, value);
    }

    public FieldVariant Variant
    {
        get => (FieldVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public string HelperText
    {
        get => (string)GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool ClearOnFocus
    {
        get => (bool)GetValue(ClearOnFocusProperty);
        set => SetValue(ClearOnFocusProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public Style CustomStyle
    {
        get => (Style)GetValue(CustomStyleProperty);
        set => SetValue(CustomStyleProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public bool AcceptsReturn
    {
        get => (bool)GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public new double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    private string _previousValue = string.Empty;

    public FormField()
    {
        InitializeComponent();
        TextBoxControl.PreviewTextInput += OnPreviewTextInput;
        TextBoxControl.GotFocus += OnTextBoxGotFocus;
        TextBoxControl.LostFocus += OnTextBoxLostFocus;
        UpdateDisplayLabel();

        // Asegurar que Value nunca sea null
        if (Value is null)
        {
            Value = string.Empty;
        }
    }

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (ClearOnFocus)
        {
            _previousValue = Value ?? string.Empty;
            TextBoxControl.SelectAll();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (ClearOnFocus)
        {
            // Si estÃ¡ vacÃ­o o solo espacios, restaurar el valor anterior
            if (string.IsNullOrWhiteSpace(Value))
            {
                Value = _previousValue;
            }
        }
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field && e.NewValue is FieldVariant variant)
        {
            switch (variant)
            {
                case FieldVariant.Password:
                    // TODO: Cambiar a PasswordBox si es necesario
                    break;
                case FieldVariant.TextArea:
                    field.TextWrapping = TextWrapping.Wrap;
                    field.AcceptsReturn = true;
                    field.MinHeight = 80;
                    break;
            }
        }
    }

    private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field && e.NewValue is Style style)
        {
            field.TextBoxControl.Style = style;
        }
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        bool isValid = true;
        string errorMessage = string.Empty;

        switch (Variant)
        {
            case FieldVariant.Number:
                isValid = IsNumericInput(e.Text);
                errorMessage = "Solo se permiten nÃºmeros";
                break;
            case FieldVariant.Decimal:
                isValid = IsDecimalInput(e.Text);
                errorMessage = "Solo se permiten nÃºmeros y punto decimal";
                break;
            case FieldVariant.Email:
                // ValidaciÃ³n bÃ¡sica, se puede mejorar
                break;
        }

        if (!isValid)
        {
            e.Handled = true;
            ShowErrorTooltip(errorMessage);
        }
    }

    private void ShowErrorTooltip(string message)
    {
        var tooltip = new System.Windows.Controls.ToolTip
        {
            Content = message,
            PlacementTarget = TextBoxControl,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            IsOpen = true,
            StaysOpen = false
        };

        // Ocultar despuÃ©s de 2 segundos
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (s, args) =>
        {
            tooltip.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    private bool IsNumericInput(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }

    private bool IsDecimalInput(string text)
    {
        // Permitir dÃ­gitos y punto decimal
        if (!Regex.IsMatch(text, @"^[0-9.]+$")) return false;

        // Verificar si ya existe un punto en el texto actual
        var currentText = TextBoxControl.Text;

        // Obtener la posiciÃ³n del cursor para saber dÃ³nde se insertarÃ¡ el texto
        var caretIndex = TextBoxControl.CaretIndex;
        var newText = currentText.Insert(caretIndex, text);

        // Validar que solo haya un punto decimal
        return newText.Count(c => c == '.') <= 1;
    }
}

