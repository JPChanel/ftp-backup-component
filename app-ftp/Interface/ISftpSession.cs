using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface ISftpSession : IAsyncDisposable
{
    Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastModifiedAsync(string remotePath, CancellationToken cancellationToken = default);
    Task<long?> GetFileSizeAsync(string remotePath, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadBytesAsync(string remotePath, CancellationToken cancellationToken = default);
    Task DownloadToLocalFileAsync(string remotePath, string localPath, bool overwrite, CancellationToken cancellationToken = default);
    Task UploadBytesAsync(byte[] content, string remotePath, bool overwrite, CancellationToken cancellationToken = default);
    Task UploadFromLocalFileAsync(string localPath, string remotePath, bool overwrite, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadStreamAsync(string remotePath, CancellationToken cancellationToken = default);
    Task<Stream> OpenWriteStreamAsync(string remotePath, bool overwrite, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageItem>> ListFilesAsync(string remotePath, bool recursive, CancellationToken cancellationToken = default);
    Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default);
    Task<bool> TrySetLastModifiedAsync(string remotePath, DateTime modifiedAt, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default);
}
