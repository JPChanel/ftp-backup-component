using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using app_ftp.Presentacion.Models;
using app_ftp.Presentacion.Utilities;
using app_ftp.Services;
using MaterialDesignThemes.Wpf;

namespace app_ftp.Presentacion.Shared.Controls.Notifications;

public partial class EstNotificationHost : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty AlertServiceProperty = DependencyProperty.Register(
        nameof(AlertService),
        typeof(IAlertService),
        typeof(EstNotificationHost),
        new PropertyMetadata(null, OnAlertServiceChanged));

    private readonly DispatcherTimer _timer;
    private IAlertService? _subscribedAlertService;
    private bool _isOpen;
    private string _currentTitle = string.Empty;
    private string _currentMessage = string.Empty;
    private Brush _currentBackgroundBrush = Brushes.White;
    private Brush _currentBorderBrush = Brushes.Transparent;
    private Brush _currentForegroundBrush = Brushes.Black;
    private PackIconKind _currentIconKind = PackIconKind.InformationOutline;

    public EstNotificationHost()
    {
        InitializeComponent();

        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IAlertService? AlertService
    {
        get => (IAlertService?)GetValue(AlertServiceProperty);
        set => SetValue(AlertServiceProperty, value);
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (_isOpen == value)
            {
                return;
            }

            _isOpen = value;
            OnPropertyChanged(nameof(IsOpen));
        }
    }

    public string CurrentTitle
    {
        get => _currentTitle;
        private set
        {
            if (_currentTitle == value)
            {
                return;
            }

            _currentTitle = value;
            OnPropertyChanged(nameof(CurrentTitle));
        }
    }

    public string CurrentMessage
    {
        get => _currentMessage;
        private set
        {
            if (_currentMessage == value)
            {
                return;
            }

            _currentMessage = value;
            OnPropertyChanged(nameof(CurrentMessage));
        }
    }

    public Brush CurrentBackgroundBrush
    {
        get => _currentBackgroundBrush;
        private set
        {
            _currentBackgroundBrush = value;
            OnPropertyChanged(nameof(CurrentBackgroundBrush));
        }
    }

    public Brush CurrentBorderBrush
    {
        get => _currentBorderBrush;
        private set
        {
            _currentBorderBrush = value;
            OnPropertyChanged(nameof(CurrentBorderBrush));
        }
    }

    public Brush CurrentForegroundBrush
    {
        get => _currentForegroundBrush;
        private set
        {
            _currentForegroundBrush = value;
            OnPropertyChanged(nameof(CurrentForegroundBrush));
        }
    }

    public PackIconKind CurrentIconKind
    {
        get => _currentIconKind;
        private set
        {
            _currentIconKind = value;
            OnPropertyChanged(nameof(CurrentIconKind));
        }
    }

    private static void OnAlertServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EstNotificationHost host)
        {
            host.RefreshSubscription();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshSubscription();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unsubscribe();
    }

    private void AlertService_AlertRaised(object? sender, EstNotificationMessage e)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => AlertService_AlertRaised(sender, e));
            return;
        }

        CurrentTitle = e.Title;
        CurrentMessage = e.Message;
        CurrentIconKind = e.IconKind ?? AlertStyleHelper.GetDefaultIcon(e.Variant);

        var palette = AlertStyleHelper.ResolvePalette(e.Variant);
        CurrentBackgroundBrush = palette.Background;
        CurrentBorderBrush = palette.Border;
        CurrentForegroundBrush = palette.Foreground;

        IsOpen = true;
        _timer.Interval = e.Duration ?? TimeSpan.FromSeconds(4);
        _timer.Stop();
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        IsOpen = false;
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        IsOpen = false;
    }

    private void RefreshSubscription()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (ReferenceEquals(_subscribedAlertService, AlertService))
        {
            return;
        }

        Unsubscribe();

        if (AlertService is null)
        {
            return;
        }

        _subscribedAlertService = AlertService;
        _subscribedAlertService.AlertRaised += AlertService_AlertRaised;
    }

    private void Unsubscribe()
    {
        if (_subscribedAlertService is null)
        {
            return;
        }

        _subscribedAlertService.AlertRaised -= AlertService_AlertRaised;
        _subscribedAlertService = null;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
