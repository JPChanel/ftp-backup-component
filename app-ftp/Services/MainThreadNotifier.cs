using app_ftp.Presentacion.Models;
using MaterialDesignThemes.Wpf;

namespace app_ftp.Services;

public class MainThreadNotifier : IAlertService
{
    public event Action<string, ToastSeverity>? StatusPublished;
    public event EventHandler<EstNotificationMessage>? AlertRaised;

    public void PublishSuccess(string message)
    {
        StatusPublished?.Invoke(message, ToastSeverity.Success);
        Show(message, "Correcto", AlertVariant.Success);
    }

    public void PublishWarning(string message)
    {
        StatusPublished?.Invoke(message, ToastSeverity.Warning);
        Show(message, "Advertencia", AlertVariant.Warning);
    }

    public void PublishError(string message)
    {
        StatusPublished?.Invoke(message, ToastSeverity.Error);
        Show(message, "Error", AlertVariant.Error, PackIconKind.AlertCircleOutline, TimeSpan.FromSeconds(6));
    }

    public void Show(string message, string title = "Notificacion", AlertVariant variant = AlertVariant.Info, PackIconKind? icon = null, TimeSpan? duration = null)
    {
        AlertRaised?.Invoke(this, new EstNotificationMessage
        {
            Message = message,
            Title = title,
            Variant = variant,
            IconKind = icon,
            Duration = duration
        });
    }
}
