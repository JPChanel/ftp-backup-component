using app_ftp.Presentacion.Models;

namespace app_ftp.Services;

public class MainThreadNotifier
{
    public event Action<string, ToastSeverity>? StatusPublished;

    public void PublishSuccess(string message) => StatusPublished?.Invoke(message, ToastSeverity.Success);

    public void PublishWarning(string message) => StatusPublished?.Invoke(message, ToastSeverity.Warning);

    public void PublishError(string message) => StatusPublished?.Invoke(message, ToastSeverity.Error);
}
