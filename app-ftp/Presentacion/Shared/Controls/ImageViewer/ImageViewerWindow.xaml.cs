using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace app_ftp.Presentacion.Shared.Controls.ImageViewer;

/// <summary>
/// Ventana para visualizar imÃ¡genes con zoom y navegaciÃ³n
/// Componente reutilizable para mostrar imÃ¡genes de balanza u otros mÃ³dulos
/// </summary>
public partial class ImageViewerWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging;
    private ScrollViewer? _currentScrollViewer;

    public ImageViewerWindow(ImageViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Configurar eventos
        viewModel.CerrarVentanaAction = () => this.Close();
        viewModel.ToggleFullscreenAction = ToggleFullscreen;
    }

    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ImageViewerViewModel vm)
        {
            if (e.ClickCount == 2)
            {
                // Doble click para pantalla completa
                vm.ToggleFullscreenCommand.Execute(null);
            }
            else if (vm.EscalaZoom > 1.0)
            {
                // Click simple con zoom activado = iniciar drag
                var image = sender as FrameworkElement;
                _currentScrollViewer = FindScrollViewer(image);

                if (_currentScrollViewer != null)
                {
                    _isDragging = true;
                    _lastMousePosition = e.GetPosition(_currentScrollViewer);
                    image?.CaptureMouse();

                    if (image != null)
                        image.Cursor = Cursors.SizeAll;
                }
            }
        }
    }

    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _currentScrollViewer != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(_currentScrollViewer);
            var delta = currentPosition - _lastMousePosition;

            // Mover el scroll en direcciÃ³n opuesta al movimiento del mouse
            _currentScrollViewer.ScrollToHorizontalOffset(_currentScrollViewer.HorizontalOffset - delta.X);
            _currentScrollViewer.ScrollToVerticalOffset(_currentScrollViewer.VerticalOffset - delta.Y);

            _lastMousePosition = currentPosition;
        }
    }

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            var image = sender as FrameworkElement;
            image?.ReleaseMouseCapture();

            if (image != null)
                image.Cursor = Cursors.Hand;

            _currentScrollViewer = null;
        }
    }

    private ScrollViewer? FindScrollViewer(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;

            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Solo hacer zoom si Ctrl estÃ¡ presionado
        if (Keyboard.Modifiers == ModifierKeys.Control && DataContext is ImageViewerViewModel vm)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                // Scroll hacia arriba = Zoom In
                vm.ZoomInCommand.Execute(null);
            }
            else
            {
                // Scroll hacia abajo = Zoom Out
                vm.ZoomOutCommand.Execute(null);
            }
        }
    }

    private void ImagenGrilla_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is System.Windows.Media.Imaging.BitmapImage imagen)
        {
            if (DataContext is ImageViewerViewModel vm)
            {
                vm.SeleccionarImagen(imagen);
            }
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }
}

