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
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(path, "*", searchOption)
                .Select(file =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new StorageItem
                    {
                        FullPath = file,
                        RelativePath = Path.GetRelativePath(path, file).Replace("\\", "/"),
                        Size = new FileInfo(file).Length,
                        ModifiedAt = File.GetLastWriteTime(file)
                    };
                })
                .ToList();

            return files;
        }, cancellationToken);
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
