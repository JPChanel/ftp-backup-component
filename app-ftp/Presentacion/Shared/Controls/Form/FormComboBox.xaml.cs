using System.Collections;
using System.Windows.Data;
using System.Windows.Markup;
using UserControl = System.Windows.Controls.UserControl;

namespace app_ftp.Presentacion.Shared.Controls.Form
{
    [ContentProperty(nameof(InlineOptions))]
    public partial class FormComboBox : UserControl, IAddChild
    {
        public List<ComboBoxOption> InlineOptions { get; } = new();

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty, OnLabelChanged));

        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register(nameof(Options), typeof(IEnumerable), typeof(FormComboBox),
                new PropertyMetadata(null, OnOptionsChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty ExtDataProperty =
            DependencyProperty.Register(nameof(ExtData), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty RequiredProperty =
            DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormComboBox),
                new PropertyMetadata(false, OnRequiredChanged));

        public static readonly DependencyProperty DisplayLabelProperty =
            DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CustomStyleProperty =
            DependencyProperty.Register(nameof(CustomStyle), typeof(Style), typeof(FormComboBox),
                new PropertyMetadata(null, OnCustomStyleChanged));

        public static readonly DependencyProperty IsFilterEnabledProperty =
            DependencyProperty.Register(nameof(IsFilterEnabled), typeof(bool), typeof(FormComboBox),
                new PropertyMetadata(false, OnIsFilterEnabledChanged));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public IEnumerable Options
        {
            get => (IEnumerable)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public object ExtData
        {
            get => GetValue(ExtDataProperty);
            set => SetValue(ExtDataProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public bool Required
        {
            get => (bool)GetValue(RequiredProperty);
            set => SetValue(RequiredProperty, value);
        }

        public string DisplayLabel
        {
            get => (string)GetValue(DisplayLabelProperty);
            private set => SetValue(DisplayLabelProperty, value);
        }

        public string HelperText
        {
            get => (string)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }

        // Usar la propiedad IsEnabled heredada (no volver a declararla)

        public Style CustomStyle
        {
            get => (Style)GetValue(CustomStyleProperty);
            set => SetValue(CustomStyleProperty, value);
        }

        public bool IsFilterEnabled
        {
            get => (bool)GetValue(IsFilterEnabledProperty);
            set => SetValue(IsFilterEnabledProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => ComboBoxControl.IsDropDownOpen;
            set => ComboBoxControl.IsDropDownOpen = value;
        }

        public FormComboBox()
        {
            InitializeComponent();
            UpdateDisplayLabel();

            // Suscribirse al evento Loaded para sincronizar el valor seleccionado
            Loaded += FormComboBox_Loaded;

            // âœ… Suscribirse al evento Unloaded para limpiar el filtro
            Unloaded += FormComboBox_Unloaded;

            // Suscribirse al evento SelectionChanged para actualizar ExtData y SelectedItem
            ComboBoxControl.SelectionChanged += ComboBoxControl_SelectionChanged;

            // Manejar filtrado
            ComboBoxControl.KeyUp += ComboBoxControl_KeyUp;

            // Reenviar foco al control interno cuando el UserControl recibe el foco
            GotFocus += (s, e) =>
            {
                if (e.OriginalSource == this)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ComboBoxControl.IsEditable)
                        {
                            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);
                            if (textBox != null)
                            {
                                Keyboard.Focus(textBox);
                                textBox.CaretIndex = textBox.Text.Length;
                                return;
                            }
                        }
                        Keyboard.Focus(ComboBoxControl);
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            };
        }

        private void FormComboBox_Unloaded(object sender, RoutedEventArgs e)
        {
            // âœ… Limpiar el filtro del CollectionView cuando se descarga el control
            if (IsFilterEnabled && ComboBoxControl.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                if (view != null)
                {
                    view.Filter = null;
                }
            }
        }

        private void ComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Actualizar ExtData y SelectedItem cuando cambia la selecciÃ³n
            if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
            {
                ExtData = option.Ext;
                SelectedItem = option;
            }
            else
            {
                ExtData = null;
                SelectedItem = ComboBoxControl.SelectedItem;
            }
        }

        private void FormComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Forzar la sincronizaciÃ³n del valor seleccionado despuÃ©s de que el control estÃ© cargado
            // Esto asegura que el binding funcione correctamente cuando el valor se setea antes de ShowDialog()
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Normalizar los valores de las opciones para asegurar comparaciÃ³n correcta
                // Convertir JsonElement y otros tipos numÃ©ricos a sus tipos base
                if (ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option && option.Value != null)
                        {
                            var originalValue = option.Value;
                            var originalType = originalValue.GetType();

                            // CRÃTICO: Manejar JsonElement (viene de deserializaciÃ³n JSON de API)
                            if (originalType.Name == "JsonElement")
                            {
                                var jsonElement = (System.Text.Json.JsonElement)originalValue;

                                // Convertir segÃºn el tipo del JsonElement
                                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    if (jsonElement.TryGetInt32(out int intValue))
                                    {
                                        option.Value = intValue;
                                    }
                                    else if (jsonElement.TryGetInt64(out long longValue))
                                    {
                                        option.Value = (int)longValue;
                                    }
                                }
                                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    option.Value = jsonElement.GetString();
                                }
                            }
                            // Convertir otros tipos numÃ©ricos a int
                            else if (originalValue is long l)
                            {
                                option.Value = (int)l;
                            }
                            else if (originalValue is decimal d)
                            {
                                option.Value = (int)d;
                            }
                            else if (originalValue is double db)
                            {
                                option.Value = (int)db;
                            }
                            else if (originalValue is float f)
                            {
                                option.Value = (int)f;
                            }
                        }
                    }
                }

                // Sincronizar el valor seleccionado
                if (Value != null && ComboBoxControl.ItemsSource != null)
                {
                    // Normalizar el valor actual si es necesario
                    var normalizedValue = Value;
                    if (Value is long l)
                        normalizedValue = (int)l;
                    else if (Value is decimal d)
                        normalizedValue = (int)d;
                    else if (Value is double db)
                        normalizedValue = (int)db;
                    else if (Value is float f)
                        normalizedValue = (int)f;

                    ComboBoxControl.SelectedValue = normalizedValue;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        public void AddChild(object value)
        {
            if (value is ComboBoxOption option)
                InlineOptions.Add(option);
        }

        public void AddText(string text)
        {
            // No se usa, pero debe existir
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (InlineOptions.Count > 0 && (Options == null))
            {
                ComboBoxControl.ItemsSource = InlineOptions;
                ComboBoxControl.DisplayMemberPath = "Label";
                ComboBoxControl.SelectedValuePath = "Value";
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox combo) combo.UpdateDisplayLabel();
        }

        private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox combo) combo.UpdateDisplayLabel();
        }

        private void UpdateDisplayLabel() => DisplayLabel = Required ? $"{Label} *" : Label;

        private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control)
            {
                var items = e.NewValue as IEnumerable;

                // Si Options es vÃ¡lido (aunque estÃ© vacÃ­o, para soportar ObservableCollection)
                if (items != null)
                {
                    control.ComboBoxControl.ItemsSource = items;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";

                    // Si el filtro estÃ¡ habilitado, configurar la vista de colecciÃ³n
                    if (control.IsFilterEnabled)
                    {
                        control.SetupFiltering();
                    }
                }
                // Si no hay Options pero sÃ­ opciones inline
                else if (control.InlineOptions.Count > 0)
                {
                    control.ComboBoxControl.ItemsSource = control.InlineOptions;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";
                }
                else
                {
                    control.ComboBoxControl.ItemsSource = null;
                }
            }
        }

        private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue is Style style)
                control.ComboBoxControl.Style = style;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue != null)
            {
                // Cuando Value cambia, actualizar ExtData si encontramos el item correspondiente
                if (control.ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in control.ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option &&
                            option.Value != null &&
                            option.Value.Equals(e.NewValue))
                        {
                            control.ExtData = option.Ext;
                            control.SelectedItem = option;
                            break;
                        }
                    }
                }
            }
        }

        private static void OnIsFilterEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control)
            {
                bool isEnabled = (bool)e.NewValue;
                control.ComboBoxControl.IsEditable = isEnabled;
                control.ComboBoxControl.IsTextSearchEnabled = !isEnabled;

                if (isEnabled)
                {
                    control.SetupFiltering();
                }
            }
        }

        private void SetupFiltering()
        {
            if (ComboBoxControl.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);

            // âœ… Solo asignar el filtro si no estÃ¡ ya asignado para evitar redundancia
            if (view.Filter != FilterPredicate)
            {
                view.Filter = FilterPredicate;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrEmpty(ComboBoxControl.Text)) return true;

            if (obj is Core.Shared.Entities.SelectOption option)
            {
                return option.Label.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase);
            }

            if (obj is ComboBoxOption inlineOption)
            {
                return inlineOption.Label.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase);
            }

            return obj.ToString()?.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private void ComboBoxControl_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsFilterEnabled) return;

            // Ignorar teclas de navegaciÃ³n para no refrescar el filtro innecesariamente
            if (e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Up ||
                e.Key == System.Windows.Input.Key.Enter ||
                e.Key == System.Windows.Input.Key.Tab ||
                e.Key == System.Windows.Input.Key.Left ||
                e.Key == System.Windows.Input.Key.Right)
            {
                return;
            }

            // Obtener el TextBox interno para gestionar el cursor y la selecciÃ³n
            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);

            // Guardar posiciÃ³n del cursor y selecciÃ³n
            int caretIndex = textBox?.CaretIndex ?? 0;
            int selectionLength = textBox?.SelectionLength ?? 0;

            // Si el texto ha cambiado y ya no coincide con la selecciÃ³n actual, limpiar la selecciÃ³n
            // Esto evita que al refrescar la vista, el ComboBox restaure el texto del item seleccionado
            if (ComboBoxControl.SelectedItem != null)
            {
                string currentLabel = string.Empty;
                if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
                    currentLabel = option.Label;
                else if (ComboBoxControl.SelectedItem is ComboBoxOption inlineOption)
                    currentLabel = inlineOption.Label;
                else
                    currentLabel = ComboBoxControl.SelectedItem.ToString() ?? string.Empty;

                if (!string.Equals(ComboBoxControl.Text, currentLabel, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxControl.SelectedItem = null;
                }
            }

            // Actualizar filtro al escribir
            var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
            if (view != null)
            {
                view.Refresh();

                // Abrir dropdown si hay texto y no estÃ¡ abierto
                if (!string.IsNullOrEmpty(ComboBoxControl.Text) && !ComboBoxControl.IsDropDownOpen)
                {
                    ComboBoxControl.IsDropDownOpen = true;
                }

                // Restaurar posiciÃ³n del cursor y limpiar selecciÃ³n para evitar sobrescritura
                if (textBox != null)
                {
                    textBox.CaretIndex = caretIndex;
                    textBox.SelectionLength = 0;
                }
            }
        }

        private void ComboBoxControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsFilterEnabled) return;

            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (!ComboBoxControl.IsDropDownOpen)
                {
                    ComboBoxControl.IsDropDownOpen = true;
                    e.Handled = true;
                    return;
                }

                // NavegaciÃ³n manual hacia abajo
                var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                if (view != null)
                {
                    var items = view.Cast<object>().ToList();
                    if (items.Count > 0)
                    {
                        if (ComboBoxControl.SelectedItem == null)
                        {
                            // Si no hay nada seleccionado, seleccionar el primero
                            ComboBoxControl.SelectedItem = items[0];
                        }
                        else
                        {
                            // Buscar el Ã­ndice actual y mover al siguiente
                            int index = items.IndexOf(ComboBoxControl.SelectedItem);
                            if (index < items.Count - 1)
                            {
                                ComboBoxControl.SelectedItem = items[index + 1];
                            }
                        }

                        // Asegurar que el texto se actualice y el cursor vaya al final
                        UpdateTextAndCaret();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                if (ComboBoxControl.IsDropDownOpen)
                {
                    // NavegaciÃ³n manual hacia arriba
                    var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                    if (view != null)
                    {
                        var items = view.Cast<object>().ToList();
                        if (items.Count > 0 && ComboBoxControl.SelectedItem != null)
                        {
                            int index = items.IndexOf(ComboBoxControl.SelectedItem);
                            if (index > 0)
                            {
                                ComboBoxControl.SelectedItem = items[index - 1];
                                UpdateTextAndCaret();
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                if (ComboBoxControl.IsDropDownOpen)
                {
                    // Si hay un item seleccionado (navegaciÃ³n con flechas), usar ese
                    if (ComboBoxControl.SelectedItem != null)
                    {
                        // Ya estÃ¡ seleccionado, solo cerrar si es Enter
                        if (e.Key == System.Windows.Input.Key.Enter)
                        {
                            ComboBoxControl.IsDropDownOpen = false;
                            e.Handled = true;
                        }
                        return;
                    }

                    // Si no hay selecciÃ³n, tomar el primero del filtro
                    var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                    if (view != null)
                    {
                        var firstItem = view.Cast<object>().FirstOrDefault();
                        if (firstItem != null)
                        {
                            ComboBoxControl.SelectedItem = firstItem;
                            UpdateTextAndCaret();
                        }
                    }

                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        ComboBoxControl.IsDropDownOpen = false;
                        e.Handled = true;
                    }
                }
            }
        }

        private void UpdateTextAndCaret()
        {
            // Actualizar texto visualmente (aunque el binding lo haga, forzamos para el caret)
            if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
                ComboBoxControl.Text = option.Label;
            else if (ComboBoxControl.SelectedItem is ComboBoxOption inlineOption)
                ComboBoxControl.Text = inlineOption.Label;

            // Mover cursor al final
            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);
            if (textBox != null)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    public class ComboBoxOption : DependencyObject
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public object? Ext { get; set; }
    }
}

