using Velopack;
using Velopack.Sources;

namespace app_ftp.Services.Updates;

public class CheckForUpdatesUseCase
{
    private readonly UpdateManager? _updateManager;
    private const string UpdateUrl = "https://github.com/JPChanel/ftp-backup-component";

    public CheckForUpdatesUseCase()
    {
        try
        {
            _updateManager = new UpdateManager(new GithubSource(UpdateUrl, null, false));
        }
        catch
        {
            _updateManager = null;
        }
    }

    public async Task<UpdateInfo?> ExecuteAsync()
    {
        try
        {
            if (_updateManager == null)
                return null;

            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            return updateInfo;
        }
        catch (Exception)
        {
            // Log error silently, UI will just show no updates available
            return null;
        }
    }

    public string GetCurrentVersion()
    {
        try
        {
            return _updateManager?.CurrentVersion?.ToString() ?? "Desarrollo";
        }
        catch
        {
            return "Desarrollo";
        }
    }
}
