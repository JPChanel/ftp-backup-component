using app_ftp.Services.Models;
using System.Windows;

namespace app_ftp.Services;

public class ExceptionMiddleware
{
    private readonly LogStore _logStore;
    private readonly MainThreadNotifier _notifier;
    private readonly Func<IEnumerable<BackupLogEntry>> _getLogs;

    public ExceptionMiddleware(LogStore logStore, MainThreadNotifier notifier, Func<IEnumerable<BackupLogEntry>> getLogs)
    {
        _logStore = logStore;
        _notifier = notifier;
        _getLogs = getLogs;
    }

    public void Handle(Exception exception, string source)
    {
        var logs = _getLogs().ToList();
        logs.Insert(0, new BackupLogEntry
        {
            Id = $"ERR-{DateTime.Now:yyyyMMddHHmmss}",
            Timestamp = DateTime.Now,
            Operation = "Unhandled exception",
            SourceName = source,
            DestinationName = "Screen",
            Status = "ERROR",
            Message = exception.Message,
            ErrorDetail = exception.ToString()
        });

        _logStore.Save(logs);
        _notifier.PublishError($"Error no controlado: {exception.Message}");
        MessageBox.Show(exception.Message, "UtiBackup - Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
