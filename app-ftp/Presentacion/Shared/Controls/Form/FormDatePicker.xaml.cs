using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

public partial class FormDatePicker : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormDatePicker),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(DateTime?), typeof(FormDatePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty RequiredProperty =
        DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormDatePicker),
            new PropertyMetadata(false, OnRequiredChanged));

    public static readonly DependencyProperty DisplayLabelProperty =
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormDatePicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HelperTextProperty =
        DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormDatePicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty EffectiveHelperTextProperty =
        DependencyProperty.Register(nameof(EffectiveHelperText), typeof(string), typeof(FormDatePicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormDatePicker), new PropertyMetadata(true));

    public static readonly DependencyProperty CustomStyleProperty =
        DependencyProperty.Register(nameof(CustomStyle), typeof(Style), typeof(FormDatePicker),
            new PropertyMetadata(null, OnCustomStyleChanged));

    public static readonly DependencyProperty IsInputValidProperty =
        DependencyProperty.Register(nameof(IsInputValid), typeof(bool), typeof(FormDatePicker),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValidationStateChanged));

    public static readonly DependencyProperty ValidationMessageProperty =
        DependencyProperty.Register(nameof(ValidationMessage), typeof(string), typeof(FormDatePicker),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValidationStateChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public DateTime? Value
    {
        get => (DateTime?)GetValue(ValueProperty);
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

    public string HelperText
    {
        get => (string)GetValue(HelperTextProperty);
        set
        {
            SetValue(HelperTextProperty, value);
            UpdateEffectiveHelperText();
        }
    }

    public string EffectiveHelperText
    {
        get => (string)GetValue(EffectiveHelperTextProperty);
        private set => SetValue(EffectiveHelperTextProperty, value);
    }

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public Style CustomStyle
    {
        get => (Style)GetValue(CustomStyleProperty);
        set => SetValue(CustomStyleProperty, value);
    }

    public bool IsInputValid
    {
        get => (bool)GetValue(IsInputValidProperty);
        set => SetValue(IsInputValidProperty, value);
    }

    public string ValidationMessage
    {
        get => (string)GetValue(ValidationMessageProperty);
        set => SetValue(ValidationMessageProperty, value);
    }

    public FormDatePicker()
    {
        InitializeComponent();
        UpdateDisplayLabel();
        UpdateEffectiveHelperText();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDatePicker datePicker)
        {
            datePicker.UpdateDisplayLabel();
        }
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDatePicker datePicker)
        {
            datePicker.UpdateDisplayLabel();
        }
    }

    private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDatePicker datePicker && e.NewValue is Style style)
        {
            datePicker.DatePickerControl.Style = style;
        }
    }

    private static void OnValidationStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDatePicker datePicker)
        {
            datePicker.UpdateEffectiveHelperText();
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }

    private void UpdateEffectiveHelperText()
    {
        EffectiveHelperText = IsInputValid || string.IsNullOrWhiteSpace(ValidationMessage)
            ? HelperText
            : ValidationMessage;
    }

    private void DatePickerControl_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DatePickerControl.SelectedDate.HasValue)
        {
            IsInputValid = true;
            ValidationMessage = string.Empty;
        }
    }

    private void DatePickerControl_DateValidationError(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        IsInputValid = false;
        ValidationMessage = "Ingresa una fecha valida.";
        e.ThrowException = false;
    }
}

