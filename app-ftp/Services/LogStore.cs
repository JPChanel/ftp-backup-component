using app_ftp.Config;
using app_ftp.Services.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace app_ftp.Services;

public class LogStore
{
    private readonly AppDataPaths _paths;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public LogStore(AppDataPaths paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<BackupLogEntry> Load()
    {
        if (!File.Exists(_paths.LogsFile))
        {
            return [];
        }

        var logs = JsonSerializer.Deserialize<List<BackupLogEntry>>(File.ReadAllText(_paths.LogsFile), _serializerOptions)
            ?? [];

        foreach (var log in logs)
        {
            if (!string.IsNullOrWhiteSpace(log.ExecutionDetails))
            {
                continue;
            }

            var detailsPath = ResolveDetailsPath(log.ExecutionDetailsFilePath);
            if (detailsPath is null || !File.Exists(detailsPath))
            {
                continue;
            }

            log.ExecutionDetailsFullPath = detailsPath;
            log.ExecutionDetails = File.ReadAllText(detailsPath);
        }

        return logs;
    }

    public void Save(IEnumerable<BackupLogEntry> logs)
    {
        Directory.CreateDirectory(_paths.LogDetailsDirectory);

        var payload = new List<BackupLogEntry>();
        var referencedDetailFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var log in logs)
        {
            var detailFilePath = ResolveOrCreateDetailsPath(log);
            if (!string.IsNullOrWhiteSpace(log.ExecutionDetails) && detailFilePath is not null)
            {
                File.WriteAllText(detailFilePath, log.ExecutionDetails);
            }

            if (!string.IsNullOrWhiteSpace(detailFilePath))
            {
                referencedDetailFiles.Add(detailFilePath);
                log.ExecutionDetailsFilePath = Path.GetFileName(detailFilePath);
                log.ExecutionDetailsFullPath = detailFilePath;
            }

            payload.Add(CloneForStorage(log, detailFilePath));
        }

        CleanupOrphanDetailFiles(referencedDetailFiles);

        var json = JsonSerializer.Serialize(payload, _serializerOptions);
        File.WriteAllText(_paths.LogsFile, json);
    }

    private BackupLogEntry CloneForStorage(BackupLogEntry log, string? detailFilePath)
    {
        return new BackupLogEntry
        {
            Id = log.Id,
            Timestamp = log.Timestamp,
            Operation = log.Operation,
            SourceName = log.SourceName,
            DestinationName = log.DestinationName,
            Status = log.Status,
            Message = log.Message,
            ExecutionDetails = string.Empty,
            ExecutionDetailsFilePath = detailFilePath is null ? null : Path.GetFileName(detailFilePath),
            FilesTransferred = log.FilesTransferred,
            FilesSkipped = log.FilesSkipped,
            SourceFilesDeleted = log.SourceFilesDeleted,
            BytesTransferred = log.BytesTransferred,
            ErrorDetail = log.ErrorDetail
        };
    }

    private string? ResolveOrCreateDetailsPath(BackupLogEntry log)
    {
        var existingPath = ResolveDetailsPath(log.ExecutionDetailsFilePath);
        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            return existingPath;
        }

        if (string.IsNullOrWhiteSpace(log.ExecutionDetails))
        {
            return null;
        }

        var safeId = SanitizeFileName(string.IsNullOrWhiteSpace(log.Id) ? $"log_{log.Timestamp:yyyyMMddHHmmss}" : log.Id);
        var fileName = $"{log.Timestamp:yyyyMMdd_HHmmss}_{safeId}.txt";
        return Path.Combine(_paths.LogDetailsDirectory, fileName);
    }

    private string? ResolveDetailsPath(string? detailsPath)
    {
        if (string.IsNullOrWhiteSpace(detailsPath))
        {
            return null;
        }

        return Path.IsPathRooted(detailsPath)
            ? detailsPath
            : Path.Combine(_paths.LogDetailsDirectory, detailsPath);
    }

    private void CleanupOrphanDetailFiles(HashSet<string> referencedDetailFiles)
    {
        if (!Directory.Exists(_paths.LogDetailsDirectory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_paths.LogDetailsDirectory, "*.txt"))
        {
            if (referencedDetailFiles.Contains(file))
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignorar bloqueos o errores puntuales de I/O para no romper guardado de logs.
            }
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegex = new Regex($"[{invalidChars}]");
        return invalidRegex.Replace(value, "_");
    }
}
