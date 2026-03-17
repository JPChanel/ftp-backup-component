using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Endpoints;

public class FtpStorageEndpoint : IStorageEndpoint
{
    private readonly ConnectionProfile _profile;
    private readonly IFtpService _ftpService;

    public FtpStorageEndpoint(ConnectionProfile profile, IFtpService ftpService)
    {
        _profile = profile;
        _ftpService = ftpService;
    }

    public Task<bool> FileExistsAsync(string path) => _ftpService.FileExists(_profile.ToCredentials(), path);

    public Task<DateTime?> GetLastModifiedAsync(string path) => _ftpService.GetLastModified(_profile.ToCredentials(), path);

    public Task<byte[]> DownloadBytesAsync(string path) => Task.FromResult(_ftpService.DownloadFileByte(_profile.ToCredentials(), path));

    public async Task UploadBytesAsync(string path, byte[] content, bool overwrite)
    {
        if (!overwrite && await FileExistsAsync(path))
        {
            return;
        }

        await _ftpService.UploadBytes(_profile.ToCredentials(), content, path);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive)
    {
        return _ftpService.ListFiles(_profile.ToCredentials(), path, recursive);
    }

    public Task EnsureDirectoryAsync(string path) => Task.CompletedTask;

    public Task DeleteFileAsync(string path) => _ftpService.DeleteFile(_profile.ToCredentials(), path);
}
