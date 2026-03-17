using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IStorageEndpoint
{
    Task<bool> FileExistsAsync(string path);
    Task<DateTime?> GetLastModifiedAsync(string path);
    Task<byte[]> DownloadBytesAsync(string path);
    Task UploadBytesAsync(string path, byte[] content, bool overwrite);
    Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive);
    Task EnsureDirectoryAsync(string path);
    Task DeleteFileAsync(string path);
}
