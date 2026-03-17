using System.IO;

namespace app_ftp.Config;

public class AppDataPaths
{
    public AppDataPaths()
    {
        Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UtiBackup");
        ConnectionsFile = Path.Combine(Root, "connections.json");
        LogsFile = Path.Combine(Root, "logs.json");
        LogDetailsDirectory = Path.Combine(Root, "logs-details");
        SettingsFile = Path.Combine(Root, "settings.json");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogDetailsDirectory);
    }

    public string Root { get; }
    public string ConnectionsFile { get; }
    public string LogsFile { get; }
    public string LogDetailsDirectory { get; }
    public string SettingsFile { get; }
}
