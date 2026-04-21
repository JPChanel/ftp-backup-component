using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IStorageEndpoint : IAsyncDisposable
{
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default);
    Task<long?> GetFileSizeAsync(string path, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default);
    Task DownloadToLocalFileAsync(string sourcePath, string localPath, bool overwrite, CancellationToken cancellationToken = default);
    Task UploadFromLocalFileAsync(string destinationPath, string localPath, bool overwrite, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadStreamAsync(string path, CancellationToken cancellationToken = default);
    Task<Stream> OpenWriteStreamAsync(string path, bool overwrite, CancellationToken cancellationToken = default);
    Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default);
    Task<bool> HasEntriesAsync(string path, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StorageItem> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default);
    Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default);
    Task<bool> TrySetLastModifiedAsync(string path, DateTime modifiedAt, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
}
