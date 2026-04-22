using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace app_ftp.Presentacion.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainContentContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || MainContentScrollViewer is null)
        {
            return;
        }

        // If an inner ScrollViewer exists (DataGrid, editor panel, modal content), let it handle wheel.
        var source = e.OriginalSource as DependencyObject;
        if (FindParent<ScrollViewer>(source) is ScrollViewer innerScroll &&
            !ReferenceEquals(innerScroll, MainContentScrollViewer))
        {
            return;
        }

        MainContentScrollViewer.ScrollToVerticalOffset(MainContentScrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
            {
                return parent;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
