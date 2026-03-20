using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

public partial class FormDateTimePicker : UserControl
{
    private bool _isUpdating = false;

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormDateTimePicker),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(DateTime?), typeof(FormDateTimePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DateValueProperty =
        DependencyProperty.Register(nameof(DateValue), typeof(DateTime?), typeof(FormDateTimePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateValueChanged));

    public static readonly DependencyProperty TimeValueProperty =
        DependencyProperty.Register(nameof(TimeValue), typeof(DateTime?), typeof(FormDateTimePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeValueChanged));

    public static readonly DependencyProperty RequiredProperty =
        DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormDateTimePicker),
            new PropertyMetadata(false, OnRequiredChanged));

    public static readonly DependencyProperty DisplayLabelProperty =
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormDateTimePicker),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormDateTimePicker),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsInputValidProperty =
        DependencyProperty.Register(nameof(IsInputValid), typeof(bool), typeof(FormDateTimePicker),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValidationStateChanged));

    public static readonly DependencyProperty ValidationMessageProperty =
        DependencyProperty.Register(nameof(ValidationMessage), typeof(string), typeof(FormDateTimePicker),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValidationStateChanged));

    public static readonly DependencyProperty HasValidationErrorProperty =
        DependencyProperty.Register(nameof(HasValidationError), typeof(bool), typeof(FormDateTimePicker),
            new PropertyMetadata(false));

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

    public DateTime? DateValue
    {
        get => (DateTime?)GetValue(DateValueProperty);
        set => SetValue(DateValueProperty, value);
    }

    public DateTime? TimeValue
    {
        get => (DateTime?)GetValue(TimeValueProperty);
        set => SetValue(TimeValueProperty, value);
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

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
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

    public bool HasValidationError
    {
        get => (bool)GetValue(HasValidationErrorProperty);
        private set => SetValue(HasValidationErrorProperty, value);
    }

    public FormDateTimePicker()
    {
        InitializeComponent();
        UpdateDisplayLabel();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control && !control._isUpdating)
        {
            control._isUpdating = true;
            var newValue = e.NewValue as DateTime?;

            if (newValue.HasValue)
            {
                control.DateValue = newValue.Value.Date;
                control.TimeValue = newValue.Value;
            }
            else
            {
                control.DateValue = null;
                control.TimeValue = null;
            }

            control._isUpdating = false;
        }
    }

    private static void OnDateValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control && !control._isUpdating)
        {
            control.UpdateValue();
        }
    }

    private static void OnTimeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control && !control._isUpdating)
        {
            control.UpdateValue();
        }
    }

    private void UpdateValue()
    {
        if (_isUpdating) return;

        _isUpdating = true;

        if (DateValue.HasValue)
        {
            var date = DateValue.Value.Date;
            var time = TimeValue ?? DateTime.MinValue;

            Value = new DateTime(
                date.Year,
                date.Month,
                date.Day,
                time.Hour,
                time.Minute,
                time.Second);
        }
        else
        {
            Value = null;
        }

        _isUpdating = false;
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control)
        {
            control.UpdateDisplayLabel();
        }
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control)
        {
            control.UpdateDisplayLabel();
        }
    }

    private static void OnValidationStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormDateTimePicker control)
        {
            control.HasValidationError = !control.IsInputValid && !string.IsNullOrWhiteSpace(control.ValidationMessage);
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
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

