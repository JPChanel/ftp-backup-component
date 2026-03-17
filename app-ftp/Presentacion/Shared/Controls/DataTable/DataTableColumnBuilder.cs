using MaterialDesignThemes.Wpf;
using System.Linq.Expressions;

namespace app_ftp.Presentacion.Shared.Controls.DataTable;

/// <summary>
/// Builder fluido para crear columnas de DataTable con IntelliSense y type-safety
/// </summary>
/// <typeparam name="T">Tipo del modelo de datos</typeparam>
public class DataTableColumnBuilder<T>
{
    public readonly DataTableColumn _column = new();

    /// <summary>
    /// Establece la propiedad a mostrar usando una expresiÃ³n lambda (con IntelliSense)
    /// </summary>
    /// <param name="propertyExpression">ExpresiÃ³n lambda que apunta a la propiedad (ej: x => x.Pes.pes_des)</param>
    public DataTableColumnBuilder<T> ForProperty<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        _column.PropertyName = GetPropertyPath(propertyExpression);
        return this;
    }

    /// <summary>
    /// Alias corto para ForProperty - Establece la propiedad con IntelliSense
    /// </summary>
    public DataTableColumnBuilder<T> Key<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        return ForProperty(propertyExpression);
    }

    /// <summary>
    /// Establece el encabezado de la columna
    /// </summary>
    public DataTableColumnBuilder<T> WithHeader(string header)
    {
        _column.Header = header;
        return this;
    }

    /// <summary>
    /// Alias corto para WithHeader
    /// </summary>
    public DataTableColumnBuilder<T> Header(string header)
    {
        return WithHeader(header);
    }

    /// <summary>
    /// Establece el ancho de la columna
    /// </summary>
    public DataTableColumnBuilder<T> WithWidth(string width)
    {
        _column.Width = width;
        return this;
    }

    /// <summary>
    /// Alias corto para WithWidth
    /// </summary>
    public DataTableColumnBuilder<T> Width(string width)
    {
        return WithWidth(width);
    }

    /// <summary>
    /// Establece el formato de visualizaciÃ³n
    /// </summary>
    public DataTableColumnBuilder<T> WithFormat(string format)
    {
        _column.StringFormat = format;
        return this;
    }

    /// <summary>
    /// Alias corto para WithFormat
    /// </summary>
    public DataTableColumnBuilder<T> Format(string format)
    {
        return WithFormat(format);
    }

    /// <summary>
    /// Establece la alineaciÃ³n horizontal
    /// </summary>
    public DataTableColumnBuilder<T> WithAlignment(string alignment)
    {
        _column.HorizontalAlignment = alignment;
        return this;
    }

    /// <summary>
    /// Alias corto para WithAlignment
    /// </summary>
    public DataTableColumnBuilder<T> Align(string alignment)
    {
        return WithAlignment(alignment);
    }

    /// <summary>
    /// Establece el tipo de columna
    /// </summary>
    public DataTableColumnBuilder<T> AsType(DataTableColumnType type)
    {
        _column.ColumnType = type;
        return this;
    }

    /// <summary>
    /// Configura como columna de texto
    /// </summary>
    public DataTableColumnBuilder<T> AsText()
    {
        _column.ColumnType = DataTableColumnType.Text;
        return this;
    }

    /// <summary>
    /// Configura como columna de nÃºmero
    /// </summary>
    public DataTableColumnBuilder<T> AsNumber(string? format = "N2")
    {
        _column.ColumnType = DataTableColumnType.Number;
        _column.StringFormat = format;
        _column.HorizontalAlignment = "Right";
        return this;
    }

    /// <summary>
    /// Configura como columna de fecha
    /// </summary>
    public DataTableColumnBuilder<T> AsDate(string? format = "dd/MM/yyyy")
    {
        _column.ColumnType = DataTableColumnType.Date;
        _column.StringFormat = format;
        return this;
    }

    /// <summary>
    /// Configura como columna de moneda
    /// </summary>
    public DataTableColumnBuilder<T> AsCurrency(string? format = "C2")
    {
        _column.ColumnType = DataTableColumnType.Currency;
        _column.StringFormat = format;
        _column.HorizontalAlignment = "Right";
        return this;
    }

    /// <summary>
    /// Configura como columna de hipervÃ­nculo
    /// </summary>
    public DataTableColumnBuilder<T> AsHyperlink(System.Windows.Input.ICommand command, string? tooltip = null)
    {
        _column.ColumnType = DataTableColumnType.Hyperlink;
        _column.HyperlinkCommand = command;
        _column.HyperlinkToolTip = tooltip;
        return this;
    }

    /// <summary>
    /// Configura como columna de template
    /// </summary>
    public DataTableColumnBuilder<T> AsTemplate(string templateKey)
    {
        _column.ColumnType = DataTableColumnType.Template;
        _column.TemplateKey = templateKey;
        return this;
    }

    /// <summary>
    /// Configura como columna booleana con iconos de estado
    /// </summary>
    public DataTableColumnBuilder<T> AsBooleanStatus(
        PackIconKind trueIcon = PackIconKind.CheckCircleOutline,
        PackIconKind falseIcon = PackIconKind.CloseCircleOutline,
        string? trueColor = "#4CAF50",
        string? falseColor = "#F44336",
        string? trueText = "Completado",
        string? falseText = "Pendiente")
    {
        _column.ColumnType = DataTableColumnType.BooleanStatus;
        _column.Status = new StatusIndicator
        {
            BooleanTrueIcon = trueIcon,
            BooleanFalseIcon = falseIcon,
            BooleanTrueColor = trueColor,
            BooleanFalseColor = falseColor,
            BooleanTrueText = trueText,
            BooleanFalseText = falseText
        };
        _column.HorizontalAlignment = "Center";
        return this;
    }

    /// <summary>
    /// Establece la prioridad de visualizaciÃ³n
    /// </summary>
    public DataTableColumnBuilder<T> WithPriority(int priority)
    {
        _column.DisplayPriority = priority;
        return this;
    }

    /// <summary>
    /// Alias corto para WithPriority
    /// </summary>
    public DataTableColumnBuilder<T> Priority(int priority)
    {
        return WithPriority(priority);
    }

    /// <summary>
    /// Establece si la columna muestra totales
    /// </summary>
    public DataTableColumnBuilder<T> WithTotal(bool showTotal = true)
    {
        _column.ShowTotal = showTotal;
        return this;
    }

    /// <summary>
    /// Alias corto para WithTotal
    /// </summary>
    public DataTableColumnBuilder<T> Total(bool showTotal = true)
    {
        return WithTotal(showTotal);
    }

    /// <summary>
    /// Establece si la columna es ordenable
    /// </summary>
    public DataTableColumnBuilder<T> WithSorting(bool canSort = true)
    {
        _column.CanSort = canSort;
        return this;
    }

    /// <summary>
    /// Alias corto para WithSorting
    /// </summary>
    public DataTableColumnBuilder<T> Sortable(bool canSort = true)
    {
        return WithSorting(canSort);
    }

    /// <summary>
    /// Agrega un botÃ³n de acciÃ³n a la columna
    /// </summary>
    public DataTableColumnBuilder<T> AddAction(
        PackIconKind icon,
        System.Windows.Input.ICommand command,
        string tooltip = "",
        int iconSize = 24,
        string? colorHex = null,
        Func<object?, bool>? isVisible = null)
    {
        _column.ColumnType = DataTableColumnType.Actions;
        _column.HorizontalAlignment = "Center";
        _column.CanSort = false;

        _column.ActionButtons.Add(new DataTableActionButton
        {
            Icon = icon,
            Command = command,
            Tooltip = tooltip,
            IconSize = iconSize,
            Foreground = colorHex != null ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)) : null,
            IsVisible = isVisible,
            Disabled = null
        });

        return this;
    }

    /// <summary>
    /// Alias corto para AddAction
    /// </summary>
    public DataTableColumnBuilder<T> Action(
        PackIconKind icon,
        System.Windows.Input.ICommand command,
        string tooltip = "",
        int iconSize = 24,
        string? colorHex = null,
        string? visibilityProperty = null)
    {
        // Si visibilityProperty no es nulo, crea un delegado que evalÃºe la propiedad por reflexiÃ³n
        Func<object?, bool>? isVisible = null;
        if (!string.IsNullOrEmpty(visibilityProperty))
        {
            isVisible = (obj) =>
            {
                if (obj == null) return false;
                var prop = obj.GetType().GetProperty(visibilityProperty);
                if (prop == null) return false;
                var value = prop.GetValue(obj);
                if (value is bool b) return b;
                if (value is bool?) return ((bool?)value) ?? false;
                return false;
            };
        }
        return AddAction(icon, command, tooltip, iconSize, colorHex, isVisible);
    }

    /// <summary>
    /// Construye y retorna la columna configurada
    /// </summary>
    public DataTableColumn Build()
    {
        return _column;
    }

    /// <summary>
    /// ConversiÃ³n implÃ­cita para poder usar el builder directamente en colecciones
    /// </summary>
    public static implicit operator DataTableColumn(DataTableColumnBuilder<T> builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Extrae la ruta de la propiedad desde la expresiÃ³n lambda
    /// </summary>
    private static string GetPropertyPath<TProp>(Expression<Func<T, TProp>> expression)
    {
        var memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null)
        {
            // Maneja conversiones (ej: x => (object)x.Property)
            if (expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
        }

        if (memberExpression == null)
            throw new ArgumentException("La expresiÃ³n debe ser una propiedad", nameof(expression));

        // Construir la ruta completa (ej: "Pes.pes_des")
        var path = new System.Collections.Generic.List<string>();
        while (memberExpression != null)
        {
            path.Insert(0, memberExpression.Member.Name);
            memberExpression = memberExpression.Expression as MemberExpression;
        }

        return string.Join(".", path);
    }
}

/// <summary>
/// Clase estÃ¡tica para crear builders de forma mÃ¡s limpia
/// </summary>
public static class DataTableColumnBuilder
{
    /// <summary>
    /// Crea un nuevo builder para columnas de DataTable con IntelliSense
    /// </summary>
    public static DataTableColumnBuilder<T> For<T>()
    {
        return new DataTableColumnBuilder<T>();
    }

    /// <summary>
    /// Crea un conjunto de columnas sin repetir el tipo genÃ©rico
    /// Uso: Columns&lt;PesajesItemDto&gt;(dto => dto.Pes, new[] { ... })
    /// </summary>
    public static System.Collections.ObjectModel.ObservableCollection<DataTableColumn> Columns<T, TEntity>(
        Expression<Func<T, TEntity>> entitySelector,
        params ColDef<TEntity>[] columns)
    {
        var collection = new System.Collections.ObjectModel.ObservableCollection<DataTableColumn>();

        foreach (var colDef in columns)
        {
            var builder = new DataTableColumnBuilder<T>();

            // Combinar entitySelector con la expresiÃ³n de la columna
            if (colDef.KeyExpression != null)
            {
                var combinedExpression = CombineExpressions(entitySelector, colDef.KeyExpression);
                builder.ForProperty(combinedExpression);
            }

            builder.WithHeader(colDef.Header)
                   .WithWidth(colDef.Width ?? "1*")
                   .WithPriority(colDef.Priority);

            if (colDef.Format != null)
                builder.WithFormat(colDef.Format);

            if (colDef.Align != null)
                builder.WithAlignment(colDef.Align);

            if (colDef.Command != null)
                builder.AsHyperlink(colDef.Command);
            else if (colDef.Template != null)
                builder.AsTemplate(colDef.Template);
            else if (colDef.Type != null)
                builder.AsType(colDef.Type.Value);

            if (colDef.Actions != null)
            {
                foreach (var action in colDef.Actions)
                {
                    builder.Action(action.Icon, action.Command, action.Tooltip ?? "", action.IconSize, action.Color, action.VisibilityProperty);
                }
            }

            collection.Add(builder.Build());
        }

        return collection;
    }

    private static Expression<Func<T, TProp>> CombineExpressions<T, TEntity, TProp>(
        Expression<Func<T, TEntity>> first,
        Expression<Func<TEntity, TProp>> second)
    {
        var parameter = first.Parameters[0];
        var body = System.Linq.Expressions.Expression.Invoke(second, first.Body);
        var combined = System.Linq.Expressions.Expression.Lambda<Func<T, TProp>>(body, parameter);
        return combined;
    }
}

/// <summary>
/// DefiniciÃ³n simplificada de columna sin tipo genÃ©rico repetido
/// </summary>
public class ColDef<TEntity>
{
    /// <summary>
    /// ConfiguraciÃ³n de estado para columnas BooleanStatus
    /// </summary>
    public StatusIndicator? Status { get; set; }

    internal Expression<Func<TEntity, object>>? KeyExpression { get; set; }

    /// <summary>
    /// Propiedad a mostrar (con IntelliSense del entity)
    /// </summary>
    public Expression<Func<TEntity, object>>? Key
    {
        get => KeyExpression;
        set => KeyExpression = value;
    }

    /// <summary>
    /// Encabezado de la columna
    /// </summary>
    public string Header { get; set; } = "";

    /// <summary>
    /// Ancho de la columna
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Formato (ej: "dd/MM/yyyy", "N2", "C2")
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// AlineaciÃ³n (Left, Center, Right)
    /// </summary>
    public string? Align { get; set; }

    /// <summary>
    /// Tipo de columna
    /// </summary>
    public DataTableColumnType? Type { get; set; }

    /// <summary>
    /// Tooltip estÃ¡tico (para compatibilidad con hipervÃ­nculos)
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// FunciÃ³n para determinar el tooltip dinÃ¡micamente basado en el item de la fila
    /// Ejemplo: Tooltip = x => x.baz_user
    /// </summary>
    public Func<TEntity, string?>? TooltipSelector { get; set; }

    /// <summary>
    /// Si se debe mostrar el total de esta columna
    /// </summary>
    public bool ShowTotal { get; set; } = false;
    /// <summary>
    /// Comando para hipervÃ­nculo
    /// </summary>
    public System.Windows.Input.ICommand? Command { get; set; }

    /// <summary>
    /// Clave del template
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Prioridad de visualizaciÃ³n
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Lista de acciones
    /// </summary>
    public List<ActionDef>? Actions { get; set; }
    public CellDisplayVariant Variant { get; set; }
    public string Color { get; set; } = string.Empty;
    public Func<TEntity, string?>? ColorSelector { get; set; }
    public PackIconKind Icon { get; set; } = PackIconKind.None;
    /// <summary>
    /// ConversiÃ³n implÃ­cita a DataTableColumn
    /// </summary>
    public static implicit operator DataTableColumn(ColDef<TEntity> colDef)
    {
        var builder = new DataTableColumnBuilder<TEntity>();

        if (colDef.KeyExpression != null)
            builder.ForProperty(colDef.KeyExpression);

        builder.WithHeader(colDef.Header)
               .WithWidth(colDef.Width ?? "1*")
               .WithPriority(colDef.Priority)
               .WithTotal(colDef.ShowTotal);

        if (colDef.Format != null)
            builder.WithFormat(colDef.Format);

        if (colDef.Align != null)
            builder.WithAlignment(colDef.Align);

        if (colDef.Command != null)
        {
            builder.AsHyperlink(colDef.Command, colDef.Tooltip);
            // Si hay TooltipSelector, configurarlo tambiÃ©n
            if (colDef.TooltipSelector != null)
            {
                builder._column.TooltipSelector = (obj) =>
                {
                    if (obj is TEntity entity)
                    {
                        return colDef.TooltipSelector(entity);
                    }
                    return null;
                };
            }
        }
        else if (colDef.Template != null)
        {
            builder.AsTemplate(colDef.Template);
        }
        else if (colDef.Type != null)
        {
            if (colDef.Type.Value == DataTableColumnType.BooleanStatus && colDef.Status != null)
            {
                builder._column.ColumnType = DataTableColumnType.BooleanStatus;
                builder._column.Status = colDef.Status;
                builder._column.HorizontalAlignment = "Center";
            }
            else
            {
                builder.AsType(colDef.Type.Value);
            }
        }

        if (colDef.Actions != null)
        {
            // Marcar la columna como tipo Actions
            builder._column.ColumnType = DataTableColumnType.Actions;
            builder._column.HorizontalAlignment = "Center";
            builder._column.CanSort = false;

            foreach (var action in colDef.Actions)
            {
                var actionButton = new DataTableActionButton
                {
                    Icon = action.Icon,
                    Command = action.Command,
                    Tooltip = action.Tooltip ?? "",
                    IconSize = action.IconSize,
                    Foreground = !string.IsNullOrEmpty(action.Color)
                        ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(action.Color))
                        : null,
                    Disabled = action.Disabled
                };
                builder._column.ActionButtons.Add(actionButton);
            }
        }

        // Mapear propiedades visuales avanzadas
        builder._column.Variant = colDef.Variant;
        builder._column.Color = colDef.Color;
        // Solo asignar icono si fue especificado explÃ­citamente (no el default None)
        builder._column.Icon = colDef.Icon;
        // Mapear ColorSelector si estÃ¡ definido
        if (colDef.ColorSelector != null)
        {
            builder._column.ColorSelector = (obj) =>
            {
                if (obj is TEntity entity)
                {
                    return colDef.ColorSelector(entity);
                }
                return null;
            };
        }

        // Mapear TooltipSelector si estÃ¡ definido (para columnas no-hyperlink)
        if (colDef.TooltipSelector != null && colDef.Command == null)
        {
            builder._column.TooltipSelector = (obj) =>
            {
                if (obj is TEntity entity)
                {
                    return colDef.TooltipSelector(entity);
                }
                return null;
            };
        }
        return builder.Build();
    }
}

/// <summary>
/// DefiniciÃ³n de una acciÃ³n para columnas tipo Actions
/// </summary>
public class ActionDef
{
    /// <summary>
    /// Icono de Material Design
    /// </summary>
    public PackIconKind Icon { get; set; }

    /// <summary>
    /// Comando a ejecutar
    /// </summary>
    public System.Windows.Input.ICommand Command { get; set; } = null!;

    /// <summary>
    /// Tooltip
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// TamaÃ±o del icono
    /// </summary>
    public int IconSize { get; set; } = 24;

    /// <summary>
    /// Color del icono en hexadecimal
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Propiedad que controla la visibilidad
    /// </summary>
    public string? VisibilityProperty { get; set; }
    /// <summary>
    /// FunciÃ³n que determina si el botÃ³n estÃ¡ deshabilitado
    /// Retorna true si el botÃ³n debe estar deshabilitado
    /// </summary>
    public Func<object?, bool>? Disabled { get; set; }
}

