namespace app_ftp.Presentacion.Shared.Controls.Loading
{
    /// <summary>
    /// Lógica de interacción para LoadingOverlay.xaml
    /// </summary>
    public partial class LoadingOverlay : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty MessageProperty =
         DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
             new PropertyMetadata("Procesando..."));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public LoadingOverlay()
        {
            InitializeComponent();
        }


    }
}

