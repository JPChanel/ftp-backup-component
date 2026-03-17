using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TextBox = System.Windows.Controls.TextBox;

namespace app_ftp.Presentacion.Shared.Controls.DataTable;

/// <summary>
/// Interfaz no genÃ©rica para acceder al ViewModel desde el control
/// </summary>
public interface IDataTableViewModel
{
    Func<object, string?, bool>? CustomFilter { get; set; }
    void ConfigureTotals(IEnumerable<string> propertyNames);
}

/// <summary>
/// Wrapper para agregar Ã­ndice y funcionalidad de expansiÃ³n a cada elemento
/// </summary>
public partial class IndexedItem<T> : ObservableObject
{
    [ObservableProperty]
    private int rowNumber;

    [ObservableProperty]
    private T item = default!;

    [ObservableProperty]
    private bool isExpanded;
}

/// <summary>
/// ViewModel base para manejar paginaciÃ³n, filtrado y ordenamiento de datos tabulares
/// Usa generics para trabajar con cualquier tipo de entidad
/// </summary>
/// <typeparam name="T">Tipo de entidad a mostrar en la tabla</typeparam>
public partial class DataTableViewModel<T> : ObservableObject, IDataTableViewModel where T : class
{
    // ColecciÃ³n completa de datos (sin filtrar ni paginar)
    private List<T> _allData = new();

    // ColecciÃ³n filtrada pero sin paginar
    private List<T> _filteredData = new();

    /// <summary>
    /// ColecciÃ³n observable de datos paginados con Ã­ndice (visible en la UI)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<IndexedItem<T>> _paginatedData = new();

    /// <summary>
    /// TÃ©rmino de bÃºsqueda global
    /// </summary>
    [ObservableProperty]
    private string? _searchTerm;

    /// <summary>
    /// PÃ¡gina actual (base 1)
    /// </summary>
    [ObservableProperty]
    private int _currentPage = 1;

    /// <summary>
    /// TamaÃ±o de pÃ¡gina
    /// </summary>
    [ObservableProperty]
    private int _pageSize = 50;

    /// <summary>
    /// Total de pÃ¡ginas
    /// </summary>
    [ObservableProperty]
    private int _totalPages = 1;

    /// <summary>
    /// Total de registros (despuÃ©s de filtrar)
    /// </summary>
    [ObservableProperty]
    private int _totalRecords;

    /// <summary>
    /// Total de registros sin filtrar
    /// </summary>
    [ObservableProperty]
    private int _totalAllRecords;

    /// <summary>
    /// Ãndice de inicio para la pÃ¡gina actual (para numeraciÃ³n)
    /// </summary>
    [ObservableProperty]
    private int _pageStartIndex;

    /// <summary>
    /// Elemento seleccionado (wrapper con Ã­ndice)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItemData))]
    private IndexedItem<T>? _selectedItem;

    /// <summary>
    /// Elemento seleccionado sin wrapper - acceso directo al dato
    /// Se actualiza automÃ¡ticamente cuando cambia SelectedItem
    /// </summary>
    public T? SelectedItemData => SelectedItem?.Item;

    /// <summary>
    /// Diccionario de totales por nombre de propiedad
    /// </summary>
    [ObservableProperty]
    private Dictionary<string, decimal> _columnTotals = new();

    /// <summary>
    /// Si se deben mostrar totales
    /// </summary>
    [ObservableProperty]
    private bool _showTotals = false;

    /// <summary>
    /// Opciones de tamaÃ±os de pÃ¡gina
    /// </summary>
    public List<int> PageSizeOptions { get; } = new() { 10, 25, 50, 100 };

    /// <summary>
    /// Predicado personalizado para filtrar datos
    /// </summary>
    public Func<T, string?, bool>? CustomFilter { get; set; }

    /// <summary>
    /// ImplementaciÃ³n explÃ­cita de la interfaz para CustomFilter (sin tipo genÃ©rico)
    /// </summary>
    Func<object, string?, bool>? IDataTableViewModel.CustomFilter
    {
        get => CustomFilter != null ? (obj, term) => obj is T item && CustomFilter(item, term) : null;
        set => CustomFilter = value != null ? (item, term) => value(item, term) : null;
    }

    /// <summary>
    /// Comandos de navegaciÃ³n
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage() => CurrentPage--;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage() => CurrentPage++;

    [RelayCommand]
    private void FirstPage() => CurrentPage = 1;

    [RelayCommand]
    private void LastPage() => CurrentPage = TotalPages;

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages && TotalPages > 0;

    /// <summary>
    /// Actualiza los datos completos y recalcula paginaciÃ³n
    /// </summary>
    public void SetData(IEnumerable<T> data)
    {
        _allData = data?.ToList() ?? new List<T>();
        TotalAllRecords = _allData.Count;
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Configura las columnas que deben mostrar totales
    /// </summary>
    public void ConfigureTotals(IEnumerable<string> propertyNames)
    {
        var propertyList = propertyNames?.ToList() ?? new List<string>();
        ShowTotals = propertyList.Any();

        if (ShowTotals)
        {
            CalculateTotals(propertyList);
        }
        else
        {
            // Limpiar totales si no hay columnas
            ColumnTotals.Clear();
        }
    }

    /// <summary>
    /// Calcula los totales de las columnas especificadas
    /// </summary>
    private void CalculateTotals(IEnumerable<string> propertyNames)
    {
        var newTotals = new Dictionary<string, decimal>();

        foreach (var propName in propertyNames)
        {
            decimal total = 0;
            foreach (var item in _filteredData)
            {
                var value = GetPropertyValueByPath(item, propName);
                if (value != null)
                {
                    if (decimal.TryParse(value.ToString(), out var numValue))
                    {
                        total += numValue;
                    }
                }
            }
            newTotals[propName] = total;
        }

        ColumnTotals = newTotals;
    }

    /// <summary>
    /// Obtiene el valor de una propiedad usando un path (ej: "Baz.baz_des")
    /// </summary>
    private object? GetPropertyValueByPath(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        var properties = propertyPath.Split('.');
        object? current = obj;

        foreach (var prop in properties)
        {
            if (current == null) return null;

            var propInfo = current.GetType().GetProperty(prop);
            if (propInfo == null) return null;

            current = propInfo.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// Aplica filtrado y paginaciÃ³n
    /// </summary>
    private void ApplyFilteringAndPaging()
    {
        // Aplicar filtro
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            _filteredData = _allData.ToList();
        }
        else
        {
            if (CustomFilter != null)
            {
                _filteredData = _allData.Where(item => CustomFilter(item, SearchTerm)).ToList();
            }
            else
            {
                // Filtro por defecto: buscar en todas las propiedades string
                _filteredData = _allData.Where(item =>
                {
                    var properties = typeof(T).GetProperties()
                        .Where(p => p.PropertyType == typeof(string) ||
                                    p.PropertyType == typeof(string));

                    return properties.Any(p =>
                    {
                        var value = p.GetValue(item)?.ToString();
                        return value?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false;
                    });
                }).ToList();
            }
        }

        var totalFiltered = _filteredData.Count;
        TotalPages = (int)Math.Ceiling((double)totalFiltered / PageSize);

        // Ajustar pÃ¡gina actual si es necesario
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }

        // Aplicar paginaciÃ³n
        var skip = (CurrentPage - 1) * PageSize;
        var pagedData = _filteredData.Skip(skip).Take(PageSize).ToList();

        // Actualizar TotalRecords con los registros de la pÃ¡gina actual
        TotalRecords = pagedData.Count;

        // Calcular el Ã­ndice de inicio para esta pÃ¡gina
        PageStartIndex = skip;

        PaginatedData.Clear();
        for (int i = 0; i < pagedData.Count; i++)
        {
            PaginatedData.Add(new IndexedItem<T>
            {
                RowNumber = skip + i + 1,
                Item = pagedData[i]
            });
        }

        // Recalcular totales si estÃ¡n habilitados
        if (ShowTotals && ColumnTotals.Any())
        {
            var propertyNames = ColumnTotals.Keys.ToList();
            CalculateTotals(propertyNames);
        }

        // Notificar cambios en comandos de navegaciÃ³n
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTermChanged(string? value)
    {
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }

    partial void OnCurrentPageChanged(int value)
    {
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Refresca los datos aplicando nuevamente filtros y paginaciÃ³n
    /// </summary>
    [RelayCommand]
    public void Refresh()
    {
        ApplyFilteringAndPaging();
    }

    /// <summary>
    /// Limpia el filtro de bÃºsqueda
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchTerm = string.Empty;
    }

    private void EditableTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }
    }
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

        // Allow numbers, decimal point, and negative sign
        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(fullText, @"^-?\d*\.?\d*$");
    }
}

