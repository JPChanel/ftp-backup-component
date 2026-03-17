using app_ftp.Config;
using app_ftp.Interface;
using app_ftp.Services.Models;
using Renci.SshNet;
using System.Diagnostics;
using System.Net;

namespace app_ftp.Services;

public class ConnectionTester : IConnectionTester
{
    public async Task<ConnectionTestResult> TestAsync(ConnectionProfile profile)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Task.Run(() => TestInternal(profile));

            stopwatch.Stop();
            return new ConnectionTestResult
            {
                Success = true,
                Message = $"Conexion valida en {stopwatch.ElapsedMilliseconds} ms.",
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private static void TestInternal(ConnectionProfile profile)
    {
        switch (profile.Type)
        {
            case ConnectionType.LocalFolder:
                TestLocal(profile);
                break;
            case ConnectionType.Ftp:
                TestFtp(profile);
                break;
            case ConnectionType.Sftp:
                TestSftp(profile);
                break;
            default:
                throw new InvalidOperationException("Debes seleccionar un tipo de conexion.");
        }
    }

    private static void TestLocal(ConnectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Host))
        {
            throw new InvalidOperationException("La ruta local es obligatoria.");
        }

        if (!Directory.Exists(profile.Host))
        {
            Directory.CreateDirectory(profile.Host);
        }
    }

    private static void TestFtp(ConnectionProfile profile)
    {
        var host = NormalizeFtpHost(profile.Host);
        using var client = new FluentFTP.FtpClient(host)
        {
            Port = profile.Port,
            Credentials = new NetworkCredential(profile.Username, profile.Password)
        };

        try
        {
            client.AutoConnect();

            if (!string.IsNullOrWhiteSpace(profile.BasePath) && !client.DirectoryExists(profile.BasePath))
            {
                throw new InvalidOperationException("Se conecto al FTP, pero la ruta base no existe o no es accesible.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo conectar al FTP: {ex.Message}");
        }
    }

    private static void TestSftp(ConnectionProfile profile)
    {
        using var client = CreateSftpClient(profile);

        try
        {
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(15);
            client.Connect();

            if (!string.IsNullOrWhiteSpace(profile.BasePath) && !client.Exists(profile.BasePath))
            {
                throw new InvalidOperationException("Se conecto al SFTP, pero la ruta base no existe o no es accesible.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo conectar al SFTP: {ex.Message}");
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    private static SftpClient CreateSftpClient(ConnectionProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
        {
            if (!File.Exists(profile.PrivateKeyPath))
            {
                throw new InvalidOperationException("La llave privada configurada no existe.");
            }

            var methods = new List<AuthenticationMethod>
            {
                new PrivateKeyAuthenticationMethod(profile.Username, new PrivateKeyFile(profile.PrivateKeyPath))
            };

            if (!string.IsNullOrWhiteSpace(profile.Password))
            {
                methods.Add(new PasswordAuthenticationMethod(profile.Username, profile.Password));
            }

            var connectionInfo = new ConnectionInfo(profile.Host, profile.Port, profile.Username, methods.ToArray());
            return new SftpClient(connectionInfo);
        }

        return new SftpClient(profile.Host, profile.Port, profile.Username, profile.Password);
    }

    private static string NormalizeFtpHost(string host)
    {
        if (host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            return host["ftp://".Length..];
        }

        return host;
    }
}
