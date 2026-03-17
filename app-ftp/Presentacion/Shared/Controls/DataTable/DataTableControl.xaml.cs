using app_ftp.Presentacion.Shared.Controls.Form;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ComboBox = System.Windows.Controls.ComboBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using StackPanel = System.Windows.Controls.StackPanel;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.DataTable;

/// <summary>
/// Control de tabla reutilizable con paginación, filtrado y columnas configurables
/// </summary>
public partial class DataTableControl : UserControl
{
    private double _currentWidth;
    private bool _isUpdatingColumns;
    private System.Windows.Threading.DispatcherTimer? _resizeTimer;

    public DataTableControl()
    {
        InitializeComponent();
        this.SizeChanged += DataTableControl_SizeChanged;
        this.Loaded += DataTableControl_Loaded;
        this.Unloaded += DataTableControl_Unloaded;
        _resizeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _resizeTimer.Tick += ResizeTimer_Tick;
    }

    /// <summary>
    /// Evento que se dispara cuando se intenta editar una celda
    /// Solo permite editar si IsEditing = true
    /// </summary>
    private void MainDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {

        if (e.Row.DataContext is IndexedItem<object> indexedItem)
        {
            var itemType = indexedItem.Item.GetType();
            var isEditingProperty = itemType.GetProperty("IsEditing");

            if (isEditingProperty != null)
            {
                var isEditing = isEditingProperty.GetValue(indexedItem.Item);

                if (isEditing is bool editing && !editing)
                {
                    e.Cancel = true;
                }
            }
        }
    }

    private void DataTableControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ActualWidth > 0)
        {
            _currentWidth = ActualWidth;
            UpdateColumnVisibility(ActualWidth);
        }
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewKeyDown -= Window_PreviewKeyDown;
            window.Closing -= Window_Closing;
            window.PreviewKeyDown += Window_PreviewKeyDown;
            window.Closing += Window_Closing;
        }
    }

    /// <summary>
    /// Evento cuando el control se descarga - limpiar el event handler de la ventana
    /// </summary>
    private void DataTableControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CleanupWindowEvents();
    }

    /// <summary>
    /// Limpia los event handlers de la ventana
    /// </summary>
    private void CleanupWindowEvents()
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewKeyDown -= Window_PreviewKeyDown;
            window.Closing -= Window_Closing;
        }
    }

    /// <summary>
    /// Evento cuando la ventana se esta cerrando - limpiar event handlers
    /// </summary>
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        CleanupWindowEvents();
    }

    /// <summary>
    /// Maneja el evento PreviewKeyDown a nivel de ventana para capturar F5 globalmente
    /// </summary>
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            if (!this.IsLoaded || !this.IsVisible)
            {
                return;
            }

            e.Handled = true;

            // IMPORTANTE: Usar ParentDataContext para acceder al ViewModel padre (no el DataTableViewModel)
            // El DataTableViewModel tiene RefreshCommand que solo refresca en memoria
            // El ViewModel padre tiene BuscarCommand/CargarCommand que consultan la base de datos
            var contextToSearch = ParentDataContext;

            if (contextToSearch != null)
            {
                // Buscar el comando en orden de prioridad (SOLO comandos que consultan BD)
                ICommand? reloadCmd = null;
                string? commandName = null;

                // 1. BuscarCommand - comando principal de bÃºsqueda
                var buscarProp = contextToSearch.GetType().GetProperty("BuscarCommand");
                if (buscarProp != null)
                {
                    reloadCmd = buscarProp.GetValue(contextToSearch) as ICommand;
                    if (reloadCmd != null) commandName = "BuscarCommand";
                }

                // 2. CargarCommand - alternativa para cargar datos
                if (reloadCmd == null)
                {
                    var cargarProp = contextToSearch.GetType().GetProperty("CargarCommand");
                    if (cargarProp != null)
                    {
                        reloadCmd = cargarProp.GetValue(contextToSearch) as ICommand;
                        if (reloadCmd != null) commandName = "CargarCommand";
                    }
                }

                // NO buscar RefreshCommand porque solo refresca en memoria
                // Ejecutar el comando si existe y puede ejecutarse
                if (reloadCmd != null && reloadCmd.CanExecute(null))
                {
                    reloadCmd.Execute(null);
                }
                else
                {
                }
            }
            else
            {
            }
        }
    }

    private void DataTableControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Usar debouncing para evitar mÃºltiples actualizaciones durante redimensionamiento
        if (e.WidthChanged && Math.Abs(e.NewSize.Width - _currentWidth) > 1)
        {
            _currentWidth = e.NewSize.Width;
            _resizeTimer?.Stop();
            _resizeTimer?.Start();
        }
    }

    private void ResizeTimer_Tick(object? sender, EventArgs e)
    {
        _resizeTimer?.Stop();

        // Usar Dispatcher para asegurar que el layout estÃ© completo
        Dispatcher.InvokeAsync(() =>
        {
            UpdateColumnVisibility(_currentWidth);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Actualiza la visibilidad de columnas segÃºn el ancho disponible
    /// </summary>
    private void UpdateColumnVisibility(double width)
    {
        if (Columns == null || MainDataGrid == null || MainDataGrid.Columns.Count == 0 || _isUpdatingColumns)
            return;

        _isUpdatingColumns = true;
        try
        {
            // Definir breakpoints
            bool isSmallScreen = width < 1000;
            bool isMediumScreen = width >= 1000 && width < 1400;
            bool hasHiddenColumns = false;

            // Verificar si hay columnas que pueden ocultarse (DisplayPriority > 1)
            bool hasExpandableColumns = Columns.Any(c => c.DisplayPriority > 1);

            // Primera pasada: calcular visibilidad sin considerar el expander aÃºn
            int columnOffset = 1; // Solo NÂ° por ahora

            for (int i = 0; i < Columns.Count; i++)
            {
                var config = Columns[i];
                int columnIndex = i + columnOffset;

                // Si hay expander, ajustar Ã­ndice
                if (HasExpanderColumn())
                    columnIndex++;

                if (columnIndex >= MainDataGrid.Columns.Count)
                    break;

                var column = MainDataGrid.Columns[columnIndex];

                // Determinar si la columna debe estar visible
                bool shouldBeVisible = config.DisplayPriority switch
                {
                    1 => true, // Siempre visible
                    2 => !isSmallScreen, // Ocultar en pantallas pequeÃ±as
                    3 => !isSmallScreen && !isMediumScreen, // Solo visible en pantallas grandes
                    _ => true
                };

                column.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;

                // Detectar si hay columnas ocultas (cualquiera con DisplayPriority > 1 que estÃ© oculta)
                if (!shouldBeVisible && config.DisplayPriority > 1)
                {
                    hasHiddenColumns = true;
                }
            }

            // Gestionar el expander dinÃ¡micamente: agregar o remover segÃºn sea necesario
            bool currentlyHasExpander = HasExpanderColumn();
            bool shouldHaveExpander = hasExpandableColumns && hasHiddenColumns;

            if (shouldHaveExpander && !currentlyHasExpander)
            {
                // Agregar el expander
                var expanderConfig = new DataTableColumn
                {
                    PropertyName = "IsExpanded",
                    Header = "",
                    Width = "80",
                    ColumnType = DataTableColumnType.Template,
                    TemplateKey = "ExpanderTemplate",
                    CanSort = false,
                    DisplayPriority = 1,
                    ShowInExpandedView = false
                };

                var expanderColumn = CreateTemplateColumn(expanderConfig);
                expanderColumn.Header = expanderConfig.Header;
                expanderColumn.Width = ParseWidth(expanderConfig.Width);
                expanderColumn.CanUserSort = expanderConfig.CanSort;
                MainDataGrid.Columns.Insert(1, expanderColumn);

                // Forzar actualización del layout
                MainDataGrid.UpdateLayout();

                // Regenerar totales para incluir el espacio del expander
                GenerateTotalsRow();
            }
            else if (!shouldHaveExpander && currentlyHasExpander)
            {
                // Remover el expander
                MainDataGrid.Columns.RemoveAt(1);

                // Forzar actualización del layout
                MainDataGrid.UpdateLayout();

                // Regenerar totales para remover el espacio del expander
                GenerateTotalsRow();
            }
            else if (currentlyHasExpander)
            {
                // Solo actualizar visibilidad del expander si ya existe
                var expanderColumn = MainDataGrid.Columns[1];
                expanderColumn.Visibility = hasHiddenColumns ? Visibility.Visible : Visibility.Collapsed;
            }

            // Forzar actualización de los detalles de fila cerrados si ya no hay columnas ocultas
            if (!hasHiddenColumns && DataContext is DataTableViewModel<object> viewModel)
            {
                foreach (var item in viewModel.PaginatedData)
                {
                    if (item.IsExpanded)
                    {
                        item.IsExpanded = false;
                    }
                }
            }

            // Actualizar fila de totales para que coincida con las columnas visibles
            if (!shouldHaveExpander || currentlyHasExpander == shouldHaveExpander)
            {
                // Solo actualizar si no acabamos de regenerar totales
                UpdateTotalsRowVisibility();
            }
        }
        finally
        {
            _isUpdatingColumns = false;
        }
    }

    /// <summary>
    /// Verifica si el DataGrid tiene una columna expander
    /// </summary>
    private bool HasExpanderColumn()
    {
        return MainDataGrid.Columns.Count > 1 &&
               MainDataGrid.Columns[1].Header?.ToString() == "" &&
               MainDataGrid.Columns[1].Width.Value == 80;
    }

    #region Dependency Properties

    /// <summary>
    /// Colección de columnas a mostrar
    /// </summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(ObservableCollection<DataTableColumn>),
            typeof(DataTableControl),
            new PropertyMetadata(null, OnColumnsChanged));

    public ObservableCollection<DataTableColumn> Columns
    {
        get => (ObservableCollection<DataTableColumn>)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// ItemsSource para los datos
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(System.Collections.IEnumerable),
            typeof(DataTableControl),
            new PropertyMetadata(null));

    public System.Collections.IEnumerable ItemsSource
    {
        get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Item seleccionado
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(DataTableControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Altura de las filas del DataGrid
    /// </summary>
    public static readonly DependencyProperty RowHeightProperty =
        DependencyProperty.Register(
            nameof(RowHeight),
            typeof(double),
            typeof(DataTableControl),
            new PropertyMetadata(48.0, OnRowHeightChanged));

    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    private static void OnRowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl control && control.MainDataGrid != null)
        {
            control.MainDataGrid.RowHeight = (double)e.NewValue;
        }
    }

    /// <summary>
    /// Colección de acciones personalizadas para el header
    /// </summary>
    public static readonly DependencyProperty HeaderActionsProperty =
        DependencyProperty.Register(
            nameof(HeaderActions),
            typeof(ObservableCollection<HeaderActionDef>),
            typeof(DataTableControl),
            new PropertyMetadata(null, OnHeaderActionsChanged));

    public ObservableCollection<HeaderActionDef> HeaderActions
    {
        get => (ObservableCollection<HeaderActionDef>)GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }

    private static void OnHeaderActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl control)
        {
            control.GenerateHeaderActions();
        }
    }

    /// <summary>
    /// Comando para recargar datos desde el servidor
    /// Si no se proporciona, el botón de reload usarÃ¡ RefreshCommand del ViewModel
    /// </summary>
    public static readonly DependencyProperty ReloadCommandProperty =
        DependencyProperty.Register(
            nameof(ReloadCommand),
            typeof(ICommand),
            typeof(DataTableControl),
            new PropertyMetadata(null));

    public ICommand? ReloadCommand
    {
        get => (ICommand?)GetValue(ReloadCommandProperty);
        set => SetValue(ReloadCommandProperty, value);
    }

    /// <summary>
    /// DataContext del ViewModel padre (usado cuando el DataContext del control es TableViewModel)
    /// Permite obtener comandos del ViewModel padre automÃ¡ticamente
    /// </summary>
    public static readonly DependencyProperty ParentDataContextProperty =
        DependencyProperty.Register(
            nameof(ParentDataContext),
            typeof(object),
            typeof(DataTableControl),
            new PropertyMetadata(null, OnParentDataContextChanged));

    public object? ParentDataContext
    {
        get => GetValue(ParentDataContextProperty);
        set => SetValue(ParentDataContextProperty, value);
    }

    private static void OnParentDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl control)
        {
            control.GenerateHeaderActions();
        }
    }

    #endregion

    /// <summary>
    /// Callback cuando cambian las columnas
    /// </summary>
    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl control)
        {
            control.GenerateColumns();
            control.GenerateTotalsRow();
            control.SetupRowDetailsTemplate();
            control.ConfigureAutomaticFeatures();
        }
    }

    /// <summary>
    /// Configura automÃ¡ticamente el filtro y los totales basÃ¡ndose en las columnas
    /// </summary>
    private void ConfigureAutomaticFeatures()
    {
        if (Columns == null || DataContext is not IDataTableViewModel viewModel)
            return;

        // 1. Configurar filtro automÃ¡tico para todas las columnas (excepto NÂ°, Acciones, IsExpanded)
        var filterableColumns = Columns
            .Where(c => c.PropertyName != "IsExpanded"
                     && c.PropertyName != "Acciones"
                     && c.ColumnType != DataTableColumnType.Actions
                     && c.ColumnType != DataTableColumnType.Template)
            .ToList();

        if (filterableColumns.Any())
        {
            viewModel.CustomFilter = (item, searchTerm) =>
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return true;

                var term = searchTerm.ToLower();
                var itemProperty = item.GetType().GetProperty("Item");
                var actualItem = itemProperty?.GetValue(item) ?? item;

                foreach (var column in filterableColumns)
                {
                    try
                    {
                        var value = GetPropertyValueByPath(actualItem, column.PropertyName);
                        if (value != null)
                        {
                            var stringValue = value.ToString()?.ToLower();
                            if (!string.IsNullOrEmpty(stringValue) && stringValue.Contains(term))
                                return true;
                        }
                    }
                    catch
                    {
                        // Ignorar errores de reflexión
                    }
                }

                return false;
            };
        }

        // 2. Configurar totales automÃ¡ticos para columnas con ShowTotal = true
        var totalColumns = Columns
            .Where(c => c.ShowTotal)
            .Select(c => c.PropertyName)
            .ToList();

        if (totalColumns.Any())
        {
            viewModel.ConfigureTotals(totalColumns);
        }
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
    /// Configura el template de detalles de fila con las columnas ocultas
    /// </summary>
    private void SetupRowDetailsTemplate()
    {
        if (Columns == null || MainDataGrid == null)
            return;

        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Application.Current.TryFindResource("MaterialDesignCardBackground"));
        factory.SetValue(Border.BorderBrushProperty, Application.Current.TryFindResource("MaterialDesignDivider"));
        factory.SetValue(Border.BorderThicknessProperty, new Thickness(0, 1, 0, 0));
        factory.SetValue(Border.PaddingProperty, new Thickness(60, 15, 15, 15));

        var gridFactory = new FrameworkElementFactory(typeof(Grid));

        // Definir columnas del grid (Label y Value)
        var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(200));
        gridFactory.AppendChild(col1);

        var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        gridFactory.AppendChild(col2);

        // Filtrar columnas que deben mostrarse en la vista expandida
        // Excluir Actions y Template (como iconos de estado)
        var expandableColumns = Columns.Where(c =>
            c.ShowInExpandedView &&
            c.DisplayPriority > 1 &&
            c.ColumnType != DataTableColumnType.Actions &&
            c.ColumnType != DataTableColumnType.Template).ToList();

        int row = 0;
        foreach (var column in expandableColumns)
        {
            var rowDef = new FrameworkElementFactory(typeof(RowDefinition));
            rowDef.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            gridFactory.AppendChild(rowDef);

            // Label (Header)
            var labelFactory = new FrameworkElementFactory(typeof(TextBlock));
            labelFactory.SetValue(TextBlock.TextProperty, $"{column.Header}:");
            labelFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            labelFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.TryFindResource("MaterialDesignBody"));
            labelFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 15, 5));
            labelFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            labelFactory.SetValue(Grid.RowProperty, row);
            labelFactory.SetValue(Grid.ColumnProperty, 0);
            gridFactory.AppendChild(labelFactory);

            // Value
            var valueFactory = CreateExpandedValueElement(column, row);
            gridFactory.AppendChild(valueFactory);

            row++;
        }

        factory.AppendChild(gridFactory);
        template.VisualTree = factory;
        MainDataGrid.RowDetailsTemplate = template;
    }

    /// <summary>
    /// Crea el elemento visual para el valor en la vista expandida
    /// </summary>
    private FrameworkElementFactory CreateExpandedValueElement(DataTableColumn column, int row)
    {
        FrameworkElementFactory valueFactory;

        switch (column.ColumnType)
        {
            case DataTableColumnType.Template:
                // Para templates, usar el mismo template
                if (!string.IsNullOrEmpty(column.TemplateKey))
                {
                    var contentControlFactory = new FrameworkElementFactory(typeof(ContentControl));
                    contentControlFactory.SetValue(ContentControl.ContentProperty, new Binding("Item"));

                    var template = TryFindResource(column.TemplateKey) as DataTemplate;
                    if (template != null)
                    {
                        contentControlFactory.SetValue(ContentControl.ContentTemplateProperty, template);
                    }

                    contentControlFactory.SetValue(Grid.RowProperty, row);
                    contentControlFactory.SetValue(Grid.ColumnProperty, 1);
                    return contentControlFactory;
                }
                goto default;

            default:
                // Para todos los demÃ¡s tipos, usar TextBlock
                valueFactory = new FrameworkElementFactory(typeof(TextBlock));

                var binding = new Binding($"Item.{column.PropertyName}");

                // Aplicar formato segÃºn el tipo
                if (!string.IsNullOrEmpty(column.StringFormat))
                {
                    binding.StringFormat = column.ColumnType == DataTableColumnType.Currency
                        ? $"{{0:{column.StringFormat}}}"
                        : column.StringFormat;
                }
                else if (column.ColumnType == DataTableColumnType.Currency)
                {
                    binding.StringFormat = "{0:C2}";
                }
                else if (column.ColumnType == DataTableColumnType.Number)
                {
                    binding.StringFormat = "{0:N2}";
                }

                valueFactory.SetBinding(TextBlock.TextProperty, binding);
                valueFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.TryFindResource("MaterialDesignBodyLight"));
                valueFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 0, 5));
                valueFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                valueFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                valueFactory.SetValue(Grid.RowProperty, row);
                valueFactory.SetValue(Grid.ColumnProperty, 1);

                break;
        }

        return valueFactory;
    }

    /// <summary>
    /// Genera las columnas del DataGrid basÃ¡ndose en la configuración
    /// </summary>
    private void GenerateColumns()
    {
        if (Columns == null || MainDataGrid == null)
            return;

        MainDataGrid.Columns.Clear();

        // Agregar columna de Ã­ndice automÃ¡tica al inicio
        var indexColumn = new System.Windows.Controls.DataGridTextColumn
        {
            Header = "N°",
            Width = new DataGridLength(60),
            IsReadOnly = true,
            Binding = new Binding("RowNumber")
        };

        var indexStyle = new Style(typeof(TextBlock));
        indexStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        indexStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        indexColumn.ElementStyle = indexStyle;

        MainDataGrid.Columns.Add(indexColumn);
        foreach (var column in Columns.Where(c => c.PropertyName != "IsExpanded"))
        {
            DataGridColumn gridColumn = column.ColumnType switch
            {
                DataTableColumnType.Text => CreateTextColumn(column),
                DataTableColumnType.Number => CreateNumberColumn(column),
                DataTableColumnType.Date => CreateDateColumn(column),
                DataTableColumnType.Currency => CreateCurrencyColumn(column),
                DataTableColumnType.Boolean => CreateBooleanColumn(column),
                DataTableColumnType.BooleanStatus => CreateBooleanStatusColumn(column),
                DataTableColumnType.Hyperlink => CreateHyperlinkColumn(column),
                DataTableColumnType.Actions => CreateActionsColumn(column),
                DataTableColumnType.Template => CreateTemplateColumn(column),
                DataTableColumnType.EditableText => CreateEditableTextColumn(column),
                DataTableColumnType.EditableNumber => CreateEditableNumberColumn(column),
                DataTableColumnType.ComboBox => CreateComboBoxColumn(column),
                _ => CreateTextColumn(column)
            };

            gridColumn.Header = column.Header;
            gridColumn.Width = ParseWidth(column.Width);
            gridColumn.CanUserSort = column.CanSort;

            MainDataGrid.Columns.Add(gridColumn);
        }

        // Aplicar visibilidad inmediatamente de forma sÃ­ncrona
        // Usar ActualWidth si estÃ¡ disponible, sino usar el ancho de la ventana padre
        double initialWidth = ActualWidth > 0 ? ActualWidth : 1920;
        if (initialWidth == 0)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                initialWidth = window.ActualWidth;
        }

        UpdateColumnVisibility(initialWidth);
    }

    /// <summary>
    /// Crea una columna de texto
    /// </summary>

    /// <summary>
    /// Crea una columna numerica
    /// </summary>
    private class DynamicColorConverter : IValueConverter
    {
        private readonly Func<object?, string?> _selector;
        private readonly Brush? _defaultBrush;

        public DynamicColorConverter(Func<object?, string?> selector, Brush? defaultBrush)
        {
            _selector = selector;
            _defaultBrush = defaultBrush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colorStr = _selector(value);
            if (!string.IsNullOrEmpty(colorStr))
            {
                try { return new System.Windows.Media.BrushConverter().ConvertFromString(colorStr); } catch { }
            }
            return _defaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    private class DynamicTooltipConverter : IValueConverter
    {
        private readonly Func<object?, string?> _selector;

        public DynamicTooltipConverter(Func<object?, string?> selector)
        {
            _selector = selector;
        }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _selector(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    private DataGridTemplateColumn CreateTemplateColumnWithVariant(DataTableColumn config)
    {
        var templateKey = config.Variant switch
        {
            CellDisplayVariant.Filled => "FilledStateTemplate",
            CellDisplayVariant.Outline => "OutlineStateTemplate",
            CellDisplayVariant.IconAndText => "IconStateTemplate",
            _ => null
        };
        var templateColumn = new DataGridTemplateColumn
        {
            IsReadOnly = config.IsReadOnly
        };

        if (string.IsNullOrEmpty(templateKey))
        {
            // Fallback o manejo para Default variant con ColorSelector?
            // Si es Default, podrÃ­amos querer simplemente un TextBlock con Foreground bindeado.
            // Vamos a crear un DataTemplate programÃ¡ticamente.

            var cellTemplate = new DataTemplate();
            var textBlockFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));

            // Binding del texto
            var textBinding = new Binding($"Item.{config.PropertyName}");
            if (!string.IsNullOrEmpty(config.StringFormat)) textBinding.StringFormat = config.StringFormat; // Aplicar formato aquÃ­
            textBlockFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, textBinding);

            // Binding del color (Foreground)
            if (config.ColorSelector != null)
            {
                var colorBinding = new Binding("Item");
                // Default color from config.Color if any
                Brush? defaultBrush = !string.IsNullOrEmpty(config.Color) ? (Brush)new System.Windows.Media.BrushConverter().ConvertFromString(config.Color) : null;
                colorBinding.Converter = new DynamicColorConverter(config.ColorSelector, defaultBrush);
                textBlockFactory.SetBinding(System.Windows.Controls.TextBlock.ForegroundProperty, colorBinding);
            }
            else if (!string.IsNullOrEmpty(config.Color))
            {
                textBlockFactory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Media.BrushConverter().ConvertFromString(config.Color));
            }

            // Alineación
            if (config.HorizontalAlignment != "Left")
            {
                textBlockFactory.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, ParseHorizontalAlignment(config.HorizontalAlignment));
            }

            textBlockFactory.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            textBlockFactory.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new System.Windows.Thickness(5, 0, 5, 0)); // Margen estÃ¡ndar

            // Binding del tooltip (si hay TooltipSelector)
            if (config.TooltipSelector != null)
            {
                var tooltipBinding = new Binding("Item");
                tooltipBinding.Converter = new DynamicTooltipConverter(config.TooltipSelector);
                textBlockFactory.SetBinding(System.Windows.Controls.TextBlock.ToolTipProperty, tooltipBinding);
            }

            cellTemplate.VisualTree = textBlockFactory;
            templateColumn.CellTemplate = cellTemplate;

            return templateColumn;
        }

        // Lógica existente para Templates con variantes (Filled, Outline, etc)
        if (!string.IsNullOrEmpty(templateKey))
        {
            var template = TryFindResource(templateKey) as DataTemplate;
            if (template != null)
            {
                // Determine default color brush
                Brush? defaultColorBrush = null;
                if (!string.IsNullOrEmpty(config.Color))
                {
                    ColumnMetadata.SetColor(templateColumn, config.Color);
                    defaultColorBrush = ColumnMetadata.GetColor(templateColumn);
                }

                var cellTemplate = new DataTemplate();

                // Crear un Border contenedor para aplicar el tooltip
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(0));

                // Binding del tooltip al Border (si hay TooltipSelector)
                if (config.TooltipSelector != null)
                {
                    var tooltipBinding = new Binding("Item");
                    tooltipBinding.Converter = new DynamicTooltipConverter(config.TooltipSelector);
                    borderFactory.SetBinding(Border.ToolTipProperty, tooltipBinding);
                }

                var contentPresenterFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
                contentPresenterFactory.SetValue(System.Windows.Controls.ContentPresenter.ContentTemplateProperty, template);

                var multiBinding = new MultiBinding { Mode = BindingMode.OneWay };

                // Binding 1: Value
                var valueBinding = new Binding($"Item.{config.PropertyName}");
                if (!string.IsNullOrEmpty(config.StringFormat)) valueBinding.StringFormat = config.StringFormat; // Aplicar formato
                multiBinding.Bindings.Add(valueBinding);

                // Binding 2: Icon
                multiBinding.Bindings.Add(new Binding { Source = config.Icon });

                // Binding 3: Color (Dynamic or Static)
                if (config.ColorSelector != null)
                {
                    var colorBinding = new Binding("Item");
                    colorBinding.Converter = new DynamicColorConverter(config.ColorSelector, defaultColorBrush);
                    multiBinding.Bindings.Add(colorBinding);
                }
                else
                {
                    multiBinding.Bindings.Add(new Binding { Source = defaultColorBrush });
                }

                multiBinding.Converter = new CellValueWithIconMultiConverter();
                contentPresenterFactory.SetBinding(System.Windows.Controls.ContentPresenter.ContentProperty, multiBinding);

                // Agregar el ContentPresenter al Border
                borderFactory.AppendChild(contentPresenterFactory);

                cellTemplate.VisualTree = borderFactory;
                templateColumn.CellTemplate = cellTemplate;
            }
        }
        return templateColumn;
    }

    private DataGridColumn CreateTextColumn(DataTableColumn config)
    {
        // Si no hay variante especial ni selector de color ni tooltip dinÃ¡mico, usar columna de texto normal
        if (config.Variant == CellDisplayVariant.Default && config.ColorSelector == null && string.IsNullOrEmpty(config.Color) && config.TooltipSelector == null)
        {
            var column = new System.Windows.Controls.DataGridTextColumn
            {
                Binding = new Binding($"Item.{config.PropertyName}")
                {
                    StringFormat = config.StringFormat
                },
                IsReadOnly = config.IsReadOnly
            };
            if (config.HorizontalAlignment != "Left")
            {
                column.ElementStyle = CreateTextBlockStyle(config.HorizontalAlignment);
            }
            return column;
        }

        return CreateTemplateColumnWithVariant(config);
    }

    private DataGridColumn CreateNumberColumn(DataTableColumn config)
    {
        // Si no hay variante especial ni selector de color ni tooltip dinÃ¡mico, usar columna de texto normal
        if (config.Variant == CellDisplayVariant.Default && config.ColorSelector == null && string.IsNullOrEmpty(config.Color) && config.TooltipSelector == null)
        {
            var format = config.StringFormat ?? "N2";
            var column = new System.Windows.Controls.DataGridTextColumn
            {
                Binding = new Binding($"Item.{config.PropertyName}")
                {
                    StringFormat = $"{{0:{format}}}"
                },
                IsReadOnly = config.IsReadOnly
            };

            column.ElementStyle = CreateTextBlockStyle(config.HorizontalAlignment == "Left" ? "Right" : config.HorizontalAlignment);

            return column;
        }

        // Ensure format is set if missing (CreateTemplateColumnWithVariant uses config.StringFormat)
        if (string.IsNullOrEmpty(config.StringFormat)) config.StringFormat = "{0:N2}"; // Default number format if using template
        else if (!config.StringFormat.Contains("{0:")) config.StringFormat = $"{{0:{config.StringFormat}}}"; // Wrap if not wrapped

        return CreateTemplateColumnWithVariant(config);
    }

    /// <summary>
    /// Crea una columna de fecha
    /// </summary>
    private System.Windows.Controls.DataGridTextColumn CreateDateColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "dd/MM/yyyy";
        return new System.Windows.Controls.DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = $"{{0:{format}}}"
            },
            IsReadOnly = config.IsReadOnly
        };
    }

    /// <summary>
    /// Crea una columna de moneda
    /// </summary>
    private System.Windows.Controls.DataGridTextColumn CreateCurrencyColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "C2";
        var column = new System.Windows.Controls.DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = $"{{0:{format}}}"
            },
            IsReadOnly = config.IsReadOnly
        };

        column.ElementStyle = CreateTextBlockStyle("Right");

        return column;
    }

    /// <summary>
    /// Crea una columna de checkbox
    /// </summary>
    private DataGridCheckBoxColumn CreateBooleanColumn(DataTableColumn config)
    {
        return new DataGridCheckBoxColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}"),
            IsReadOnly = config.IsReadOnly
        };
    }

    /// <summary>
    /// Crea una columna con hipervÃ­nculo clickeable
    /// </summary>
    private DataGridTemplateColumn CreateHyperlinkColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template para el hipervÃ­nculo
        var factory = new FrameworkElementFactory(typeof(TextBlock));

        // Crear el Hyperlink interno
        var hyperlinkFactory = new FrameworkElementFactory(typeof(System.Windows.Documents.Hyperlink));
        hyperlinkFactory.SetBinding(
            System.Windows.Documents.Hyperlink.CommandProperty,
            new Binding
            {
                Source = config.HyperlinkCommand
            });
        hyperlinkFactory.SetBinding(
            System.Windows.Documents.Hyperlink.CommandParameterProperty,
            new Binding("Item"));

        if (!string.IsNullOrEmpty(config.HyperlinkToolTip))
        {
            hyperlinkFactory.SetValue(
                System.Windows.Documents.Hyperlink.ToolTipProperty,
                config.HyperlinkToolTip);
        }

        // Crear el Run para el texto del hipervÃ­nculo
        var runFactory = new FrameworkElementFactory(typeof(System.Windows.Documents.Run));
        runFactory.SetBinding(
            System.Windows.Documents.Run.TextProperty,
            new Binding($"Item.{config.PropertyName}"));

        hyperlinkFactory.AppendChild(runFactory);
        factory.AppendChild(hyperlinkFactory);

        column.CellTemplate = new DataTemplate { VisualTree = factory };

        return column;
    }

    /// <summary>
    /// Crea una columna con botones de acción configurables
    /// </summary>
    private DataGridTemplateColumn CreateActionsColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template dinÃ¡micamente
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
        factory.SetValue(StackPanel.HorizontalAlignmentProperty, ParseHorizontalAlignment(config.HorizontalAlignment));

        // Agregar cada botón de acción
        foreach (var actionButton in config.ActionButtons)
        {
            var buttonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
            buttonFactory.SetValue(System.Windows.Controls.Button.StyleProperty,
                Application.Current.TryFindResource("MaterialDesignIconButton"));
            buttonFactory.SetValue(System.Windows.Controls.Button.WidthProperty, actionButton.Width);
            buttonFactory.SetValue(System.Windows.Controls.Button.HeightProperty, actionButton.Height);
            buttonFactory.SetValue(System.Windows.Controls.Button.ToolTipProperty, actionButton.Tooltip);
            buttonFactory.SetValue(System.Windows.Controls.Button.MarginProperty,
                ParseThickness(actionButton.Margin));

            // Establecer el comando
            if (actionButton.Command != null)
            {
                buttonFactory.SetValue(System.Windows.Controls.Button.CommandProperty, actionButton.Command);
                buttonFactory.SetBinding(System.Windows.Controls.Button.CommandParameterProperty,
                    new Binding("Item"));
            }
            // Binding para IsEnabled basado en Disabled
            if (actionButton.Disabled != null)
            {
                var isEnabledBinding = new Binding("Item")
                {
                    Converter = new InvertBooleanFunctionConverter(actionButton.Disabled)
                };
                buttonFactory.SetBinding(System.Windows.Controls.Button.IsEnabledProperty, isEnabledBinding);
            }
            // Establecer el color si estÃ¡ especificado
            if (actionButton.Foreground != null)
            {
                buttonFactory.SetValue(System.Windows.Controls.Button.ForegroundProperty, actionButton.Foreground);
            }

            // Crear el icono
            var iconFactory = new FrameworkElementFactory(typeof(MaterialDesignThemes.Wpf.PackIcon));
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.KindProperty, actionButton.Icon);
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.WidthProperty, actionButton.IconSize);
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.HeightProperty, actionButton.IconSize);

            buttonFactory.AppendChild(iconFactory);
            factory.AppendChild(buttonFactory);
        }

        column.CellTemplate = new DataTemplate { VisualTree = factory };

        return column;
    }

    /// <summary>
    /// Crea una columna con template personalizado
    /// </summary>
    private DataGridTemplateColumn CreateTemplateColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = config.IsReadOnly
        };

        if (!string.IsNullOrEmpty(config.TemplateKey))
        {
            // Intentar buscar en los recursos de este control
            DataTemplate? template = TryFindResource(config.TemplateKey) as DataTemplate;

            // Si no se encuentra, buscar en el Application.Current.Resources
            if (template == null && Application.Current != null)
            {
                template = Application.Current.TryFindResource(config.TemplateKey) as DataTemplate;
            }

            // Si aÃºn no se encuentra, buscar en el Ã¡rbol visual hacia arriba
            if (template == null)
            {
                DependencyObject? parent = this;
                while (parent != null && template == null)
                {
                    parent = LogicalTreeHelper.GetParent(parent);
                    if (parent is FrameworkElement fe)
                    {
                        template = fe.TryFindResource(config.TemplateKey) as DataTemplate;
                    }
                }
            }

            if (template != null)
            {
                column.CellTemplate = template;
            }
        }

        return column;
    }

    /// <summary>
    /// Crea una columna con Ã­cono de estado (check verde para true, X roja para false)
    /// Soporta expresiones complejas con acceso a propiedades del Item
    /// </summary>
    private DataGridTemplateColumn CreateBooleanStatusColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template para el estado
        var factory = new FrameworkElementFactory(typeof(PackIcon));

        var status = config.Status ?? new StatusIndicator();

        // Binding al objeto completo Item para poder acceder a todas sus propiedades
        var kindParam = $"{status.BooleanTrueIcon}|{status.BooleanFalseIcon}";
        var kindBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = kindParam,
                ReturnType = ExpressionReturnType.Icon
            }
        };
        factory.SetBinding(PackIcon.KindProperty, kindBinding);

        // Binding para el color del Ã­cono
        var colorParam = $"{status.BooleanTrueColor ?? "#4CAF50"}|{status.BooleanFalseColor ?? "#F44336"}";
        var colorBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = colorParam,
                ReturnType = ExpressionReturnType.Color
            }
        };
        factory.SetBinding(PackIcon.ForegroundProperty, colorBinding);

        // Propiedades del Ã­cono
        factory.SetValue(PackIcon.WidthProperty, 24.0);
        factory.SetValue(PackIcon.HeightProperty, 24.0);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Binding para el ToolTip
        var tooltipParam = $"{status.BooleanTrueText ?? "Completado"}|{status.BooleanFalseText ?? "Pendiente"}";
        var tooltipBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = tooltipParam,
                ReturnType = ExpressionReturnType.Text
            }
        };
        factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

        var dataTemplate = new DataTemplate
        {
            VisualTree = factory
        };

        column.CellTemplate = dataTemplate;
        return column;
    }

    /// <summary>
    /// Crea una columna de texto editable con modo de edición inline
    /// </summary>
    private DataGridTemplateColumn CreateEditableTextColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();

        // Template Ãºnico que muestra TextBlock o TextBox segÃºn IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(5, 0, 5, 0));

        // TextBlock (modo lectura)
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{config.PropertyName}"));
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Trigger para ocultar cuando estÃ¡ editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);

        // TextBox con estilo simplificado (modo edición)
        var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));

        // âœ¨ OPTIMIZACIÃ“N: Usar estilo mÃ¡s ligero y UpdateSourceTrigger.LostFocus
        // Esto reduce drÃ¡sticamente las notificaciones de cambio durante la edición
        textBoxFactory.SetBinding(TextBox.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus // Solo actualiza al salir del campo
        });
        textBoxFactory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBoxFactory.SetValue(TextBox.FontSizeProperty, 13.0);
        textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(8, 4, 8, 4));
        textBoxFactory.SetValue(TextBox.MarginProperty, new Thickness(0, 2, 0, 2));
        textBoxFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        textBoxFactory.SetValue(TextBox.BackgroundProperty, System.Windows.Media.Brushes.Transparent);

        // Trigger para mostrar solo cuando estÃ¡ editando
        var textBoxTrigger = new DataTrigger();
        textBoxTrigger.Binding = new Binding("Item.IsEditing");
        textBoxTrigger.Value = false;
        textBoxTrigger.Setters.Add(new Setter(TextBox.VisibilityProperty, Visibility.Collapsed));

        var textBoxStyleWithTrigger = new Style(typeof(TextBox));
        textBoxStyleWithTrigger.Triggers.Add(textBoxTrigger);
        textBoxFactory.SetValue(TextBox.StyleProperty, textBoxStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(textBoxFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Crea una columna numÃ©rica editable con modo de edición inline
    /// </summary>
    private DataGridTemplateColumn CreateEditableNumberColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();
        var format = config.StringFormat ?? "N2";

        // Template Ãºnico que muestra TextBlock o TextBox segÃºn IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(5, 0, 5, 0));

        // TextBlock (modo lectura)
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            StringFormat = $"{{0:{format}}}"
        });
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);

        // Trigger para ocultar cuando estÃ¡ editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);


        // TextBox con estilo simplificado (modo edición)
        var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));

        // âœ¨ OPTIMIZACIÃ“N: Usar estilo mÃ¡s ligero y UpdateSourceTrigger.LostFocus
        textBoxFactory.SetBinding(TextBox.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus // Solo actualiza al salir del campo
        });
        textBoxFactory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBoxFactory.SetValue(TextBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Right);
        textBoxFactory.SetValue(TextBox.FontSizeProperty, 13.0);
        textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(8, 4, 8, 4));
        textBoxFactory.SetValue(TextBox.MarginProperty, new Thickness(0, 2, 0, 2));
        textBoxFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        textBoxFactory.SetValue(TextBox.BackgroundProperty, System.Windows.Media.Brushes.Transparent);

        // Binding de IsReadOnly para campos como Peso Bruto que pueden bloquearse
        if (config.PropertyName.Contains("Peso") || config.PropertyName.Contains("pb"))
        {
            textBoxFactory.SetBinding(TextBox.IsReadOnlyProperty, new Binding("Item.IsPesoBrutoReadOnly"));
        }

        // Trigger para mostrar solo cuando estÃ¡ editando
        var textBoxTrigger = new DataTrigger();
        textBoxTrigger.Binding = new Binding("Item.IsEditing");
        textBoxTrigger.Value = false;
        textBoxTrigger.Setters.Add(new Setter(TextBox.VisibilityProperty, Visibility.Collapsed));

        var textBoxStyleWithTrigger = new Style(typeof(TextBox));
        textBoxStyleWithTrigger.Triggers.Add(textBoxTrigger);
        textBoxFactory.SetValue(TextBox.StyleProperty, textBoxStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(textBoxFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Crea una columna con ComboBox para selección de opciones con filtrado en tiempo real
    /// </summary>
    private DataGridTemplateColumn CreateComboBoxColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();

        // Template Ãºnico que muestra TextBlock o ComboBox segÃºn IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(0));

        // TextBlock (modo lectura) - muestra el texto descriptivo
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        var displayPropertyName = config.PropertyName.EndsWith("_id")
            ? config.PropertyName.Replace("_id", "_des")
            : config.PropertyName;

        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{displayPropertyName}"));
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Trigger para ocultar cuando estÃ¡ editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);

        // ComboBox con estilo Material Design (modo edición) + FILTRADO EN TIEMPO REAL
        var comboFactory = new FrameworkElementFactory(typeof(ComboBox));

        // Aplicar estilo Material Design estÃ¡ndar
        var mdStyle = Application.Current.TryFindResource("MaterialDesignFilledComboBox") as Style;
        if (mdStyle != null)
        {
            comboFactory.SetValue(ComboBox.StyleProperty, mdStyle);
        }

        // âœ¨ HABILITAR FILTRADO EN TIEMPO REAL âœ¨
        comboFactory.SetValue(ComboBox.IsEditableProperty, true); // Permite escribir texto
        comboFactory.SetValue(ComboBox.IsTextSearchEnabledProperty, false); // Desactivar bÃºsqueda nativa (usaremos filtrado custom)

        // ItemsSource desde la colección proporcionada - USAR BINDING PARA QUE SEA DINÃMICO
        if (config.ComboBoxItemsSource != null)
        {
            // Crear un binding a la fuente de datos para que se actualice dinÃ¡micamente
            var itemsSourceBinding = new Binding()
            {
                Source = config.ComboBoxItemsSource,
                Mode = BindingMode.OneWay
            };
            comboFactory.SetBinding(ComboBox.ItemsSourceProperty, itemsSourceBinding);
        }

        // DisplayMemberPath y SelectedValuePath para objetos complejos
        if (!string.IsNullOrEmpty(config.ComboBoxDisplayMemberPath))
        {
            comboFactory.SetValue(ComboBox.DisplayMemberPathProperty, config.ComboBoxDisplayMemberPath);
        }

        if (!string.IsNullOrEmpty(config.ComboBoxSelectedValuePath))
        {
            comboFactory.SetValue(ComboBox.SelectedValuePathProperty, config.ComboBoxSelectedValuePath);

            // Usar SelectedValue cuando hay SelectedValuePath
            comboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding($"Item.{config.PropertyName}")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus // Optimización: actualiza al cerrar dropdown
            });
        }
        else
        {
            // Usar SelectedItem para colecciones simples (strings)
            comboFactory.SetBinding(ComboBox.SelectedItemProperty, new Binding($"Item.{config.PropertyName}")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus // Optimización: actualiza al cerrar dropdown
            });
        }

        comboFactory.SetValue(ComboBox.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        comboFactory.SetValue(ComboBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        comboFactory.SetValue(ComboBox.FontSizeProperty, 13.0);
        comboFactory.SetValue(ComboBox.PaddingProperty, new Thickness(8, 4, 8, 4));
        comboFactory.SetValue(ComboBox.MarginProperty, new Thickness(0));

        // âœ¨ OPTIMIZACIÃ“N: Configurar filtrado solo cuando el ComboBox obtiene el foco (modo edición) âœ¨
        // Esto evita configurar el filtrado para todos los ComboBox al cargar, mejorando el rendimiento
        comboFactory.AddHandler(ComboBox.GotFocusEvent, new RoutedEventHandler((sender, e) =>
        {
            if (sender is ComboBox combo && config.ComboBoxItemsSource is System.Collections.IEnumerable itemsSource)
            {
                // Verificar si ya fue configurado para evitar configuraciones duplicadas
                if (combo.Tag == null || combo.Tag.ToString() != "FilterConfigured")
                {
                    SetupComboBoxFiltering(combo, itemsSource, config.ComboBoxDisplayMemberPath);
                    combo.Tag = "FilterConfigured"; // Marcar como configurado
                }
            }
        }));

        // Trigger para mostrar solo cuando estÃ¡ editando
        var comboTrigger = new DataTrigger();
        comboTrigger.Binding = new Binding("Item.IsEditing");
        comboTrigger.Value = false;
        comboTrigger.Setters.Add(new Setter(ComboBox.VisibilityProperty, Visibility.Collapsed));

        var comboStyleWithTrigger = new Style(typeof(ComboBox), mdStyle);
        comboStyleWithTrigger.Triggers.Add(comboTrigger);
        comboFactory.SetValue(ComboBox.StyleProperty, comboStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(comboFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Configura el filtrado en tiempo real para un ComboBox
    /// </summary>
    private void SetupComboBoxFiltering(ComboBox combo, System.Collections.IEnumerable originalSource, string? displayMemberPath)
    {
        // Guardar la fuente original
        var originalList = originalSource.Cast<object>().ToList();

        // Crear CollectionViewSource para filtrado
        var viewSource = new CollectionViewSource { Source = originalList };
        combo.ItemsSource = viewSource.View;

        // Variable para rastrear si estamos seleccionando programÃ¡ticamente
        bool isSelectingItem = false;
        bool isUpdatingText = false;

        // âœ¨ DEBOUNCE TIMER para evitar filtrado excesivo al escribir rÃ¡pido âœ¨
        System.Windows.Threading.DispatcherTimer? debounceTimer = null;

        // Evento cuando se escribe en el ComboBox
        combo.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((sender, e) =>
        {
            if (isSelectingItem || isUpdatingText) return; // Evitar filtrado durante selección

            var textBox = e.OriginalSource as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            var searchText = textBox.Text?.ToLower() ?? "";

            // Detener el timer anterior si existe
            debounceTimer?.Stop();

            // Si el texto estÃ¡ vacÃ­o, limpiar filtro y selección inmediatamente
            if (string.IsNullOrEmpty(searchText))
            {
                isUpdatingText = true;

                try
                {
                    // Limpiar filtro para mostrar todos los items
                    viewSource.View.Filter = null;
                    viewSource.View.Refresh();

                    // Limpiar la selección de forma segura
                    combo.SelectedItem = null;
                    combo.SelectedValue = null;
                    combo.SelectedIndex = -1;
                }
                catch
                {
                    // Ignorar errores de conversión al limpiar
                }
                finally
                {
                    isUpdatingText = false;
                }

                return; // Salir temprano
            }

            // Crear o reutilizar el timer de debounce
            if (debounceTimer == null)
            {
                debounceTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300) // Esperar 300ms despuÃ©s del Ãºltimo keystroke
                };
                debounceTimer.Tick += (s, args) =>
                {
                    debounceTimer.Stop();

                    // Obtener el texto actual del ComboBox en el momento de ejecutar el filtro
                    var currentSearchText = combo.Text?.ToLower() ?? "";

                    // Aplicar filtro solo si hay texto
                    viewSource.View.Filter = item =>
                    {
                        // Obtener el texto a comparar
                        string itemText = "";
                        if (!string.IsNullOrEmpty(displayMemberPath))
                        {
                            var prop = item.GetType().GetProperty(displayMemberPath);
                            itemText = prop?.GetValue(item)?.ToString()?.ToLower() ?? "";
                        }
                        else
                        {
                            itemText = item?.ToString()?.ToLower() ?? "";
                        }

                        return itemText.Contains(currentSearchText);
                    };

                    // Refrescar la vista
                    viewSource.View.Refresh();

                    // Abrir el dropdown si hay resultados
                    if (viewSource.View.Cast<object>().Any())
                    {
                        combo.IsDropDownOpen = true;
                    }
                };
            }

            // Iniciar el timer (se reinicia en cada keystroke)
            debounceTimer.Start();
        }));

        // âœ¨ NUEVO: Manejar la tecla Tab para auto-seleccionar el primer item filtrado âœ¨
        combo.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Tab && !string.IsNullOrEmpty(combo.Text))
            {
                var filteredItems = viewSource.View.Cast<object>().ToList();

                if (filteredItems.Any())
                {
                    // Seleccionar el primer item filtrado
                    isSelectingItem = true;
                    combo.SelectedItem = filteredItems.First();
                    combo.IsDropDownOpen = false;
                    isSelectingItem = false;

                    // Marcar el evento como manejado para que no se mueva al siguiente control aÃºn
                    // (permitir que WPF procese la selección primero)
                }
            }
            else if (e.Key == Key.Enter && !string.IsNullOrEmpty(combo.Text))
            {
                // Enter tambiÃ©n selecciona el primer item si hay filtro activo
                var filteredItems = viewSource.View.Cast<object>().ToList();

                if (filteredItems.Any() && combo.SelectedItem == null)
                {
                    isSelectingItem = true;
                    combo.SelectedItem = filteredItems.First();
                    combo.IsDropDownOpen = false;
                    isSelectingItem = false;
                    e.Handled = true; // Prevenir que Enter haga otra acción
                }
            }
        };

        // Cuando se selecciona un item, actualizar el texto
        combo.SelectionChanged += (s, e) =>
        {
            if (isUpdatingText) return; // Evitar bucle infinito

            if (combo.SelectedItem != null && !isSelectingItem)
            {
                isSelectingItem = true;

                // Limpiar filtro para mostrar todos los items
                viewSource.View.Filter = null;
                viewSource.View.Refresh();

                isSelectingItem = false;
            }
            else if (combo.SelectedItem == null && !isSelectingItem)
            {
                // Si se limpia la selección, mostrar todos los items
                viewSource.View.Filter = null;
                viewSource.View.Refresh();
            }
        };

        // Al abrir el dropdown, mostrar todos los items si no hay texto
        combo.DropDownOpened += (s, e) =>
        {
            if (string.IsNullOrEmpty(combo.Text))
            {
                viewSource.View.Filter = null;
                viewSource.View.Refresh();
            }
        };

        // Al cargar, asegurar que no haya selección inicial
        combo.Loaded += (s, e) =>
        {
            if (combo.SelectedItem == null && combo.SelectedIndex == -1)
            {
                // Forzar que no haya selección inicial
                isUpdatingText = true;
                try
                {
                    combo.SelectedIndex = -1;
                    combo.SelectedValue = null;
                    combo.Text = string.Empty;
                }
                catch
                {
                    // Ignorar errores de conversión
                }
                finally
                {
                    isUpdatingText = false;
                }
            }
        };
    }

    /// <summary>
    /// Crea un estilo para TextBlock con alineación especÃ­fica
    /// </summary>
    private Style CreateTextBlockStyle(string alignment)
    {
        var style = new Style(typeof(TextBlock));

        var horizontalAlignment = alignment switch
        {
            "Center" => HorizontalAlignment.Center,
            "Right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, horizontalAlignment));
        style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 0, 5, 0)));

        return style;
    }

    /// <summary>
    /// Parsea el ancho de columna
    /// </summary>
    private DataGridLength ParseWidth(string width)
    {
        if (width == "*")
            return new DataGridLength(1, DataGridLengthUnitType.Star);

        if (width.EndsWith("*"))
        {
            var value = width.TrimEnd('*');
            if (double.TryParse(value, out var starValue))
                return new DataGridLength(starValue, DataGridLengthUnitType.Star);
        }

        if (width.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return DataGridLength.Auto;

        if (double.TryParse(width, out var pixels))
            return new DataGridLength(pixels);

        return new DataGridLength(1, DataGridLengthUnitType.Star);
    }

    /// <summary>
    /// Parsea un string de margen en Thickness
    /// </summary>
    private Thickness ParseThickness(string margin)
    {
        var parts = margin.Split(',');
        if (parts.Length == 1 && double.TryParse(parts[0], out var uniform))
            return new Thickness(uniform);
        if (parts.Length == 2 && double.TryParse(parts[0], out var horizontal) && double.TryParse(parts[1], out var vertical))
            return new Thickness(horizontal, vertical, horizontal, vertical);
        if (parts.Length == 4 &&
            double.TryParse(parts[0], out var left) &&
            double.TryParse(parts[1], out var top) &&
            double.TryParse(parts[2], out var right) &&
            double.TryParse(parts[3], out var bottom))
            return new Thickness(left, top, right, bottom);

        return new Thickness(0);
    }

    /// <summary>
    /// Parsea un string de alineación horizontal
    /// </summary>
    private static HorizontalAlignment ParseHorizontalAlignment(string alignment)
    {
        return alignment switch
        {
            "Center" => HorizontalAlignment.Center,
            "Right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }

    /// <summary>
    /// Parsea un string de alineación de texto
    /// </summary>
    private static TextAlignment ParseTextAlignment(string alignment)
    {
        return alignment switch
        {
            "Center" => TextAlignment.Center,
            "Right" => TextAlignment.Right,
            "Justify" => TextAlignment.Justify,
            _ => TextAlignment.Left
        };
    }

    /// <summary>
    /// Genera la fila de totales dinÃ¡micamente
    /// </summary>
    private void GenerateTotalsRow()
    {
        if (Columns == null)
            return;

        // Buscar el Grid de totales
        var totalsGrid = this.FindName("TotalsGrid") as Grid;
        if (totalsGrid == null)
            return;

        totalsGrid.Children.Clear();
        totalsGrid.ColumnDefinitions.Clear();

        // Agregar columna para el Ã­ndice (NÂ°)
        totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

        var indexLabel = new TextBlock
        {
            Text = "TOTALES",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12, 0, 12, 0),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(33, 33, 33))
        };
        Grid.SetColumn(indexLabel, 0);
        totalsGrid.Children.Add(indexLabel);

        int columnIndex = 1;

        // Verificar si hay columnas ACTUALMENTE ocultas (verificar en el DataGrid)
        bool hasHiddenColumnsNow = HasExpanderColumn();
        if (hasHiddenColumnsNow)
        {
            // Agregar columna vacÃ­a para el expander
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            columnIndex++;
        }

        // Agregar columnas configuradas (excluyendo IsExpanded manual si existe)
        foreach (var column in Columns.Where(c => c.PropertyName != "IsExpanded"))
        {
            var colWidth = ParseWidth(column.Width);
            // Convertir DataGridLength a GridLength
            GridLength gridLength;
            if (colWidth.UnitType == DataGridLengthUnitType.Star)
                gridLength = new GridLength(colWidth.Value, GridUnitType.Star);
            else if (colWidth.IsAuto)
                gridLength = GridLength.Auto;
            else
                gridLength = new GridLength(colWidth.Value);

            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = gridLength });

            if (column.ShowTotal)
            {
                var totalBlock = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(12, 0, 12, 0),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(33, 33, 33)),
                    // Agregar Tag para identificar la columna asociada
                    Tag = column.PropertyName
                };

                // Binding al total de la columna usando indexador de diccionario
                var binding = new Binding($"ColumnTotals[{column.PropertyName}]")
                {
                    FallbackValue = "0"
                };

                if (column.ColumnType == DataTableColumnType.Currency)
                {
                    binding.StringFormat = column.StringFormat ?? "C2";
                }
                else if (column.ColumnType == DataTableColumnType.Number)
                {
                    binding.StringFormat = column.StringFormat ?? "N2";
                }

                if (column.HorizontalAlignment == "Right")
                    totalBlock.HorizontalAlignment = HorizontalAlignment.Right;
                else if (column.HorizontalAlignment == "Center")
                    totalBlock.HorizontalAlignment = HorizontalAlignment.Center;

                totalBlock.SetBinding(TextBlock.TextProperty, binding);

                Grid.SetColumn(totalBlock, columnIndex);
                totalsGrid.Children.Add(totalBlock);
            }

            columnIndex++;
        }
    }

    /// <summary>
    /// Genera los botones de acción del header dinÃ¡micamente
    /// Si no se proporcionan HeaderActions, genera automÃ¡ticamente un botón de reload
    /// </summary>
    private void GenerateHeaderActions()
    {
        var headerActionsContainer = this.FindName("HeaderActionsContainer") as StackPanel;
        if (headerActionsContainer == null)
            return;

        headerActionsContainer.Children.Clear();

        // Si no hay HeaderActions personalizadas, crear botón de reload automÃ¡tico
        if (HeaderActions == null || HeaderActions.Count == 0)
        {
            // Intentar obtener el comando de reload
            ICommand? reloadCmd = ReloadCommand;

            // Si no hay ReloadCommand, intentar usar RefreshCommand del ViewModel
            // Primero buscar en ParentDataContext (ViewModel padre), luego en DataContext
            if (reloadCmd == null)
            {
                var contextToSearch = ParentDataContext ?? DataContext;

                if (contextToSearch != null)
                {
                    var refreshProp = contextToSearch.GetType().GetProperty("RefreshCommand");
                    if (refreshProp == null)
                    {
                        refreshProp = contextToSearch.GetType().GetProperty("BuscarCommand");
                    }
                    if (refreshProp == null)
                    {
                        refreshProp = contextToSearch.GetType().GetProperty("CargarCommand");
                    }
                    reloadCmd = refreshProp?.GetValue(contextToSearch) as ICommand;
                }
            }

            // Solo crear el botón si hay un comando disponible
            if (reloadCmd != null)
            {
                var autoReloadButton = new System.Windows.Controls.Button
                {
                    Command = reloadCmd,
                    ToolTip = "Recargar datos",
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 0, 8, 0),
                    Style = (Style)FindResource("MaterialDesignIconButton"),
                    Content = new PackIcon
                    {
                        Kind = PackIconKind.Refresh,
                        Width = 20,
                        Height = 20
                    }
                };

                headerActionsContainer.Children.Add(autoReloadButton);
            }

            return; // No procesar mÃ¡s si usamos el botón automÃ¡tico
        }

        // Renderizar HeaderActions personalizadas
        foreach (var action in HeaderActions)
        {
            if (action.IsIconButton)
            {
                // Crear botón estilo icono (sin fondo, solo icono)
                var iconButton = new System.Windows.Controls.Button
                {
                    Command = action.Command,
                    ToolTip = action.Tooltip,
                    Width = 32,
                    Height = 32,
                    Margin = ParseMargin(action.Margin),
                    Style = (Style)FindResource("MaterialDesignIconButton"),
                    Content = new PackIcon
                    {
                        Kind = action.Icon,
                        Width = 20,
                        Height = 20
                    }
                };

                // Aplicar función de deshabilitado si existe
                if (action.IsDisabled != null)
                {
                    iconButton.IsEnabled = !action.IsDisabled();
                }

                headerActionsContainer.Children.Add(iconButton);
            }
            else
            {
                // Crear botón normal con CustomButton
                var button = new CustomButton
                {
                    Text = action.Text,
                    IconKind = action.Icon,
                    Command = action.Command,
                    ToolTip = action.Tooltip,
                    Variant = action.Variant,
                    IsOutlined = action.IsOutlined,
                    Height = action.Height,
                    Margin = ParseMargin(action.Margin)
                };

                // Aplicar color personalizado solo si es Custom
                if (action.Variant == ButtonVariant.Custom && !string.IsNullOrEmpty(action.BackgroundColor))
                {
                    button.BackgroundColor = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(action.BackgroundColor));
                }

                // Aplicar función de deshabilitado si existe
                if (action.IsDisabled != null)
                {
                    button.IsEnabled = !action.IsDisabled();
                }

                headerActionsContainer.Children.Add(button);
            }
        }
    }

    /// <summary>
    /// Parsea un string de margen en formato "left,top,right,bottom"
    /// </summary>
    private Thickness ParseMargin(string margin)
    {
        var parts = margin.Split(',');
        if (parts.Length == 4)
            return new Thickness(
                double.Parse(parts[0]),
                double.Parse(parts[1]),
                double.Parse(parts[2]),
                double.Parse(parts[3]));
        return new Thickness(0);
    }

    /// <summary>
    /// Actualiza la visibilidad de las celdas de totales segÃºn las columnas visibles
    /// </summary>
    private void UpdateTotalsRowVisibility()
    {
        var totalsGrid = this.FindName("TotalsGrid") as Grid;
        if (totalsGrid == null || Columns == null)
            return;

        // Iterar sobre las columnas del DataGrid y sincronizar con los totales
        for (int i = 0; i < Columns.Count && i + 1 < MainDataGrid.Columns.Count; i++)
        {
            var config = Columns[i];
            var dataGridColumn = MainDataGrid.Columns[i + 1]; // +1 por columna de Ã­ndice

            // Encontrar el TextBlock correspondiente en totalsGrid por Tag
            foreach (var child in totalsGrid.Children)
            {
                if (child is TextBlock textBlock && textBlock.Tag?.ToString() == config.PropertyName)
                {
                    // Sincronizar visibilidad con la columna del DataGrid
                    textBlock.Visibility = dataGridColumn.Visibility;
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Convertidor para mostrar el Ã­ndice de fila (base 1) con offset de pÃ¡gina
/// </summary>
public class IndexConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Verificar que los valores no sean nulos ni UnsetValue
        if (values.Length >= 2 &&
            values[0] != DependencyProperty.UnsetValue &&
            values[1] != DependencyProperty.UnsetValue)
        {
            int rowIndex = 0;
            int pageStartIndex = 0;

            if (values[0] is int idx)
                rowIndex = idx;

            if (values[1] is int offset)
                pageStartIndex = offset;

            // rowIndex es el AlternationIndex (0-based dentro de la pÃ¡gina)
            // pageStartIndex es el offset de registros de pÃ¡ginas anteriores
            return (pageStartIndex + rowIndex + 1).ToString();
        }
        return "0";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Tipo de retorno para el ExpressionConverter
/// </summary>
public enum ExpressionReturnType
{
    Icon,
    Color,
    Text
}

/// <summary>
/// ParÃ¡metros para el ExpressionConverter
/// </summary>
public class ExpressionParameter
{
    public string PropertyName { get; set; } = "";
    public string Expression { get; set; } = "";
    public ExpressionReturnType ReturnType { get; set; }
}

/// <summary>
/// Convertidor potente que evalÃºa expresiones con acceso completo al objeto Item
/// Soporta:
/// - Formato simple: "Check|Close" (para booleanos)
/// - Expresiones condicionales: "Item.Estado == 1 ? Check : Item.Estado == 2 ? Alert : Close"
/// - Acceso a propiedades: "Item.PesoNeto", "Item.Cliente"
/// - Operaciones matemÃ¡ticas: "Item.Bruto - Item.Tara"
/// - Comparaciones: ==, !=, >, <, >=, <=
/// </summary>
public class ExpressionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not ExpressionParameter expParam || value == null)
            return GetDefaultValue(targetType);

        try
        {
            var expression = expParam.Expression;
            if (string.IsNullOrWhiteSpace(expression))
                return GetDefaultValue(targetType);

            string result;

            // Formato simple "valor1|valor2" (para booleanos o valores convertibles)
            if (!expression.Contains('?') && expression.Contains('|'))
            {
                var propertyValue = GetPropertyValue(value, expParam.PropertyName);

                // Convertir el valor a booleano
                bool boolValue;
                if (propertyValue is bool b)
                {
                    boolValue = b;
                }
                else if (propertyValue is int intVal)
                {
                    boolValue = intVal == 1;
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(int?))
                {
                    var nullableInt = (int?)propertyValue;
                    boolValue = nullableInt == 1;
                }
                else
                {
                    return GetDefaultValue(targetType);
                }

                var parts = expression.Split('|');
                result = boolValue ? parts[0].Trim() : parts[1].Trim();
            }
            // Expresiones con operadores ternarios y acceso a propiedades
            else if (expression.Contains('?'))
            {
                result = EvaluateTernaryExpression(value, expression);
            }
            else
            {
                // Expresión simple o acceso a propiedad
                result = EvaluateExpression(value, expression);
            }

            // Convertir al tipo de destino segÃºn ReturnType
            return expParam.ReturnType switch
            {
                ExpressionReturnType.Icon => ConvertToIcon(result),
                ExpressionReturnType.Color => ConvertToColor(result),
                ExpressionReturnType.Text => result,
                _ => result
            };
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Obtiene el valor de una propiedad del objeto usando reflexión
    /// Soporta: "PropertyName", "Item.PropertyName", propiedades anidadas
    /// </summary>
    private object? GetPropertyValue(object obj, string propertyPath)
    {
        try
        {
            // Remover "Item." si existe
            propertyPath = propertyPath.Replace("Item.", "").Trim();

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
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// EvalÃºa expresiones simples o acceso a propiedades
    /// Ejemplos: "Item.Estado", "Item.Bruto - Item.Tara", "100"
    /// </summary>
    private string EvaluateExpression(object item, string expression)
    {
        expression = expression.Trim();

        // Si contiene "Item.", es acceso a propiedad
        if (expression.Contains("Item."))
        {
            // Operaciones matemÃ¡ticas simples
            if (expression.Contains('+') || expression.Contains('-') ||
                expression.Contains('*') || expression.Contains('/'))
            {
                return EvaluateMathExpression(item, expression).ToString();
            }

            // Acceso simple a propiedad
            var value = GetPropertyValue(item, expression);
            return value?.ToString() ?? "";
        }

        return expression;
    }

    /// <summary>
    /// EvalÃºa expresiones matemÃ¡ticas simples
    /// Ejemplo: "Item.Bruto - Item.Tara"
    /// </summary>
    private double EvaluateMathExpression(object item, string expression)
    {
        try
        {
            // Reemplazar referencias a propiedades con sus valores
            var tokens = expression.Split(new[] { '+', '-', '*', '/', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries);

            var evalExpression = expression;
            foreach (var token in tokens)
            {
                var trimmedToken = token.Trim();
                if (trimmedToken.StartsWith("Item."))
                {
                    var value = GetPropertyValue(item, trimmedToken);
                    if (value != null)
                    {
                        evalExpression = evalExpression.Replace(trimmedToken, value.ToString());
                    }
                }
            }

            // Evaluar la expresión matemÃ¡tica (solo operaciones bÃ¡sicas)
            return EvaluateSimpleMath(evalExpression);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// EvalÃºa operaciones matemÃ¡ticas bÃ¡sicas sin usar eval dinÃ¡mico
    /// </summary>
    private double EvaluateSimpleMath(string expression)
    {
        try
        {
            // Remover espacios
            expression = expression.Replace(" ", "");

            // Orden de operaciones: *, / primero, luego +, -
            // Esta es una implementación simple, para casos complejos usar NCalc o similar

            // Por ahora, solo suma/resta simple
            if (expression.Contains('+'))
            {
                var parts = expression.Split('+');
                return parts.Sum(p => double.Parse(p.Trim()));
            }
            if (expression.Contains('-'))
            {
                var parts = expression.Split('-');
                var result = double.Parse(parts[0].Trim());
                for (int i = 1; i < parts.Length; i++)
                    result -= double.Parse(parts[i].Trim());
                return result;
            }

            return double.Parse(expression);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// EvalÃºa expresiones con operadores ternarios anidados
    /// Ejemplo: "Item.Estado == 1 ? Check : Item.Estado == 2 ? Alert : Close"
    /// </summary>
    private string EvaluateTernaryExpression(object item, string expression)
    {
        try
        {
            var questionIndex = expression.IndexOf('?');
            if (questionIndex == -1)
                return expression.Trim();

            var condition = expression.Substring(0, questionIndex).Trim();
            var colonIndex = FindMatchingColon(expression, questionIndex);

            if (colonIndex == -1)
                return expression.Trim();

            var trueValue = expression.Substring(questionIndex + 1, colonIndex - questionIndex - 1).Trim();
            var falseValue = expression.Substring(colonIndex + 1).Trim();

            bool conditionResult = EvaluateCondition(item, condition);

            string selectedBranch = conditionResult ? trueValue : falseValue;

            // Evaluar recursivamente si hay mÃ¡s ternarios
            if (selectedBranch.Contains('?'))
                return EvaluateTernaryExpression(item, selectedBranch);

            return selectedBranch;
        }
        catch
        {
            return expression.Trim();
        }
    }

    /// <summary>
    /// Encuentra el ':' que corresponde al '?' (manejando ternarios anidados)
    /// </summary>
    private int FindMatchingColon(string expression, int questionIndex)
    {
        int depth = 0;
        for (int i = questionIndex + 1; i < expression.Length; i++)
        {
            if (expression[i] == '?') depth++;
            else if (expression[i] == ':')
            {
                if (depth == 0) return i;
                depth--;
            }
        }
        return -1;
    }

    /// <summary>
    /// EvalÃºa condiciones con acceso a propiedades del Item
    /// Ejemplo: "Item.Estado == 1", "Item.PesoNeto > 1000"
    /// </summary>
    private bool EvaluateCondition(object item, string condition)
    {
        try
        {
            condition = condition.Trim();

            // Extraer operador
            string op = "";
            int opIndex = -1;

            foreach (var testOp in new[] { "==", "!=", ">=", "<=", ">", "<" })
            {
                opIndex = condition.IndexOf(testOp);
                if (opIndex != -1)
                {
                    op = testOp;
                    break;
                }
            }

            if (opIndex == -1)
            {
                // Sin operador, evaluar como booleano directo
                var value = EvaluateExpression(item, condition);
                return bool.TryParse(value, out bool boolResult) && boolResult;
            }

            // Dividir en partes izquierda y derecha
            var leftExpr = condition.Substring(0, opIndex).Trim();
            var rightExpr = condition.Substring(opIndex + op.Length).Trim();

            // Evaluar ambas partes
            var leftValue = EvaluateExpression(item, leftExpr);
            var rightValue = rightExpr.Trim('"', '\'');

            return CompareValues(leftValue, rightValue, op);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Compara dos valores usando el operador especificado
    /// </summary>
    private bool CompareValues(string leftValue, string rightValue, string op)
    {
        try
        {
            // Intentar comparación numÃ©rica
            if (double.TryParse(leftValue, out double numLeft) &&
                double.TryParse(rightValue, out double numRight))
            {
                return op switch
                {
                    "==" => Math.Abs(numLeft - numRight) < 0.0001,
                    "!=" => Math.Abs(numLeft - numRight) >= 0.0001,
                    ">" => numLeft > numRight,
                    "<" => numLeft < numRight,
                    ">=" => numLeft >= numRight,
                    "<=" => numLeft <= numRight,
                    _ => false
                };
            }

            // Comparación de strings
            return op switch
            {
                "==" => string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private object ConvertToIcon(string value)
    {
        return Enum.TryParse<PackIconKind>(value, true, out var icon)
            ? icon
            : PackIconKind.HelpCircle;
    }

    private object ConvertToColor(string value)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    private object GetDefaultValue(Type targetType)
    {
        return targetType.Name switch
        {
            nameof(PackIconKind) => PackIconKind.HelpCircle,
            nameof(Brush) or nameof(SolidColorBrush) => new SolidColorBrush(Colors.Gray),
            _ => "N/A"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }


}

