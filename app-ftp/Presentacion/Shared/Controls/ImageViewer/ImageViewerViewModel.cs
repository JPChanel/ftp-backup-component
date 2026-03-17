using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace app_ftp.Presentacion.Shared.Controls.ImageViewer;

/// <summary>
/// ViewModel para el visualizador de imÃ¡genes
/// Maneja la lÃ³gica de navegaciÃ³n, zoom y modos de visualizaciÃ³n
/// </summary>
public partial class ImageViewerViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _tituloSecundario;

    [ObservableProperty]
    private bool _mostrarPesaje = true;

    [ObservableProperty]
    private bool _mostrarDestare;

    [ObservableProperty]
    private bool _modoCarrusel = true;

    [ObservableProperty]
    private bool _modoGrilla;

    [ObservableProperty]
    private BitmapImage? _imagenActual;

    [ObservableProperty]
    private int _indiceActual = 1;

    [ObservableProperty]
    private int _totalImagenes;

    [ObservableProperty]
    private bool _puedeIrAnterior;

    [ObservableProperty]
    private bool _puedeIrSiguiente;

    [ObservableProperty]
    private bool _tieneImagenesDestare;

    [ObservableProperty]
    private bool _cargandoImagen;

    [ObservableProperty]
    private ObservableCollection<BitmapImage> _imagenesActuales = new();

    [ObservableProperty]
    private double _escalaZoom = 1.0;

    public bool MostrarScrollBars => EscalaZoom > 1.0;

    private readonly List<BitmapImage> _imagenesPesaje;
    private readonly List<BitmapImage> _imagenesDestare;
    private int _indiceActualInterno;
    private const double ZoomIncrement = 0.2;
    private const double ZoomMin = 0.5;
    private const double ZoomMax = 5.0;

    public Action? CerrarVentanaAction { get; set; }
    public Action? ToggleFullscreenAction { get; set; }

    public ImageViewerViewModel(
        List<BitmapImage> imagenesPesaje,
        List<BitmapImage>? imagenesDestare = null,
        string? tituloSecundario = null)
    {
        _imagenesPesaje = imagenesPesaje ?? new List<BitmapImage>();
        _imagenesDestare = imagenesDestare ?? new List<BitmapImage>();
        TituloSecundario = tituloSecundario ?? "Capturas de cÃ¡mara";
        TieneImagenesDestare = _imagenesDestare.Any();

        CargarImagenesIniciales();
    }

    private void CargarImagenesIniciales()
    {
        ImagenesActuales = new ObservableCollection<BitmapImage>(_imagenesPesaje);
        TotalImagenes = _imagenesPesaje.Count;

        if (TotalImagenes > 0)
        {
            _indiceActualInterno = 0;
            IndiceActual = 1;
            ImagenActual = _imagenesPesaje[0];
            ActualizarBotonesNavegacion();
        }
    }

    partial void OnMostrarPesajeChanged(bool value)
    {
        if (value)
        {
            MostrarDestare = false;
            ImagenesActuales = new ObservableCollection<BitmapImage>(_imagenesPesaje);
            TotalImagenes = _imagenesPesaje.Count;
            _indiceActualInterno = 0;
            IndiceActual = 1;
            if (_imagenesPesaje.Any())
            {
                ImagenActual = _imagenesPesaje[0];
            }
            ActualizarBotonesNavegacion();
        }
    }

    partial void OnMostrarDestareChanged(bool value)
    {
        if (value)
        {
            MostrarPesaje = false;
            ImagenesActuales = new ObservableCollection<BitmapImage>(_imagenesDestare);
            TotalImagenes = _imagenesDestare.Count;
            _indiceActualInterno = 0;
            IndiceActual = 1;
            if (_imagenesDestare.Any())
            {
                ImagenActual = _imagenesDestare[0];
            }
            ActualizarBotonesNavegacion();
        }
    }

    partial void OnModoCarruselChanged(bool value)
    {
        if (value)
        {
            ModoGrilla = false;
        }
    }

    partial void OnModoGrillaChanged(bool value)
    {
        if (value)
        {
            ModoCarrusel = false;
        }
    }

    [RelayCommand]
    private void ImagenAnterior()
    {
        var listaActual = MostrarPesaje ? _imagenesPesaje : _imagenesDestare;
        if (listaActual.Count == 0) return;

        // Carrusel infinito: si estÃ¡ en la primera, ir a la Ãºltima
        if (_indiceActualInterno > 0)
        {
            _indiceActualInterno--;
        }
        else
        {
            _indiceActualInterno = listaActual.Count - 1;
        }

        IndiceActual = _indiceActualInterno + 1;
        ImagenActual = listaActual[_indiceActualInterno];
        ActualizarBotonesNavegacion();
    }

    [RelayCommand]
    private void ImagenSiguiente()
    {
        var listaActual = MostrarPesaje ? _imagenesPesaje : _imagenesDestare;
        if (listaActual.Count == 0) return;

        // Carrusel infinito: si estÃ¡ en la Ãºltima, ir a la primera
        if (_indiceActualInterno < listaActual.Count - 1)
        {
            _indiceActualInterno++;
        }
        else
        {
            _indiceActualInterno = 0;
        }

        IndiceActual = _indiceActualInterno + 1;
        ImagenActual = listaActual[_indiceActualInterno];
        ActualizarBotonesNavegacion();
    }

    [RelayCommand]
    private void ZoomIn()
    {
        if (EscalaZoom < ZoomMax)
        {
            EscalaZoom = Math.Min(EscalaZoom + ZoomIncrement, ZoomMax);
            OnPropertyChanged(nameof(MostrarScrollBars));
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (EscalaZoom > ZoomMin)
        {
            EscalaZoom = Math.Max(EscalaZoom - ZoomIncrement, ZoomMin);
            OnPropertyChanged(nameof(MostrarScrollBars));
        }
    }

    [RelayCommand]
    private void ZoomReset()
    {
        EscalaZoom = 1.0;
        OnPropertyChanged(nameof(MostrarScrollBars));
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        ToggleFullscreenAction?.Invoke();
    }

    [RelayCommand]
    private void Cerrar()
    {
        CerrarVentanaAction?.Invoke();
    }

    private void ActualizarBotonesNavegacion()
    {
        var listaActual = MostrarPesaje ? _imagenesPesaje : _imagenesDestare;
        // Carrusel infinito: siempre permitir navegaciÃ³n si hay imÃ¡genes
        PuedeIrAnterior = listaActual.Count > 0;
        PuedeIrSiguiente = listaActual.Count > 0;

        // Restablecer zoom al cambiar de imagen
        EscalaZoom = 1.0;
        OnPropertyChanged(nameof(MostrarScrollBars));
    }

    /// <summary>
    /// Selecciona una imagen desde la vista de cuadrÃ­cula
    /// </summary>
    public void SeleccionarImagen(BitmapImage imagen)
    {
        var listaActual = MostrarPesaje ? _imagenesPesaje : _imagenesDestare;
        _indiceActualInterno = listaActual.IndexOf(imagen);

        if (_indiceActualInterno >= 0)
        {
            IndiceActual = _indiceActualInterno + 1;
            ImagenActual = imagen;

            // Cambiar a modo carrusel al seleccionar una imagen
            ModoCarrusel = true;
            ActualizarBotonesNavegacion();
        }
    }
}

