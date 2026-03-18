using app_ftp.Interface;
using app_ftp.Services.Models;
using FluentFTP;
using System.Net;

namespace app_ftp.Services.Protocols;

public class FtpServiceRepository : IFtpService
{
    public Task<byte[]> DownloadFileByte(FtpCredentials credentials, string document_path, CancellationToken cancellationToken = default) =>
        RunWithClientAsync(credentials, cancellationToken, client =>
        {
            client.DownloadBytes(out var bytes, document_path);
            return bytes;
        });

    public Task<bool> DownloadFilePath(FtpCredentials credentials, string document_path, string path_local, CancellationToken cancellationToken = default) =>
        RunWithClientAsync(credentials, cancellationToken, client => client.DownloadFile(path_local, document_path) == FtpStatus.Success);

    public async Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithClientAsync(credentials, cancellationToken, client =>
        {
            var remoteDirectory = GetRemoteDirectory(remotePath);
            if (!string.IsNullOrWhiteSpace(remoteDirectory))
            {
                client.CreateDirectory(remoteDirectory);
            }

            client.UploadBytes(document, remotePath, FtpRemoteExists.Overwrite, true);
            return true;
        });
    }

    public async Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithClientAsync(credentials, cancellationToken, client =>
        {
            var remoteDirectory = GetRemoteDirectory(remotePath);
            if (!string.IsNullOrWhiteSpace(remoteDirectory))
            {
                client.CreateDirectory(remoteDirectory);
            }

            client.UploadFile(filePath, remotePath, FtpRemoteExists.Overwrite, true);
            return true;
        });
    }

    public Task<bool> FileExists(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithClientAsync(credentials, cancellationToken, client => client.FileExists(remotePath));

    public Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithClientAsync<DateTime?>(credentials, cancellationToken, client =>
        {
            if (!client.FileExists(remotePath))
            {
                return null;
            }

            return client.GetModifiedTime(remotePath);
        });

    public Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive, CancellationToken cancellationToken = default) =>
        RunWithClientAsync<IReadOnlyList<StorageItem>>(credentials, cancellationToken, client =>
        {
            var root = NormalizeRemotePath(remotePath);
            var listing = recursive
                ? client.GetListing(root, FtpListOption.Recursive)
                : client.GetListing(root);

            return listing
                .Where(item => item.Type == FtpObjectType.File)
                .Select(item => new StorageItem
                {
                    FullPath = item.FullName,
                    RelativePath = GetRelativeRemotePath(root, item.FullName),
                    Size = item.Size,
                    ModifiedAt = item.Modified
                })
                .ToList();
        });

    public async Task DeleteFile(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default)
    {
        await RunWithClientAsync(credentials, cancellationToken, client =>
        {
            if (client.FileExists(remotePath))
            {
                client.DeleteFile(remotePath);
            }

            return true;
        });
    }

    private static FluentFTP.FtpClient CreateClient(FtpCredentials credentials)
    {
        var host = NormalizeHost(credentials.Host);
        var client = new FluentFTP.FtpClient(host);
        client.Port = credentials.Port;
        client.Credentials = new NetworkCredential(credentials.Username, credentials.Password);
        return client;
    }

    private static async Task<T> RunWithClientAsync<T>(FtpCredentials credentials, CancellationToken cancellationToken, Func<FluentFTP.FtpClient, T> action)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operationTask = Task.Run(() =>
        {
            using var client = CreateClient(credentials);
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    client.Disconnect();
                }
                catch
                {
                }

                try
                {
                    client.Dispose();
                }
                catch
                {
                }
            });

            cancellationToken.ThrowIfCancellationRequested();
            client.AutoConnect();
            cancellationToken.ThrowIfCancellationRequested();
            return action(client);
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

    private static string NormalizeHost(string host)
    {
        if (host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            return host["ftp://".Length..];
        }

        return host;
    }

    private static string GetRemoteDirectory(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return string.Empty;
        }

        var normalized = remotePath.Replace("\\", "/");
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash <= 0 ? string.Empty : normalized[..lastSlash];
    }

    private static string NormalizeRemotePath(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return "/";
        }

        var normalized = remotePath.Replace("\\", "/");
        return normalized.StartsWith('/') ? normalized : $"/{normalized}";
    }

    private static string GetRelativeRemotePath(string root, string fullName)
    {
        var normalizedRoot = NormalizeRemotePath(root).TrimEnd('/');
        var normalizedFullName = NormalizeRemotePath(fullName);

        if (!normalizedFullName.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullName.TrimStart('/');
        }

        return normalizedFullName[normalizedRoot.Length..].TrimStart('/');
    }
}
