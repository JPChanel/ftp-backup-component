using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Endpoints;

public class SftpStorageEndpoint : IStorageEndpoint
{
    private readonly ConnectionProfile _profile;
    private readonly ISftpService _sftpService;

    public SftpStorageEndpoint(ConnectionProfile profile, ISftpService sftpService)
    {
        _profile = profile;
        _sftpService = sftpService;
    }

    public Task<bool> FileExistsAsync(string path)
    {
        return _sftpService.FileExists(_profile.ToCredentials(), path);
    }

    public Task<DateTime?> GetLastModifiedAsync(string path) => _sftpService.GetLastModified(_profile.ToCredentials(), path);

    public Task<byte[]> DownloadBytesAsync(string path) => Task.FromResult(_sftpService.DownloadFileByte(_profile.ToCredentials(), path));

    public Task UploadBytesAsync(string path, byte[] content, bool overwrite)
    {
        return _sftpService.UploadBytes(_profile.ToCredentials(), content, path);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive)
    {
        return _sftpService.ListFiles(_profile.ToCredentials(), path, recursive);
    }

    public Task EnsureDirectoryAsync(string path) => Task.CompletedTask;

    public Task DeleteFileAsync(string path) => _sftpService.DeleteFile(_profile.ToCredentials(), path);
}
