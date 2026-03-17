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

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }
}

