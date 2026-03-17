using app_ftp.Presentacion.Common;

namespace app_ftp.Services.Models;

public class AppSettings : ObservableObject
{
    private string _applicationName = "UtiBackup";
    private string _nextSyncEstimate = string.Empty;
    private int _maxVisibleLogs = 200;
    private string _localBackupRoot = string.Empty;

    public string ApplicationName { get => _applicationName; set => SetProperty(ref _applicationName, value); }
    public string NextSyncEstimate { get => _nextSyncEstimate; set => SetProperty(ref _nextSyncEstimate, value); }
    public int MaxVisibleLogs { get => _maxVisibleLogs; set => SetProperty(ref _maxVisibleLogs, value); }
    public string LocalBackupRoot { get => _localBackupRoot; set => SetProperty(ref _localBackupRoot, value); }
}
