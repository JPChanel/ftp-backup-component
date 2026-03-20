using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Endpoints;

public class SftpStorageEndpoint : IStorageEndpoint
{
    private readonly ConnectionProfile _profile;
    private readonly ISftpService _sftpService;
    private ISftpSession? _session;

    public SftpStorageEndpoint(ConnectionProfile profile, ISftpService sftpService)
    {
        _profile = profile;
        _sftpService = sftpService;
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return GetSessionCallAsync(session => session.FileExistsAsync(path, cancellationToken), cancellationToken);
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return GetSessionCallAsync(session => session.DirectoryExistsAsync(path, cancellationToken), cancellationToken);
    }

    public Task<DateTime?> GetLastModifiedAsync(string path, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.GetLastModifiedAsync(path, cancellationToken), cancellationToken);

    public Task<long?> GetFileSizeAsync(string path, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.GetFileSizeAsync(path, cancellationToken), cancellationToken);

    public Task<byte[]> DownloadBytesAsync(string path, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.DownloadBytesAsync(path, cancellationToken), cancellationToken);

    public Task DownloadToLocalFileAsync(string sourcePath, string localPath, bool overwrite, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.DownloadToLocalFileAsync(sourcePath, localPath, overwrite, cancellationToken), cancellationToken);

    public Task UploadFromLocalFileAsync(string destinationPath, string localPath, bool overwrite, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.UploadFromLocalFileAsync(localPath, destinationPath, overwrite, cancellationToken), cancellationToken);

    public Task<Stream> OpenReadStreamAsync(string path, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.OpenReadStreamAsync(path, cancellationToken), cancellationToken);

    public Task<Stream> OpenWriteStreamAsync(string path, bool overwrite, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.OpenWriteStreamAsync(path, overwrite, cancellationToken), cancellationToken);

    public Task UploadBytesAsync(string path, byte[] content, bool overwrite, CancellationToken cancellationToken = default)
    {
        return GetSessionCallAsync(session => session.UploadBytesAsync(content, path, overwrite, cancellationToken), cancellationToken);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        return GetSessionCallAsync(session => session.ListFilesAsync(path, recursive, cancellationToken), cancellationToken);
    }

    public Task EnsureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.MoveFileAsync(sourcePath, destinationPath, overwrite, cancellationToken), cancellationToken);

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default) =>
        GetSessionCallAsync(session => session.DeleteFileAsync(path, cancellationToken), cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }
    }

    private async Task<T> GetSessionCallAsync<T>(Func<ISftpSession, Task<T>> action, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        return await action(session);
    }

    private async Task GetSessionCallAsync(Func<ISftpSession, Task> action, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        await action(session);
    }

    private async Task<ISftpSession> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        _session = await _sftpService.OpenSessionAsync(_profile.ToCredentials(), cancellationToken);
        return _session;
    }
}
