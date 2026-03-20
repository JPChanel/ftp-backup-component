using app_ftp.Config;
using app_ftp.Interface;
using app_ftp.Services.Models;
using System.Text;

namespace app_ftp.Services;

public class BackupOrchestrator
{
    private const int StreamCopyBufferSize = 1024 * 128;
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
            Operation = "Backup",
            SourceName = request.Source.Name,
            DestinationName = request.Destination.Name,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        try
        {
            await using var sourceEndpoint = _endpointFactory.Create(request.Source);
            await using var destinationEndpoint = _endpointFactory.Create(request.Destination);
            var sourceTarget = NormalizePath(request.Source, request.SourcePath);
            var destinationRoot = NormalizeDestinationRoot(request.Destination, request.DestinationPath);
            await ValidateRequestAsync(request, sourceEndpoint, destinationEndpoint, sourceTarget, destinationRoot, progress, detailBuilder, cancellationToken);

            Report(progress, detailBuilder, "SISTEMA", "INICIANDO PROCESO DE COPIA...");
            await ProcessSourceEntriesDfsAsync(request, sourceEndpoint, destinationEndpoint, sourceTarget, progress, detailBuilder, log, cancellationToken);

            log.Status = log.DirectoriesSkipped > 0 ? "PARTIAL" : "SUCCESS";
            log.Message = BuildSuccessMessage(request, log);
            Report(progress, detailBuilder, "SISTEMA", log.DirectoriesSkipped > 0 ? "BACKUP FINALIZADO CON OMITIDOS" : "BACKUP FINALIZADO");
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
        IProgress<BackupProgressEntry>? progress,
        StringBuilder detailBuilder,
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

        Report(progress, detailBuilder, "ORIGEN", "VALIDANDO ACCESO REAL", sourceTarget);

        SourceState sourceState;
        try
        {
            sourceState = await GetSourceStateAsync(sourceEndpoint, request.Source, sourceTarget, request.SourcePath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error validando origen '{FormatSourcePathLabel(request.SourcePath)}': {ex.Message}", ex);
        }

        if (!sourceState.Exists)
        {
            throw new InvalidOperationException($"La ruta origen '{FormatSourcePathLabel(request.SourcePath)}' no existe o no es accesible.");
        }

        if (sourceState.IsEmptyDirectory)
        {
            throw new InvalidOperationException($"La carpeta origen '{FormatSourcePathLabel(request.SourcePath)}' existe pero esta vacia.");
        }

        Report(progress, detailBuilder, "ORIGEN", "ACCESO OK", sourceTarget);

        if (request.DeleteSourceAfterCopy && request.Source.Type != ConnectionType.LocalFolder && request.Source.Type != ConnectionType.Ftp && request.Source.Type != ConnectionType.Sftp)
        {
            throw new InvalidOperationException("La eliminacion de origen no esta soportada para este tipo de conexion.");
        }

        await ValidateDestinationAccessAsync(request, destinationEndpoint, destinationRoot, progress, detailBuilder, cancellationToken);
    }

    private static async Task ValidateDestinationAccessAsync(
        BackupExecutionRequest request,
        IStorageEndpoint destinationEndpoint,
        string destinationRoot,
        IProgress<BackupProgressEntry>? progress,
        StringBuilder detailBuilder,
        CancellationToken cancellationToken)
    {
        var destinationBasePath = NormalizePath(request.Destination, string.Empty);
        Report(progress, detailBuilder, "DESTINO", "VALIDANDO ACCESO REAL", destinationBasePath, destinationRoot);

        try
        {
            if (request.Destination.Type == ConnectionType.LocalFolder)
            {
                Directory.CreateDirectory(destinationBasePath);
            }
            else if (request.Destination.Type is ConnectionType.Ftp or ConnectionType.Sftp)
            {
                var probePath = string.IsNullOrWhiteSpace(request.Destination.BasePath) ? "/" : destinationBasePath;

                if (!await destinationEndpoint.DirectoryExistsAsync(probePath, cancellationToken))
                {
                    throw new InvalidOperationException($"La ruta base destino '{request.Destination.PathLabel}' no existe o no es accesible.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error validando destino '{request.Destination.PathLabel}': {ex.Message}", ex);
        }

        Report(progress, detailBuilder, "DESTINO", "ACCESO OK", destinationBasePath, destinationRoot);
    }

    private static async Task ProcessSourceEntriesDfsAsync(
        BackupExecutionRequest request,
        IStorageEndpoint sourceEndpoint,
        IStorageEndpoint destinationEndpoint,
        string sourceTarget,
        IProgress<BackupProgressEntry>? progress,
        StringBuilder detailBuilder,
        BackupLogEntry log,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await sourceEndpoint.FileExistsAsync(sourceTarget, cancellationToken))
        {
            var singleItem = new StorageItem
            {
                FullPath = sourceTarget,
                RelativePath = Path.GetFileName(sourceTarget.Replace("\\", "/").TrimEnd('/')),
                IsDirectory = false,
                Size = await sourceEndpoint.GetFileSizeAsync(sourceTarget, cancellationToken) ?? 0,
                ModifiedAt = await sourceEndpoint.GetLastModifiedAsync(sourceTarget, cancellationToken) ?? DateTime.MinValue
            };

            if (PassesDateFilter(singleItem.ModifiedAt, request.FilterFromDate, request.FilterToDate))
            {
                await ProcessFileAsync(request, sourceEndpoint, destinationEndpoint, singleItem, progress, detailBuilder, log, cancellationToken);
            }

            return;
        }

        var stack = new Stack<string>();
        stack.Push(sourceTarget);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = stack.Pop();
            if (request.Source.Type is ConnectionType.Ftp or ConnectionType.Sftp)
            {
                Report(progress, detailBuilder, currentDirectory, "EXPLORANDO DIRECTORIO", currentDirectory);
            }

            IReadOnlyList<StorageItem> entries;
            try
            {
                entries = await sourceEndpoint.ListAsync(currentDirectory, false, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Report(progress, detailBuilder, currentDirectory, $"ERROR EXPLORANDO DIRECTORIO: {ex.Message}", currentDirectory);
                log.DirectoriesSkipped++;
                continue;
            }

            foreach (var directory in entries
                         .Where(item => item.IsDirectory)
                         .OrderByDescending(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                stack.Push(directory.FullPath);
            }

            foreach (var file in entries
                         .Where(item => !item.IsDirectory)
                         .OrderBy(item => item.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = BuildRelativePath(request.Source, sourceTarget, file.FullPath);
                var fileToProcess = new StorageItem
                {
                    FullPath = file.FullPath,
                    RelativePath = relativePath,
                    IsDirectory = false,
                    Size = file.Size,
                    ModifiedAt = file.ModifiedAt
                };

                if (!PassesDateFilter(fileToProcess.ModifiedAt, request.FilterFromDate, request.FilterToDate))
                {
                    continue;
                }

                await ProcessFileAsync(request, sourceEndpoint, destinationEndpoint, fileToProcess, progress, detailBuilder, log, cancellationToken);
            }
        }
    }

    private static async Task ProcessFileAsync(
        BackupExecutionRequest request,
        IStorageEndpoint sourceEndpoint,
        IStorageEndpoint destinationEndpoint,
        StorageItem item,
        IProgress<BackupProgressEntry>? progress,
        StringBuilder detailBuilder,
        BackupLogEntry log,
        CancellationToken cancellationToken)
    {
        var destinationFullPath = CombinePath(request.Destination, request.DestinationPath, item.RelativePath);
        string? transferTargetPath = null;

        try
        {
            var destinationExistedBefore = await destinationEndpoint.FileExistsAsync(destinationFullPath, cancellationToken);
            if (destinationExistedBefore)
            {
                if (!request.OverwriteExisting)
                {
                    log.FilesSkipped++;
                    Report(progress, detailBuilder, item.RelativePath, "OMITIDO", item.FullPath, destinationFullPath);
                    return;
                }

                var destinationModifiedAt = await destinationEndpoint.GetLastModifiedAsync(destinationFullPath, cancellationToken);
                if (destinationModifiedAt.HasValue && item.ModifiedAt <= destinationModifiedAt.Value)
                {
                    log.FilesSkipped++;
                    Report(progress, detailBuilder, item.RelativePath, "OMITIDO", item.FullPath, destinationFullPath);
                    return;
                }
            }

            transferTargetPath = request.DeleteSourceAfterCopy
                ? BuildTemporaryDestinationPath(destinationFullPath)
                : destinationFullPath;

            var bytesTransferred = await TransferFileAsync(
                request,
                sourceEndpoint,
                destinationEndpoint,
                item,
                transferTargetPath,
                cancellationToken);

            if (request.DeleteSourceAfterCopy)
            {
                await VerifyCopiedFileAsync(sourceEndpoint, destinationEndpoint, item, transferTargetPath, cancellationToken);
                await destinationEndpoint.MoveFileAsync(transferTargetPath, destinationFullPath, request.OverwriteExisting, cancellationToken);
                await VerifyCopiedFileAsync(sourceEndpoint, destinationEndpoint, item, destinationFullPath, cancellationToken);
            }

            Report(progress, detailBuilder, item.RelativePath, "COPIADO", item.FullPath, destinationFullPath);

            if (request.DeleteSourceAfterCopy)
            {
                if (destinationExistedBefore && !request.OverwriteExisting)
                {
                    throw new InvalidOperationException("No es seguro eliminar el origen porque el archivo ya existia en destino y la sobrescritura esta desactivada.");
                }

                await sourceEndpoint.DeleteFileAsync(item.FullPath, cancellationToken);
                log.SourceFilesDeleted++;
                Report(progress, detailBuilder, item.RelativePath, "ORIGEN ELIMINADO", item.FullPath, destinationFullPath);
            }

            log.FilesTransferred++;
            log.BytesTransferred += bytesTransferred;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (request.DeleteSourceAfterCopy
                && transferTargetPath is not null
                && !string.Equals(transferTargetPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
            {
                await TryDeleteFileAsync(destinationEndpoint, transferTargetPath, cancellationToken);
            }

            Report(progress, detailBuilder, item.RelativePath, "ERROR", item.FullPath, destinationFullPath);
            throw new InvalidOperationException($"Error procesando '{item.RelativePath}': {ex.Message}", ex);
        }
    }

    private static async Task<SourceState> GetSourceStateAsync(IStorageEndpoint endpoint, ConnectionProfile profile, string sourceTarget, string sourcePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (profile.Type == ConnectionType.LocalFolder)
        {
            if (File.Exists(sourceTarget))
            {
                return SourceState.FileFound();
            }

            if (Directory.Exists(sourceTarget))
            {
                var hasEntries = Directory.EnumerateFileSystemEntries(sourceTarget).Any();
                return hasEntries ? SourceState.DirectoryFound() : SourceState.EmptyDirectory();
            }

            return SourceState.NotFound();
        }

        if (profile.Type == ConnectionType.Ftp || profile.Type == ConnectionType.Sftp)
        {
            var normalizedSourcePath = sourcePath?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                var rootItems = await endpoint.ListAsync(sourceTarget, false, cancellationToken);
                return rootItems.Count > 0 ? SourceState.DirectoryFound() : SourceState.EmptyDirectory();
            }

            if (await endpoint.FileExistsAsync(sourceTarget, cancellationToken))
            {
                return SourceState.FileFound();
            }

            if (!await endpoint.DirectoryExistsAsync(sourceTarget, cancellationToken))
            {
                return SourceState.NotFound();
            }

            var items = await endpoint.ListAsync(sourceTarget, false, cancellationToken);
            return items.Count > 0 ? SourceState.DirectoryFound() : SourceState.EmptyDirectory();
        }

        return await endpoint.FileExistsAsync(sourceTarget, cancellationToken)
            ? SourceState.FileFound()
            : SourceState.NotFound();
    }

    private static string FormatSourcePathLabel(string sourcePath)
    {
        return string.IsNullOrWhiteSpace(sourcePath) ? "/" : $"/{sourcePath.Trim().Trim('/')}";
    }

    private readonly record struct SourceState(bool Exists, bool IsEmptyDirectory)
    {
        public static SourceState NotFound() => new(false, false);
        public static SourceState FileFound() => new(true, false);
        public static SourceState DirectoryFound() => new(true, false);
        public static SourceState EmptyDirectory() => new(true, true);
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

    private static async Task<long> TransferFileAsync(
        BackupExecutionRequest request,
        IStorageEndpoint sourceEndpoint,
        IStorageEndpoint destinationEndpoint,
        StorageItem item,
        string destinationFullPath,
        CancellationToken cancellationToken)
    {
        if (request.Source.Type == ConnectionType.LocalFolder && request.Destination.Type == ConnectionType.LocalFolder)
        {
            await sourceEndpoint.DownloadToLocalFileAsync(item.FullPath, destinationFullPath, request.OverwriteExisting, cancellationToken);
            return item.Size > 0 ? item.Size : new FileInfo(destinationFullPath).Length;
        }

        if (request.Destination.Type == ConnectionType.LocalFolder)
        {
            await sourceEndpoint.DownloadToLocalFileAsync(item.FullPath, destinationFullPath, request.OverwriteExisting, cancellationToken);
            return item.Size > 0 ? item.Size : new FileInfo(destinationFullPath).Length;
        }

        if (request.Source.Type == ConnectionType.LocalFolder)
        {
            await destinationEndpoint.UploadFromLocalFileAsync(destinationFullPath, item.FullPath, request.OverwriteExisting, cancellationToken);
            return item.Size > 0 ? item.Size : new FileInfo(item.FullPath).Length;
        }

        await destinationEndpoint.EnsureDirectoryAsync(destinationFullPath, cancellationToken);
        await using var sourceStream = await sourceEndpoint.OpenReadStreamAsync(item.FullPath, cancellationToken);
        await using var destinationStream = await destinationEndpoint.OpenWriteStreamAsync(destinationFullPath, request.OverwriteExisting, cancellationToken);
        await sourceStream.CopyToAsync(destinationStream, StreamCopyBufferSize, cancellationToken);
        await destinationStream.FlushAsync(cancellationToken);

        return item.Size;
    }

    private static async Task VerifyCopiedFileAsync(
        IStorageEndpoint sourceEndpoint,
        IStorageEndpoint destinationEndpoint,
        StorageItem item,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var expectedSize = item.Size;
        if (expectedSize == 0)
        {
            expectedSize = await sourceEndpoint.GetFileSizeAsync(item.FullPath, cancellationToken) ?? 0;
        }

        var destinationSize = await destinationEndpoint.GetFileSizeAsync(destinationPath, cancellationToken);
        if (!destinationSize.HasValue)
        {
            throw new InvalidOperationException($"No se pudo verificar el archivo copiado en destino: {destinationPath}");
        }

        if (destinationSize.Value != expectedSize)
        {
            throw new InvalidOperationException(
                $"La copia verificada no coincide en tamano. Esperado: {expectedSize} bytes. Destino: {destinationSize.Value} bytes.");
        }
    }

    private static string BuildTemporaryDestinationPath(string destinationFullPath)
    {
        return $"{destinationFullPath}.baas-partial-{Guid.NewGuid():N}";
    }

    private static async Task TryDeleteFileAsync(IStorageEndpoint endpoint, string path, CancellationToken cancellationToken)
    {
        try
        {
            if (await endpoint.FileExistsAsync(path, cancellationToken))
            {
                await endpoint.DeleteFileAsync(path, cancellationToken);
            }
        }
        catch
        {
        }
    }

    private static string BuildSuccessMessage(BackupExecutionRequest request, BackupLogEntry log)
    {
        var baseMessage = string.IsNullOrWhiteSpace(request.Notes) ? "Backup completado." : request.Notes;
        var skippedMessage = log.FilesSkipped > 0
            ? $" Se omitieron {log.FilesSkipped} archivo(s) porque el destino ya tenia una version igual o mas reciente."
            : string.Empty;

        var skippedDirectoriesMessage = log.DirectoriesSkipped > 0
            ? $" Se omitieron {log.DirectoriesSkipped} carpeta(s) por error durante la exploracion."
            : string.Empty;

        var deletedMessage = request.DeleteSourceAfterCopy
            ? $" Se eliminaron {log.SourceFilesDeleted} archivo(s) del origen tras verificar la copia."
            : string.Empty;

        return $"{baseMessage}{skippedMessage}{skippedDirectoriesMessage}{deletedMessage}";
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

    private static string BuildRelativePath(ConnectionProfile profile, string rootPath, string fullPath)
    {
        if (profile.Type == ConnectionType.LocalFolder)
        {
            return Path.GetRelativePath(rootPath, fullPath).Replace("\\", "/");
        }

        var normalizedRoot = NormalizeRemotePath(rootPath).TrimEnd('/');
        var normalizedFullPath = NormalizeRemotePath(fullPath);

        if (!normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullPath.TrimStart('/');
        }

        return normalizedFullPath[normalizedRoot.Length..].TrimStart('/');
    }

    private static string NormalizeRemotePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var normalized = value.Replace("\\", "/");
        return normalized.StartsWith('/') ? normalized : $"/{normalized}";
    }
}
