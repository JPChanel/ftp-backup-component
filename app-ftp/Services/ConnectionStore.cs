using app_ftp.Config;
using app_ftp.Services.Models;
using System.IO;
using System.Text.Json;

namespace app_ftp.Services;

public class ConnectionStore
{
    private readonly AppDataPaths _paths;
    private readonly CredentialProtector _protector;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public ConnectionStore(AppDataPaths paths, CredentialProtector protector)
    {
        _paths = paths;
        _protector = protector;
    }

    public IReadOnlyList<ConnectionProfile> Load()
    {
        if (!File.Exists(_paths.ConnectionsFile))
        {
            return [];
        }

        var json = File.ReadAllText(_paths.ConnectionsFile);
        var items = JsonSerializer.Deserialize<List<ConnectionProfile>>(json, _serializerOptions) ?? new();
        foreach (var item in items)
        {
            item.Password = _protector.Unprotect(item.Password);
        }

        return items;
    }

    public void Save(IEnumerable<ConnectionProfile> connections)
    {
        var payload = connections.Select(connection =>
        {
            var clone = connection.Clone();
            clone.Password = _protector.Protect(clone.Password);
            return clone;
        }).ToList();

        var json = JsonSerializer.Serialize(payload, _serializerOptions);
        File.WriteAllText(_paths.ConnectionsFile, json);
    }
}
