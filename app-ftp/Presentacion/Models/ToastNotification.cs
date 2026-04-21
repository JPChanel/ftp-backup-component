namespace app_ftp.Presentacion.Models;

public enum ToastSeverity
{
    Success,
    Warning,
    Error
}

public sealed class ToastNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Message { get; init; } = string.Empty;

    public ToastSeverity Severity { get; init; } = ToastSeverity.Success;

    public bool IsWarning => Severity == ToastSeverity.Warning;

    public bool IsError => Severity == ToastSeverity.Error;
}
