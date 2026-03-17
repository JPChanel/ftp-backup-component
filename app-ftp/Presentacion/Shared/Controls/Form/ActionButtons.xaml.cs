using MaterialDesignThemes.Wpf;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form;

public partial class ActionButtons : UserControl
{
    public static readonly DependencyProperty PrimaryCommandProperty =
        DependencyProperty.Register(nameof(PrimaryCommand), typeof(ICommand), typeof(ActionButtons), new PropertyMetadata(null));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(ActionButtons), new PropertyMetadata(null));

    public static readonly DependencyProperty PrimaryTextProperty =
        DependencyProperty.Register(nameof(PrimaryText), typeof(string), typeof(ActionButtons), new PropertyMetadata("Guardar"));

    public static readonly DependencyProperty CancelTextProperty =
        DependencyProperty.Register(nameof(CancelText), typeof(string), typeof(ActionButtons), new PropertyMetadata("Cancelar"));

    public static readonly DependencyProperty PrimaryIconKindProperty =
        DependencyProperty.Register(nameof(PrimaryIconKind), typeof(PackIconKind), typeof(ActionButtons),
            new PropertyMetadata(PackIconKind.ContentSave, OnPrimaryIconKindChanged));

    public static readonly DependencyProperty IsPrimaryEnabledProperty =
        DependencyProperty.Register(nameof(IsPrimaryEnabled), typeof(bool), typeof(ActionButtons), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowPrimaryProperty =
        DependencyProperty.Register(nameof(ShowPrimary), typeof(bool), typeof(ActionButtons), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowCancelProperty =
        DependencyProperty.Register(nameof(ShowCancel), typeof(bool), typeof(ActionButtons), new PropertyMetadata(true));

    public ICommand PrimaryCommand
    {
        get => (ICommand)GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public string PrimaryText
    {
        get => (string)GetValue(PrimaryTextProperty);
        set => SetValue(PrimaryTextProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public PackIconKind PrimaryIconKind
    {
        get => (PackIconKind)GetValue(PrimaryIconKindProperty);
        set => SetValue(PrimaryIconKindProperty, value);
    }

    public bool IsPrimaryEnabled
    {
        get => (bool)GetValue(IsPrimaryEnabledProperty);
        set => SetValue(IsPrimaryEnabledProperty, value);
    }

    public bool ShowPrimary
    {
        get => (bool)GetValue(ShowPrimaryProperty);
        set => SetValue(ShowPrimaryProperty, value);
    }

    public bool ShowCancel
    {
        get => (bool)GetValue(ShowCancelProperty);
        set => SetValue(ShowCancelProperty, value);
    }

    public ActionButtons()
    {
        InitializeComponent();
    }

    private static void OnPrimaryIconKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActionButtons buttons)
            buttons.PrimaryIcon.Kind = (PackIconKind)e.NewValue;
    }
}

