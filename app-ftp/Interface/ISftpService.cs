using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface ISftpService
{
    byte[] DownloadFileByte(FtpCredentials credentials, string document_path);
    bool DownloadFilePath(FtpCredentials credentials, string document_path, string path_local);
    Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath);
    Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath);
    Task<bool> FileExists(FtpCredentials credentials, string remotePath);
    Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath);
    Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive);
    Task DeleteFile(FtpCredentials credentials, string remotePath);
}
