using app_ftp.Services.Models;
using Renci.SshNet;

namespace app_ftp.Services.Protocols;

public class SftpProtocol
{
    private string _errorDescription = string.Empty;
    private SftpClient? _sftpClient;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public SftpProtocol(string host, int port, string username, string password)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
    }

    public bool Connect(int timeout)
    {
        try
        {
            _sftpClient = new SftpClient(_host, _port, _username, _password)
            {
                ConnectionInfo = { Timeout = TimeSpan.FromSeconds(timeout) },
                KeepAliveInterval = TimeSpan.FromSeconds(10)
            };
            _sftpClient.Connect();
            return true;
        }
        catch
        {
            throw new Exception("Surgió un error al conectarse con el SFTP");
        }
    }

    public bool UploadFile(string sourcePath, string destinationPath, bool overwrite)
    {
        try
        {
            _errorDescription = string.Empty;
            using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            Client.UploadFile(fileStream, destinationPath, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo subir el documento al SFTP " + ex.Message);
        }
    }

    public bool UploadByte(byte[] fileBytes, string destinationPath, bool overwrite)
    {
        try
        {
            _errorDescription = string.Empty;

            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new ArgumentException("El array de bytes está vacío o es nulo.");
            }

            if (!Client.IsConnected)
            {
                Client.Connect();
            }

            var folder = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(folder) && !Client.Exists(folder))
            {
                Client.CreateDirectory(folder);
            }

            var tempPath = Path.GetTempFileName();
            File.WriteAllBytes(tempPath, fileBytes);

            using var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
            Client.UploadFile(fileStream, destinationPath, overwrite);
            File.Delete(tempPath);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo subir el documento al SFTP: " + ex.Message, ex);
        }
    }

    public bool DownloadFile(string sftpPath, string localPath)
    {
        try
        {
            _errorDescription = string.Empty;
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
            Client.DownloadFile(sftpPath, fileStream);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo recuperar el documento del SFTP " + ex.Message);
        }
    }

    public byte[] DownloadFileBytes(string sftpPath)
    {
        try
        {
            _errorDescription = string.Empty;
            using var memoryStream = new MemoryStream();
            Client.DownloadFile(sftpPath, memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo recuperar el documento del SFTP " + ex.Message);
        }
    }

    public bool DeleteFile(string filePath)
    {
        try
        {
            _errorDescription = string.Empty;
            Client.DeleteFile(filePath);
            return true;
        }
        catch (Exception ex)
        {
            _errorDescription = "SFTP_0004: ERROR - " + ex.Message;
            return false;
        }
    }

    public bool FileExists(string filePath)
    {
        try
        {
            _errorDescription = string.Empty;
            return Client.Exists(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo validar la existencia en SFTP: " + ex.Message, ex);
        }
    }

    public DateTime? GetLastWriteTime(string filePath)
    {
        try
        {
            _errorDescription = string.Empty;
            if (!Client.Exists(filePath))
            {
                return null;
            }

            return Client.GetAttributes(filePath).LastWriteTimeUtc;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo obtener la fecha de modificacion en SFTP: " + ex.Message, ex);
        }
    }

    public IReadOnlyList<StorageItem> ListFiles(string rootPath, bool recursive)
    {
        try
        {
            _errorDescription = string.Empty;
            var normalizedRoot = string.IsNullOrWhiteSpace(rootPath) ? "." : rootPath.Replace("\\", "/");
            var items = new List<StorageItem>();
            LoadDirectory(items, normalizedRoot, normalizedRoot, recursive);
            return items;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo listar archivos en SFTP: " + ex.Message, ex);
        }
    }

    public bool RenameFile(string oldPath, string newPath)
    {
        try
        {
            _errorDescription = string.Empty;
            Client.RenameFile(oldPath, newPath);
            return true;
        }
        catch (Exception ex)
        {
            _errorDescription = "SFTP_0005: ERROR - " + ex.Message;
            return false;
        }
    }

    public bool DeleteDirectory(string folderPath)
    {
        try
        {
            _errorDescription = string.Empty;
            Client.DeleteDirectory(folderPath);
            return true;
        }
        catch (Exception ex)
        {
            _errorDescription = "SFTP_0006: ERROR - " + ex.Message;
            return false;
        }
    }

    public bool CreateDirectory(string folderPath)
    {
        try
        {
            _errorDescription = string.Empty;
            var folder = Path.GetDirectoryName(folderPath);
            if (!string.IsNullOrWhiteSpace(folder) && !Client.Exists(folder))
            {
                Client.CreateDirectory(folder);
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Surgió un error al crear la carpeta SFTP: " + ex.Message);
        }
    }

    public bool CreateTextFile(string filePath)
    {
        try
        {
            _errorDescription = string.Empty;
            using var stream = new MemoryStream();
            Client.UploadFile(stream, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _errorDescription = "SFTP_0008: ERROR - " + ex.Message;
            return false;
        }
    }

    public void Close()
    {
        try
        {
            _sftpClient?.Disconnect();
        }
        finally
        {
            _sftpClient = null;
        }
    }

    public string GetErrorDescription()
    {
        return _errorDescription;
    }

    private SftpClient Client => _sftpClient ?? throw new InvalidOperationException("La conexion SFTP no esta inicializada.");

    private void LoadDirectory(List<StorageItem> items, string rootPath, string currentPath, bool recursive)
    {
        foreach (var entry in Client.ListDirectory(currentPath))
        {
            if (entry.Name is "." or "..")
            {
                continue;
            }

            if (entry.IsDirectory)
            {
                if (recursive)
                {
                    LoadDirectory(items, rootPath, entry.FullName, true);
                }

                continue;
            }

            items.Add(new StorageItem
            {
                FullPath = entry.FullName,
                RelativePath = GetRelativeRemotePath(rootPath, entry.FullName),
                Size = entry.Attributes.Size,
                ModifiedAt = entry.Attributes.LastWriteTimeUtc
            });
        }
    }

    private static string GetRelativeRemotePath(string rootPath, string fullName)
    {
        var normalizedRoot = NormalizeRemotePath(rootPath).TrimEnd('/');
        var normalizedFullName = NormalizeRemotePath(fullName);

        if (!normalizedFullName.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullName.TrimStart('/');
        }

        return normalizedFullName[normalizedRoot.Length..].TrimStart('/');
    }

    private static string NormalizeRemotePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var normalized = value.Replace("\\", "/");
        return normalized.StartsWith('/') ? normalized : $"/{normalized}";
    }
}
