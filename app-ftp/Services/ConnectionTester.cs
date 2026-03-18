using app_ftp.Config;
using app_ftp.Interface;
using app_ftp.Services.Models;
using Renci.SshNet;
using System.Diagnostics;
using System.Net;

namespace app_ftp.Services;

public class ConnectionTester : IConnectionTester
{
    public async Task<ConnectionTestResult> TestAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await RunWithRetriesAsync(profile, cancellationToken);

            stopwatch.Stop();
            return new ConnectionTestResult
            {
                Success = true,
                Message = $"Conexion valida en {stopwatch.ElapsedMilliseconds} ms.",
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            throw;
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

    private static void TestInternal(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (profile.Type)
        {
            case ConnectionType.LocalFolder:
                TestLocal(profile, cancellationToken);
                break;
            case ConnectionType.Ftp:
                TestFtp(profile, cancellationToken);
                break;
            case ConnectionType.Sftp:
                TestSftp(profile, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("Debes seleccionar un tipo de conexion.");
        }
    }

    private static void TestLocal(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(profile.Host))
        {
            throw new InvalidOperationException("La ruta local es obligatoria.");
        }

        if (!Directory.Exists(profile.Host))
        {
            Directory.CreateDirectory(profile.Host);
        }
    }

    private static void TestFtp(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        var host = NormalizeFtpHost(profile.Host);
        using var client = new FluentFTP.FtpClient(host)
        {
            Port = profile.Port,
            Credentials = new NetworkCredential(profile.Username, profile.Password)
        };
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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            client.AutoConnect();
            cancellationToken.ThrowIfCancellationRequested();

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

    private static void TestSftp(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        using var client = CreateSftpClient(profile);
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
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

        try
        {
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(Math.Max(1, profile.TimeoutSeconds));
            cancellationToken.ThrowIfCancellationRequested();
            client.Connect();
            cancellationToken.ThrowIfCancellationRequested();

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

    private static void ObserveBackgroundFault(Task task)
    {
        _ = task.ContinueWith(
            completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task RunWithRetriesAsync(ConnectionProfile profile, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(0, profile.RetryCount) + 1;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, profile.TimeoutSeconds)));
            var effectiveToken = timeoutCts.Token;

            var operationTask = Task.Run(() => TestInternal(profile, effectiveToken), CancellationToken.None);

            try
            {
                await operationTask.WaitAsync(effectiveToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ObserveBackgroundFault(operationTask);
                throw;
            }
            catch (OperationCanceledException)
            {
                ObserveBackgroundFault(operationTask);
                lastException = new TimeoutException($"La prueba de conexion supero el timeout de {profile.TimeoutSeconds} segundo(s).");
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

        throw lastException ?? new InvalidOperationException("La prueba de conexion fallo sin detalles.");
    }
}
