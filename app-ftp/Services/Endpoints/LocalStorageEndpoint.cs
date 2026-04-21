using app_ftp.Interface;
using app_ftp.Services.Models;
using System.IO;

namespace app_ftp.Services.Endpoints;

public class LocalStorageEndpoint : IStorageEndpoint
{
    private readonly ConnectionProfile _profile;

    public LocalStorageEndpoint(ConnectionProfile profile)
    {
        _profile = profile;
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(path));
    }

    public Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(File.GetLastWriteTimeUtc(path));
    }

    public Task<long?> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            return Task.FromResult<long?>(null);
        }

        return Task.FromResult<long?>(new FileInfo(path).Length);
    }

    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) => File.ReadAllBytesAsync(path, cancellationToken);

    public Task DownloadToLocalFileAsync(string sourcePath, string localPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!overwrite && File.Exists(localPath))
        {
            return Task.CompletedTask;
        }

        EnsureLocalDirectory(localPath);
        File.Copy(sourcePath, localPath, overwrite);
        return Task.CompletedTask;
    }

    public Task UploadFromLocalFileAsync(string destinationPath, string localPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!overwrite && File.Exists(destinationPath))
        {
            return Task.CompletedTask;
        }

        EnsureLocalDirectory(destinationPath);
        File.Copy(localPath, destinationPath, overwrite);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<Stream> OpenWriteStreamAsync(string path, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLocalDirectory(path);
        if (!overwrite && File.Exists(path))
        {
            throw new IOException($"El archivo ya existe en destino: {path}");
        }

        Stream stream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public async Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!overwrite && File.Exists(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public Task<bool> HasEntriesAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => Directory.EnumerateFileSystemEntries(path).Any(), cancellationToken);
    }

    public async IAsyncEnumerable<StorageItem> ListAsync(string path, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var directory in Directory.EnumerateDirectories(path, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StorageItem
            {
                FullPath = directory,
                RelativePath = Path.GetFileName(directory), // o calcular ruta relativa si hace falta
                IsDirectory = true,
                ModifiedAt = Directory.GetLastWriteTime(directory)
            };
        }

        foreach (var file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StorageItem
            {
                FullPath = file,
                RelativePath = Path.GetFileName(file).Replace("\\", "/"),
                IsDirectory = false,
                Size = new FileInfo(file).Length,
                ModifiedAt = File.GetLastWriteTime(file)
            };
        }
    }

    private static IEnumerable<StorageItem> EnumerateRecursive(string directory, CancellationToken cancellationToken)
    {
        foreach (var subdirectory in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StorageItem
            {
                FullPath = subdirectory,
                RelativePath = Path.GetFileName(subdirectory),
                IsDirectory = true,
                ModifiedAt = Directory.GetLastWriteTime(subdirectory)
            };

            foreach (var nested in EnumerateRecursive(subdirectory, cancellationToken))
            {
                yield return nested;
            }
        }

        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StorageItem
            {
                FullPath = file,
                RelativePath = Path.GetFileName(file),
                IsDirectory = false,
                Size = new FileInfo(file).Length,
                ModifiedAt = File.GetLastWriteTime(file)
            };
        }
    }

    public Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.GetDirectoryName(path) ?? _profile.Host;
        Directory.CreateDirectory(directory);
        return Task.CompletedTask;
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLocalDirectory(destinationPath);
        File.Move(sourcePath, destinationPath, overwrite);
        return Task.CompletedTask;
    }

    public Task<bool> TrySetLastModifiedAsync(string path, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.SetLastWriteTimeUtc(path, NormalizeUtc(modifiedAt));
        return Task.FromResult(true);
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static void EnsureLocalDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
