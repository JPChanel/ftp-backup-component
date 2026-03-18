using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IStorageEndpoint
{
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default);
    Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default);
    Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
}
