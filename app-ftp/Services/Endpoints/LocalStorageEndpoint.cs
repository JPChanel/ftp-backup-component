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

    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) => File.ReadAllBytesAsync(path, cancellationToken);

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

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<StorageItem>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = new List<StorageItem>();

            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                items.Add(new StorageItem
                {
                    FullPath = directory,
                    RelativePath = Path.GetFileName(directory),
                    IsDirectory = true,
                    ModifiedAt = Directory.GetLastWriteTime(directory)
                });
            }

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                items.Add(new StorageItem
                {
                    FullPath = file,
                    RelativePath = Path.GetFileName(file).Replace("\\", "/"),
                    IsDirectory = false,
                    Size = new FileInfo(file).Length,
                    ModifiedAt = File.GetLastWriteTime(file)
                });
            }

            if (recursive)
            {
                foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var nestedItem in EnumerateRecursive(directory, cancellationToken))
                    {
                        items.Add(nestedItem);
                    }
                }
            }

            return items;
        }, cancellationToken);
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

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
