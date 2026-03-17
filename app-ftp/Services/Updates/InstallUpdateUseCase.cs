using Velopack;

namespace app_ftp.Services.Updates;

public class InstallUpdateUseCase
{
    private readonly UpdateManager? _updateManager;

    public InstallUpdateUseCase()
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
