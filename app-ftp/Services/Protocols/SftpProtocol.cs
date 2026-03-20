using app_ftp.Services.Models;
using Renci.SshNet;

namespace app_ftp.Services.Protocols;

public class SftpProtocol
{
    private string _errorDescription = string.Empty;
    private SftpClient? _sftpClient;
    private readonly FtpCredentials _credentials;

    public SftpProtocol(FtpCredentials credentials)
    {
        _credentials = credentials;
    }

    public bool Connect(int timeout)
    {
        try
        {
            _sftpClient = CreateClient(timeout);
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

    public Stream OpenRead(string sftpPath)
    {
        try
        {
            _errorDescription = string.Empty;
            return Client.OpenRead(sftpPath);
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo abrir el stream de lectura SFTP: " + ex.Message, ex);
        }
    }

    public Stream OpenWrite(string sftpPath, bool overwrite)
    {
        try
        {
            _errorDescription = string.Empty;
            if (!overwrite && Client.Exists(sftpPath))
            {
                throw new IOException($"El archivo ya existe en destino: {sftpPath}");
            }

            CreateDirectory(sftpPath);
            return overwrite ? Client.OpenWrite(sftpPath) : Client.Create(sftpPath);
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo abrir el stream de escritura SFTP: " + ex.Message, ex);
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

    public bool DirectoryExists(string directoryPath)
    {
        try
        {
            _errorDescription = string.Empty;
            if (!Client.Exists(directoryPath))
            {
                return false;
            }

            return Client.GetAttributes(directoryPath).IsDirectory;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo validar la carpeta en SFTP: " + ex.Message, ex);
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

    public long? GetFileSize(string filePath)
    {
        try
        {
            _errorDescription = string.Empty;
            if (!Client.Exists(filePath))
            {
                return null;
            }

            return Client.GetAttributes(filePath).Size;
        }
        catch (Exception ex)
        {
            throw new Exception("No se pudo obtener el tamano del archivo en SFTP: " + ex.Message, ex);
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

    public bool IsConnected => _sftpClient?.IsConnected == true;

    private SftpClient Client => _sftpClient ?? throw new InvalidOperationException("La conexion SFTP no esta inicializada.");

    private SftpClient CreateClient(int timeout)
    {
        if (!string.IsNullOrWhiteSpace(_credentials.PrivateKeyPath))
        {
            if (!File.Exists(_credentials.PrivateKeyPath))
            {
                throw new Exception("La llave privada configurada no existe.");
            }

            var methods = new List<AuthenticationMethod>
            {
                new PrivateKeyAuthenticationMethod(_credentials.Username, new PrivateKeyFile(_credentials.PrivateKeyPath))
            };

            if (!string.IsNullOrWhiteSpace(_credentials.Password))
            {
                methods.Add(new PasswordAuthenticationMethod(_credentials.Username, _credentials.Password));
            }

            var connectionInfo = new ConnectionInfo(_credentials.Host, _credentials.Port, _credentials.Username, methods.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            return new SftpClient(connectionInfo)
            {
                KeepAliveInterval = TimeSpan.FromSeconds(10)
            };
        }

        return new SftpClient(_credentials.Host, _credentials.Port, _credentials.Username, _credentials.Password)
        {
            ConnectionInfo = { Timeout = TimeSpan.FromSeconds(timeout) },
            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };
    }

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
                items.Add(new StorageItem
                {
                    FullPath = entry.FullName,
                    RelativePath = GetRelativeRemotePath(rootPath, entry.FullName),
                    IsDirectory = true,
                    ModifiedAt = entry.Attributes.LastWriteTimeUtc
                });

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
                IsDirectory = false,
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
