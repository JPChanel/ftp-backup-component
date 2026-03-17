using Velopack;

namespace app_ftp.Services.Updates;

public class DownloadUpdateUseCase
{
    private readonly UpdateManager? _updateManager;

    public DownloadUpdateUseCase()
    {
        try
        {
            _updateManager = new UpdateManager("https://github.com/JPChanel/ftp-backup-component");
        }
        catch
        {
            _updateManager = null;
        }
    }

    public async Task<bool> ExecuteAsync(UpdateInfo updateInfo, Action<int> progress)
    {
        try
        {
            if (_updateManager == null)
                return false;

            await _updateManager.DownloadUpdatesAsync(updateInfo, progress);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
