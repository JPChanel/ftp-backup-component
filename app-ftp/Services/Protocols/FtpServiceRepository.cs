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

    public Task<bool> FileExists(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return Task.FromResult(false);
        }

        return RunWithClientAsync(credentials, cancellationToken, client => client.FileExists(NormalizeRemotePath(remotePath)));
    }

    public Task<bool> DirectoryExists(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithClientAsync(credentials, cancellationToken, client => client.DirectoryExists(NormalizeRemotePath(remotePath)));

    public Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default) =>
        RunWithClientAsync<DateTime?>(credentials, cancellationToken, client =>
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return null;
            }

            var normalizedPath = NormalizeRemotePath(remotePath);

            if (!client.FileExists(normalizedPath))
            {
                return null;
            }

            return client.GetModifiedTime(normalizedPath);
        });

    public Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive, CancellationToken cancellationToken = default) =>
        RunWithClientAsync<IReadOnlyList<StorageItem>>(credentials, cancellationToken, client =>
        {
            var root = NormalizeRemotePath(remotePath);
            var listing = recursive
                ? client.GetListing(root, FtpListOption.Recursive)
                : client.GetListing(root);

            return listing
                .Where(item => item.Type is FtpObjectType.File or FtpObjectType.Directory)
                .Select(item => new StorageItem
                {
                    FullPath = item.FullName,
                    RelativePath = GetRelativeRemotePath(root, item.FullName),
                    IsDirectory = item.Type == FtpObjectType.Directory,
                    Size = item.Size,
                    ModifiedAt = item.Modified
                })
                .ToList();
        });

    public async Task DeleteFile(FtpCredentials credentials, string remotePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return;
        }

        await RunWithClientAsync(credentials, cancellationToken, client =>
        {
            var normalizedPath = NormalizeRemotePath(remotePath);

            if (client.FileExists(normalizedPath))
            {
                client.DeleteFile(normalizedPath);
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
        var attempts = Math.Max(0, credentials.RetryCount) + 1;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, credentials.TimeoutSeconds)));
            var effectiveToken = timeoutCts.Token;

            var operationTask = Task.Run(() =>
            {
                using var client = CreateClient(credentials);
                using var registration = effectiveToken.Register(() =>
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

                effectiveToken.ThrowIfCancellationRequested();
                client.AutoConnect();
                effectiveToken.ThrowIfCancellationRequested();
                return action(client);
            }, CancellationToken.None);

            try
            {
                return await operationTask.WaitAsync(effectiveToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObserveBackgroundFault(operationTask);
                throw;
            }
            catch (OperationCanceledException)
            {
                ObserveBackgroundFault(operationTask);
                lastException = new TimeoutException($"La operacion FTP supero el timeout de {credentials.TimeoutSeconds} segundo(s).");
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            if (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(lastException?.Message ?? "La operacion FTP fallo sin detalles.", lastException);
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
