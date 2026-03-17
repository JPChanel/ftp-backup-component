using app_ftp.Config;
using app_ftp.Interface;
using app_ftp.Services.Models;
using System.IO;
using System.Text;

namespace app_ftp.Services;

public class BackupOrchestrator
{
    private readonly IStorageEndpointFactory _endpointFactory;

    public BackupOrchestrator(IStorageEndpointFactory endpointFactory)
    {
        _endpointFactory = endpointFactory;
    }

    public async Task<BackupLogEntry> ExecuteAsync(
        BackupExecutionRequest request,
        IProgress<BackupProgressEntry>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.Now;
        var detailBuilder = new StringBuilder();
        var log = new BackupLogEntry
        {
            Id = $"#{timestamp:MMddHHmmss}",
            Timestamp = timestamp,
            Operation = "Sync Backup",
            SourceName = request.Source.Name,
            DestinationName = request.Destination.Name
        };

        try
        {
            var sourceEndpoint = _endpointFactory.Create(request.Source);
            var destinationEndpoint = _endpointFactory.Create(request.Destination);
            var sourceTarget = NormalizePath(request.Source, request.SourcePath);
            var destinationRoot = NormalizeDestinationRoot(request.Destination, request.DestinationPath);

            Report(progress, detailBuilder, "SISTEMA", "RUTAS INICIALES", sourceTarget, destinationRoot);
            await ValidateRequestAsync(request, sourceEndpoint, destinationEndpoint, sourceTarget, destinationRoot, cancellationToken);

            var sourceItems = await ResolveSourceItemsAsync(sourceEndpoint, request.Source, sourceTarget, request.SourcePath, request.FilterFromDate, request.FilterToDate);
            Report(progress, detailBuilder, "SISTEMA", $"Preparados {sourceItems.Count} elemento(s) para backup.");

            foreach (var item in sourceItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationFullPath = CombinePath(request.Destination, request.DestinationPath, item.RelativePath);

                try
                {
                    var destinationExistedBefore = await destinationEndpoint.FileExistsAsync(destinationFullPath);
                    if (destinationExistedBefore)
                    {
                        if (!request.OverwriteExisting)
                        {
                            log.FilesSkipped++;
                            Report(progress, detailBuilder, item.RelativePath, "OMITIDO", item.FullPath, destinationFullPath);
                            continue;
                        }

                        var destinationModifiedAt = await destinationEndpoint.GetLastModifiedAsync(destinationFullPath);
                        if (destinationModifiedAt.HasValue && item.ModifiedAt <= destinationModifiedAt.Value)
                        {
                            log.FilesSkipped++;
                            Report(progress, detailBuilder, item.RelativePath, "OMITIDO", item.FullPath, destinationFullPath);
                            continue;
                        }
                    }

                    var bytes = await sourceEndpoint.DownloadBytesAsync(item.FullPath);
                    await destinationEndpoint.EnsureDirectoryAsync(destinationFullPath);
                    await destinationEndpoint.UploadBytesAsync(destinationFullPath, bytes, request.OverwriteExisting);
                    var destinationExists = await destinationEndpoint.FileExistsAsync(destinationFullPath);
                    if (!destinationExists)
                    {
                        throw new InvalidOperationException($"No se pudo verificar el archivo copiado en destino: {destinationFullPath}");
                    }

                    Report(progress, detailBuilder, item.RelativePath, "COPIADO", item.FullPath, destinationFullPath);

                    if (request.DeleteSourceAfterCopy)
                    {
                        if (destinationExistedBefore && !request.OverwriteExisting)
                        {
                            throw new InvalidOperationException("No es seguro eliminar el origen porque el archivo ya existia en destino y la sobrescritura esta desactivada.");
                        }

                        await sourceEndpoint.DeleteFileAsync(item.FullPath);
                        log.SourceFilesDeleted++;
                        Report(progress, detailBuilder, item.RelativePath, "ORIGEN ELIMINADO", item.FullPath, destinationFullPath);
                    }

                    log.FilesTransferred++;
                    log.BytesTransferred += bytes.LongLength;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Report(progress, detailBuilder, item.RelativePath, "ERROR", item.FullPath, destinationFullPath);
                    throw new InvalidOperationException($"Error procesando '{item.RelativePath}': {ex.Message}", ex);
                }
            }

            log.Status = "SUCCESS";
            log.Message = BuildSuccessMessage(request, log);
            Report(progress, detailBuilder, "SISTEMA", "BACKUP FINALIZADO");
        }
        catch (OperationCanceledException)
        {
            log.Status = "CANCELLED";
            log.Message = "Operacion cancelada por el usuario.";
            Report(progress, detailBuilder, "SISTEMA", "CANCELADO");
        }
        catch (Exception ex)
        {
            log.Status = "ERROR";
            log.Message = ex.Message;
            log.ErrorDetail = ex.ToString();
            Report(progress, detailBuilder, "SISTEMA", $"ERROR: {ex.Message}");
        }

        log.ExecutionDetails = detailBuilder.ToString().Trim();
        return log;
    }

    private async Task ValidateRequestAsync(
        BackupExecutionRequest request,
        IStorageEndpoint sourceEndpoint,
        IStorageEndpoint destinationEndpoint,
        string sourceTarget,
        string destinationRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.FilterFromDate.HasValue && request.FilterToDate.HasValue && request.FilterFromDate > request.FilterToDate)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser mayor que la fecha final.");
        }

        if (IsSameRoute(request, sourceTarget, destinationRoot))
        {
            throw new InvalidOperationException("Origen y destino no pueden ser la misma ruta.");
        }

        if (!await SourceExistsAsync(sourceEndpoint, request.Source, sourceTarget, request.SourcePath))
        {
            throw new InvalidOperationException("La ruta o archivo origen no existe o no es accesible.");
        }

        if (request.DeleteSourceAfterCopy && request.Source.Type != ConnectionType.LocalFolder && request.Source.Type != ConnectionType.Ftp && request.Source.Type != ConnectionType.Sftp)
        {
            throw new InvalidOperationException("La eliminacion de origen no esta soportada para este tipo de conexion.");
        }

        _ = destinationEndpoint;
    }

    private static async Task<IReadOnlyList<StorageItem>> ResolveSourceItemsAsync(
        IStorageEndpoint endpoint,
        ConnectionProfile profile,
        string sourceTarget,
        string sourcePath,
        DateTime? from,
        DateTime? to)
    {
        if (profile.Type == ConnectionType.LocalFolder && Directory.Exists(sourceTarget))
        {
            var items = await endpoint.ListAsync(sourceTarget, true);
            return items.Where(item => PassesDateFilter(item.ModifiedAt, from, to)).ToList();
        }

        if (profile.Type == ConnectionType.Ftp || profile.Type == ConnectionType.Sftp)
        {
            var items = await endpoint.ListAsync(sourceTarget, true);
            return items.Where(item => PassesDateFilter(item.ModifiedAt, from, to)).ToList();
        }

        var item = new StorageItem
        {
            FullPath = sourceTarget,
            RelativePath = Path.GetFileName(sourceTarget.Replace("\\", "/").TrimEnd('/')),
            ModifiedAt = await endpoint.GetLastModifiedAsync(sourceTarget) ?? DateTime.MinValue
        };

        return [item];
    }

    private static async Task<bool> SourceExistsAsync(IStorageEndpoint endpoint, ConnectionProfile profile, string sourceTarget, string sourcePath)
    {
        if (profile.Type == ConnectionType.LocalFolder)
        {
            return Directory.Exists(sourceTarget) || File.Exists(sourceTarget);
        }

        if (profile.Type == ConnectionType.Ftp || profile.Type == ConnectionType.Sftp)
        {
            await endpoint.ListAsync(sourceTarget, true);
            return true;
        }

        return await endpoint.FileExistsAsync(sourceTarget);
    }

    private static string NormalizePath(ConnectionProfile profile, string path)
    {
        return profile.Type == ConnectionType.LocalFolder
            ? Path.Combine(profile.Host, path ?? string.Empty)
            : CombineSegments(profile.BasePath, path);
    }

    private static string CombinePath(ConnectionProfile profile, string root, string relative)
    {
        return profile.Type == ConnectionType.LocalFolder
            ? Path.Combine(profile.Host, root ?? string.Empty, relative ?? string.Empty)
            : CombineSegments(profile.BasePath, root, relative);
    }

    private static string NormalizeDestinationRoot(ConnectionProfile profile, string path)
    {
        return profile.Type == ConnectionType.LocalFolder
            ? Path.Combine(profile.Host, path ?? string.Empty)
            : CombineSegments(profile.BasePath, path);
    }

    private static string CombineSegments(params string[] segments)
    {
        var clean = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.Replace("\\", "/").Trim('/'));

        return string.Join("/", clean);
    }

    private static bool PassesDateFilter(DateTime value, DateTime? from, DateTime? to)
    {
        if (from.HasValue && value < from.Value)
        {
            return false;
        }

        if (to.HasValue && value > to.Value)
        {
            return false;
        }

        return true;
    }

    private static bool IsSameRoute(BackupExecutionRequest request, string sourceTarget, string destinationRoot)
    {
        if (request.Source.Id == Guid.Empty || request.Destination.Id == Guid.Empty)
        {
            return false;
        }

        return request.Source.Id == request.Destination.Id
               && string.Equals(
                   sourceTarget.Replace("\\", "/").TrimEnd('/'),
                   destinationRoot.Replace("\\", "/").TrimEnd('/'),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSuccessMessage(BackupExecutionRequest request, BackupLogEntry log)
    {
        var baseMessage = string.IsNullOrWhiteSpace(request.Notes) ? "Backup completado." : request.Notes;
        var skippedMessage = log.FilesSkipped > 0
            ? $" Se omitieron {log.FilesSkipped} archivo(s) porque el destino ya tenia una version igual o mas reciente."
            : string.Empty;

        var deletedMessage = request.DeleteSourceAfterCopy
            ? $" Se eliminaron {log.SourceFilesDeleted} archivo(s) del origen tras verificar la copia."
            : string.Empty;

        return $"{baseMessage}{skippedMessage}{deletedMessage}";
    }

    private static void Report(
        IProgress<BackupProgressEntry>? progress,
        StringBuilder detailBuilder,
        string fileName,
        string status,
        string? sourcePath = null,
        string? destinationPath = null)
    {
        var entry = new BackupProgressEntry
        {
            Timestamp = DateTime.Now,
            FileName = fileName,
            Status = status
        };

        progress?.Report(entry);
        var sourcePart = string.IsNullOrWhiteSpace(sourcePath) ? "-" : sourcePath;
        var destinationPart = string.IsNullOrWhiteSpace(destinationPath) ? "-" : destinationPath;
        detailBuilder.AppendLine($"{entry.TimestampText} | {entry.Status} | ARCHIVO: {entry.FileName} | ORIGEN: {sourcePart} | DESTINO: {destinationPart}");
    }
}
