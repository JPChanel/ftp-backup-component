using Velopack;
using Velopack.Sources;

namespace app_ftp.Services.Updates;

public class DownloadUpdateUseCase
{
    private readonly UpdateManager? _updateManager;
    private const string UpdateUrl = "https://github.com/JPChanel/ftp-backup-component";

    public DownloadUpdateUseCase()
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
