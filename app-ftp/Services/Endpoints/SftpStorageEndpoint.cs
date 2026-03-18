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

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return _sftpService.FileExists(_profile.ToCredentials(), path, cancellationToken);
    }

    public Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default) => _sftpService.GetLastModified(_profile.ToCredentials(), path, cancellationToken);

    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) => _sftpService.DownloadFileByte(_profile.ToCredentials(), path, cancellationToken);

    public Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default)
    {
        return _sftpService.UploadBytes(_profile.ToCredentials(), content, path, cancellationToken);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        return _sftpService.ListFiles(_profile.ToCredentials(), path, recursive, cancellationToken);
    }

    public Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default) => _sftpService.DeleteFile(_profile.ToCredentials(), path, cancellationToken);
}
