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

    public Task<bool> FileExistsAsync(string path) => Task.FromResult(File.Exists(path));

    public Task<DateTime?> GetLastModifiedAsync(string path)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(File.GetLastWriteTimeUtc(path));
    }

    public Task<byte[]> DownloadBytesAsync(string path) => File.ReadAllBytesAsync(path);

    public async Task UploadBytesAsync(string path, byte[] content, bool overwrite)
    {
        if (!overwrite && File.Exists(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, content);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, "*", searchOption)
            .Select(file => new StorageItem
            {
                FullPath = file,
                RelativePath = Path.GetRelativePath(path, file).Replace("\\", "/"),
                Size = new FileInfo(file).Length,
                ModifiedAt = File.GetLastWriteTime(file)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StorageItem>>(files);
    }

    public Task EnsureDirectoryAsync(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? _profile.Host;
        Directory.CreateDirectory(directory);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
