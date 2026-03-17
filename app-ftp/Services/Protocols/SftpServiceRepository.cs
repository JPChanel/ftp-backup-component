using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Protocols;

public class SftpServiceRepository : ISftpService
{
    public byte[] DownloadFileByte(FtpCredentials credentials, string document_path)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        var document = sftpProtocol.DownloadFileBytes(document_path);
        sftpProtocol.Close();
        return document;
    }

    public bool DownloadFilePath(FtpCredentials credentials, string document_path, string path_local)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        var success = sftpProtocol.DownloadFile(document_path, path_local);
        sftpProtocol.Close();
        return success;
    }

    public Task UploadBytes(FtpCredentials credentials, byte[] document, string remotePath)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        sftpProtocol.CreateDirectory(remotePath);
        sftpProtocol.UploadByte(document, remotePath, true);
        sftpProtocol.Close();
        return Task.CompletedTask;
    }

    public Task UploadFileAsync(FtpCredentials credentials, string filePath, string remotePath)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        sftpProtocol.CreateDirectory(remotePath);
        sftpProtocol.UploadFile(filePath, remotePath, true);
        sftpProtocol.Close();
        return Task.CompletedTask;
    }

    public Task<bool> FileExists(FtpCredentials credentials, string remotePath)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        var exists = sftpProtocol.FileExists(remotePath);
        sftpProtocol.Close();
        return Task.FromResult(exists);
    }

    public Task<DateTime?> GetLastModified(FtpCredentials credentials, string remotePath)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        var value = sftpProtocol.GetLastWriteTime(remotePath);
        sftpProtocol.Close();
        return Task.FromResult<DateTime?>(value);
    }

    public Task<IReadOnlyList<StorageItem>> ListFiles(FtpCredentials credentials, string remotePath, bool recursive)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        var items = sftpProtocol.ListFiles(remotePath, recursive);
        sftpProtocol.Close();
        return Task.FromResult<IReadOnlyList<StorageItem>>(items);
    }

    public Task DeleteFile(FtpCredentials credentials, string remotePath)
    {
        var sftpProtocol = new SftpProtocol(credentials.Host, credentials.Port, credentials.Username, credentials.Password);
        sftpProtocol.Connect(120);
        sftpProtocol.DeleteFile(remotePath);
        sftpProtocol.Close();
        return Task.CompletedTask;
    }
}
