using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace app_ftp.Presentacion.Shared.Controls.PdfViewer;

/// <summary>
/// Ventana para previsualizar documentos PDF con opciones de guardar e imprimir
/// Utiliza WebView2 para mostrar el documento PDF directamente en la ventana
/// </summary>
public partial class PdfViewerWindow : Window
{
    private readonly PdfViewerViewModel _viewModel;
    private readonly byte[] _pdfBytes;

    public PdfViewerWindow(byte[] pdfBytes, string titulo = "Documento PDF", bool imprimirAutomatico = false)
    {
        InitializeComponent();

        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        _viewModel = new PdfViewerViewModel(this, pdfBytes, titulo, imprimirAutomatico);
        DataContext = _viewModel;

        Loaded += PdfViewerWindow_Loaded;
        Closed += PdfViewerWindow_Closed;
        KeyDown += PdfViewerWindow_KeyDown;
    }

    private async void PdfViewerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CargarPdfEnWebViewAsync(PdfWebView);
    }

    private void PdfViewerWindow_Closed(object? sender, EventArgs e)
    {
        // Limpiar archivo temporal del ViewModel
        _viewModel.LimpiarArchivoTemporal();
    }
    private void PdfViewerWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Delete)
        {
            Close();
        }
    }
}

/// <summary>
/// ViewModel para el visor de PDF
/// </summary>
public partial class PdfViewerViewModel : ObservableObject
{
    private readonly Window _window;
    private readonly byte[] _pdfBytes;
    private string? _tempPdfPath;

    private readonly bool _imprimirAutomatico;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool isDocumentLoaded;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public PdfViewerViewModel(Window window, byte[] pdfBytes, string titulo, bool imprimirAutomatico)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        title = titulo;
        _imprimirAutomatico = imprimirAutomatico;
    }

    /// <summary>
    /// Carga el PDF en el control WebView2
    /// </summary>
    public async Task CargarPdfEnWebViewAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
    {
        string tempPath = string.Empty;

        try
        {
            IsLoading = true;
            HasError = false;
            await webView.EnsureCoreWebView2Async(null);

            await Task.Run(() =>
            {
                tempPath = Path.Combine(Path.GetTempPath(), $"{Title}_{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempPath, _pdfBytes);
            });

            _tempPdfPath = tempPath;

            webView.Source = new Uri(tempPath);
            if (_imprimirAutomatico)
            {
                await webView.ExecuteScriptAsync("window.print();");
            }
            IsDocumentLoaded = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"No se pudo cargar el PDF: {ex.Message}";

            _window.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    _window,
                    $"Error al cargar el PDF: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Limpia el archivo temporal del PDF
    /// </summary>
    public void LimpiarArchivoTemporal()
    {
        if (!string.IsNullOrEmpty(_tempPdfPath) && File.Exists(_tempPdfPath))
        {
            try
            {
                File.Delete(_tempPdfPath);
            }
            catch
            {
            }
        }
    }
}

