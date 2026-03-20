using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Endpoints;

public class FtpStorageEndpoint : IStorageEndpoint
{
    private readonly ConnectionProfile _profile;
    private readonly IFtpService _ftpService;
    private IFtpSession? _session;

    public FtpStorageEndpoint(ConnectionProfile profile, IFtpService ftpService)
    {
        _profile = profile;
        _ftpService = ftpService;
    }

    public async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default) =>
        await (await GetSessionAsync(cancellationToken)).FileExistsAsync(path, cancellationToken);

    public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default) =>
        await (await GetSessionAsync(cancellationToken)).DirectoryExistsAsync(path, cancellationToken);

    public async Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default) =>
        await (await GetSessionAsync(cancellationToken)).GetLastModifiedAsync(path, cancellationToken);

    public async Task<long?> GetFileSizeAsync(string path, CancellationToken cancellationToken = default) =>
        await (await GetSessionAsync(cancellationToken)).GetFileSizeAsync(path, cancellationToken);

    public async Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) =>
        await (await GetSessionAsync(cancellationToken)).DownloadBytesAsync(path, cancellationToken);

    public async Task DownloadToLocalFileAsync(string sourcePath, string localPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        await (await GetSessionAsync(cancellationToken)).DownloadToLocalFileAsync(sourcePath, localPath, overwrite, cancellationToken);
    }

    public async Task UploadFromLocalFileAsync(string destinationPath, string localPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        await (await GetSessionAsync(cancellationToken)).UploadFromLocalFileAsync(localPath, destinationPath, overwrite, cancellationToken);
    }

    public async Task<Stream> OpenReadStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        return await (await GetSessionAsync(cancellationToken)).OpenReadStreamAsync(path, cancellationToken);
    }

    public async Task<Stream> OpenWriteStreamAsync(string path, bool overwrite, CancellationToken cancellationToken = default)
    {
        return await (await GetSessionAsync(cancellationToken)).OpenWriteStreamAsync(path, overwrite, cancellationToken);
    }

    public async Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default)
    {
        await (await GetSessionAsync(cancellationToken)).UploadBytesAsync(content, path, overwrite, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        return await (await GetSessionAsync(cancellationToken)).ListFilesAsync(path, recursive, cancellationToken);
    }

    public Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default)
    {
        await (await GetSessionAsync(cancellationToken)).MoveFileAsync(sourcePath, destinationPath, overwrite, cancellationToken);
    }

    public async Task<bool> TrySetLastModifiedAsync(string path, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        return await (await GetSessionAsync(cancellationToken)).TrySetLastModifiedAsync(path, modifiedAt, cancellationToken);
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await (await GetSessionAsync(cancellationToken)).DeleteFileAsync(path, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }
    }

    private async Task<IFtpSession> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        _session = await _ftpService.OpenSessionAsync(_profile.ToCredentials(), cancellationToken);
        return _session;
    }
}
