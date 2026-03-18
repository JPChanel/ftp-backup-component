using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IFtpService
{
    Task<byte[]> DownloadFileByte(FtpCredentials credentials, string document_path, CancellationToken cancellationToken = default);
    Task<bool> DownloadFilePath(FtpCredentials credentials, string document_path, string path_local, CancellationToken cancellationToken = default);
    Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath, CancellationToken cancellationToken = default);
    Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath, CancellationToken cancellationToken = default);
    Task<bool> FileExists(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive, CancellationToken cancellationToken = default);
    Task DeleteFile(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default);
}
