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

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default) => _ftpService.FileExists(_profile.ToCredentials(), path, cancellationToken);

    public Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default) => _ftpService.GetLastModified(_profile.ToCredentials(), path, cancellationToken);

    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) => _ftpService.DownloadFileByte(_profile.ToCredentials(), path, cancellationToken);

    public async Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default)
    {
        if (!overwrite && await FileExistsAsync(path, cancellationToken))
        {
            return;
        }

        await _ftpService.UploadBytes(_profile.ToCredentials(), content, path, cancellationToken);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        return _ftpService.ListFiles(_profile.ToCredentials(), path, recursive, cancellationToken);
    }

    public Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default) => _ftpService.DeleteFile(_profile.ToCredentials(), path, cancellationToken);
}
