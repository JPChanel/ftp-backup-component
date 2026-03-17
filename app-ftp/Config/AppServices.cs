using app_ftp.Interface;
using app_ftp.Presentacion.ViewModels;
using app_ftp.Services;
using app_ftp.Services.Endpoints;
using app_ftp.Services.Models;
using app_ftp.Services.Protocols;
using app_ftp.Services.Updates;
namespace app_ftp.Config;

public class AppServices
{
    public required MainViewModel MainViewModel { get; init; }
    public required ExceptionMiddleware ExceptionMiddleware { get; init; }

    public static AppServices Create()
    {
        var paths = new AppDataPaths();
        var protector = new CredentialProtector();
        var connectionStore = new ConnectionStore(paths, protector);
        var settingsStore = new SettingsStore(paths);
        var logStore = new LogStore(paths);
        var notifier = new MainThreadNotifier();
        IFtpService ftpService = new FtpServiceRepository();
        ISftpService sftpService = new SftpServiceRepository();
        IConnectionTester connectionTester = new ConnectionTester();
        IStorageEndpointFactory endpointFactory = new StorageEndpointFactory(ftpService, sftpService);
        var orchestrator = new BackupOrchestrator(endpointFactory);

        var checkUpdates = new CheckForUpdatesUseCase();
        var downloadUpdates = new DownloadUpdateUseCase();
        var installUpdates = new InstallUpdateUseCase();

        List<BackupLogEntry> currentLogs = logStore.Load().ToList();
        var exceptionMiddleware = new ExceptionMiddleware(logStore, notifier, () => currentLogs);

        var viewModel = new MainViewModel(connectionStore, settingsStore, logStore, orchestrator, connectionTester, notifier, checkUpdates, downloadUpdates, installUpdates);
        currentLogs = viewModel.Logs.ToList();
        notifier.StatusPublished += (_, _) => currentLogs = viewModel.Logs.ToList();

        return new AppServices
        {
            MainViewModel = viewModel,
            ExceptionMiddleware = exceptionMiddleware
        };
    }
}
