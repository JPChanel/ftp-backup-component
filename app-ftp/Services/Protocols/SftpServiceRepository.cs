using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Protocols;

public class SftpServiceRepository : ISftpService
{
    public Task<byte[]> DownloadFileByte(FtpCredentials credentials, string document_path, CancellationToken cancellationToken = default) =>
        RunWithProtocolAsync(credentials, cancellationToken, protocol => protocol.DownloadFileBytes(document_path));

    public Task<bool> DownloadFilePath(FtpCredentials credentials, string document_path, string path_local, CancellationToken cancellationToken = default) =>
        RunWithProtocolAsync(credentials, cancellationToken, protocol => protocol.DownloadFile(document_path, path_local));

    public async Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithProtocolAsync(credentials, cancellationToken, protocol =>
        {
            protocol.CreateDirectory(remotePath);
            protocol.UploadByte(document, remotePath, true);
            return true;
        });
    }

    public async Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithProtocolAsync(credentials, cancellationToken, protocol =>
        {
            protocol.CreateDirectory(remotePath);
            protocol.UploadFile(filePath, remotePath, true);
            return true;
        });
    }

    public Task<bool> FileExists(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithProtocolAsync(credentials, cancellationToken, protocol => protocol.FileExists(remotePath));

    public Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithProtocolAsync<DateTime?>(credentials, cancellationToken, protocol => protocol.GetLastWriteTime(remotePath));

    public Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive, CancellationToken cancellationToken = default) =>
        RunWithProtocolAsync<IReadOnlyList<StorageItem>>(credentials, cancellationToken, protocol => protocol.ListFiles(remotePath, recursive));

    public async Task DeleteFile(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithProtocolAsync(credentials, cancellationToken, protocol =>
        {
            protocol.DeleteFile(remotePath);
            return true;
        });
    }

    private static async Task<T> RunWithProtocolAsync<T>(FtpCredentials credentials, CancellationToken cancellationToken, Func<SftpProtocol, T> action)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operationTask = Task.Run(() =>
        {
            var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
            using var registration = cancellationToken.Register(() => sftpProtocol.Close());

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                sftpProtocol.Connect(120);
                cancellationToken.ThrowIfCancellationRequested();
                return action(sftpProtocol);
            }
            finally
            {
                sftpProtocol.Close();
            }
        }, CancellationToken.None);

        try
        {
            return await operationTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObserveBackgroundFault(operationTask);
            throw;
        }
    }

    private static void ObserveBackgroundFault(Task task)
    {
        _ = task.ContinueWith(
            completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
