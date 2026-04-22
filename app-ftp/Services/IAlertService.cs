using app_ftp.Presentacion.Models;
using MaterialDesignThemes.Wpf;

namespace app_ftp.Services;

public interface IAlertService
{
    event EventHandler<EstNotificationMessage> AlertRaised;

    void Show(string message, string title = "Notificacion", AlertVariant variant = AlertVariant.Info, PackIconKind? icon = null, TimeSpan? duration = null);
}
