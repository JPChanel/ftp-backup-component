using MaterialDesignThemes.Wpf;

namespace app_ftp.Presentacion.Shared.Entities;

public class MenuItem
{
    public string Text { get; set; }
    public PackIconKind IconKind { get; set; }
    public string ModuleName { get; set; }
    public string? Badge { get; set; } // Para mostrar badges como "DEV" o "PROD"
}

