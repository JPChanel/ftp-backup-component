using app_ftp.Presentacion.ViewModels;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Threading;

namespace app_ftp.Presentacion.Views;

public partial class DashboardView : UserControl
{
    private INotifyCollectionChanged? _currentEntries;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_currentEntries is not null)
        {
            _currentEntries.CollectionChanged -= OnEntriesCollectionChanged;
            _currentEntries = null;
        }

        if (e.NewValue is DashboardViewModel vm)
        {
            _currentEntries = vm.BackupConsoleEntries;
            _currentEntries.CollectionChanged += OnEntriesCollectionChanged;
        }
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || BackupConsoleList.Items.Count == 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var last = BackupConsoleList.Items[BackupConsoleList.Items.Count - 1];
            BackupConsoleList.ScrollIntoView(last);
        }));
    }
}
