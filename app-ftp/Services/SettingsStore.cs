using app_ftp.Config;
using app_ftp.Services.Models;
using System.Text.Json;

namespace app_ftp.Services;

public class SettingsStore
{
    private readonly AppDataPaths _paths;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public SettingsStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_paths.SettingsFile))
        {
            return new AppSettings();
        }

        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_paths.SettingsFile), _serializerOptions)
            ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(_paths.SettingsFile, json);
    }
}
