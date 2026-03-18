using Velopack;
using Velopack.Sources;

namespace app_ftp.Services.Updates;

public class InstallUpdateUseCase
{
    private readonly UpdateManager? _updateManager;
    private const string UpdateUrl = "https://github.com/JPChanel/ftp-backup-component";

    public InstallUpdateUseCase()
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

    public void Execute(UpdateInfo updateInfo)
    {
        try
        {
            if (_updateManager == null)
                return;

            _updateManager.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (Exception)
        {
            // Ignore error
        }
    }
}
