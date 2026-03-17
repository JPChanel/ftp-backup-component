using app_ftp.Interface;
using app_ftp.Services.Models;
using FluentFTP;
using System.Net;

namespace app_ftp.Services.Protocols;

public class FtpServiceRepository : IFtpService
{
    public byte[] DownloadFileByte(FtpCredentials credentials, string document_path)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();
        client.DownloadBytes(out var bytes, document_path);
        return bytes;
    }

    public bool DownloadFilePath(FtpCredentials credentials, string document_path, string path_local)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();
        return client.DownloadFile(path_local, document_path) == FtpStatus.Success;
    }

    public Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();

        var remoteDirectory = GetRemoteDirectory(remotePath);
        if (!string.IsNullOrWhiteSpace(remoteDirectory))
        {
            client.CreateDirectory(remoteDirectory);
        }

        client.UploadBytes(document, remotePath, FtpRemoteExists.Overwrite, true);
        return Task.CompletedTask;
    }

    public Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();

        var remoteDirectory = GetRemoteDirectory(remotePath);
        if (!string.IsNullOrWhiteSpace(remoteDirectory))
        {
            client.CreateDirectory(remoteDirectory);
        }

        client.UploadFile(filePath, remotePath, FtpRemoteExists.Overwrite, true);
        return Task.CompletedTask;
    }

    public Task<bool> FileExists(FtpCredentials credentials, string remotePath)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();
        return Task.FromResult(client.FileExists(remotePath));
    }

    public Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();

        if (!client.FileExists(remotePath))
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(client.GetModifiedTime(remotePath));
    }

    public Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();

        var root = NormalizeRemotePath(remotePath);
        var listing = recursive
            ? client.GetListing(root, FtpListOption.Recursive)
            : client.GetListing(root);

        var items = listing
            .Where(item => item.Type == FtpObjectType.File)
            .Select(item => new StorageItem
            {
                FullPath = item.FullName,
                RelativePath = GetRelativeRemotePath(root, item.FullName),
                Size = item.Size,
                ModifiedAt = item.Modified
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StorageItem>>(items);
    }

    public Task DeleteFile(FtpCredentials credentials, string remotePath)
    {
        using var client = CreateClient(credentials);
        client.AutoConnect();

        if (client.FileExists(remotePath))
        {
            client.DeleteFile(remotePath);
        }

        return Task.CompletedTask;
    }

    private static FluentFTP.FtpClient CreateClient(FtpCredentials credentials)
    {
        var host = NormalizeHost(credentials.Host);
        var client = new FluentFTP.FtpClient(host);
        client.Port = credentials.Port;
        client.Credentials = new NetworkCredential(credentials.Username, credentials.Password);
        return client;
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
