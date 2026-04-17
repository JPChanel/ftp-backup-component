namespace app_ftp.Presentacion.Models;

public sealed class ToastNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Message { get; init; } = string.Empty;

    public bool IsError { get; init; }
}
