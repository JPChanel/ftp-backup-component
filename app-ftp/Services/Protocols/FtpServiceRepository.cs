using app_ftp.Interface;
using app_ftp.Services.Models;
using FluentFTP;
using System.Net;

namespace app_ftp.Services.Protocols;

public class FtpServiceRepository : IFtpService
{
    public async Task<IFtpSession> OpenSessionAsync(FtpCredentials credentials, CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(0, credentials.RetryCount) + 1;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var client = CreateClient(credentials);

            try
            {
                await ConnectAsync(client, credentials, cancellationToken);
                return new FtpSession(client, Math.Max(1, credentials.TimeoutSeconds), Math.Max(0, credentials.RetryCount));
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                client.Dispose();
                lastException = ex;
            }

            if (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(lastException?.Message ?? "No se pudo abrir la sesion FTP.", lastException);
    }

    private static async Task ConnectAsync(FtpClient client, FtpCredentials credentials, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, credentials.TimeoutSeconds)));
        var effectiveToken = timeoutCts.Token;

        var connectTask = Task.Run(() =>
        {
            using var registration = effectiveToken.Register(() => TryDisconnect(client));
            effectiveToken.ThrowIfCancellationRequested();
            client.AutoConnect();
            effectiveToken.ThrowIfCancellationRequested();
        }, CancellationToken.None);

        try
        {
            await connectTask.WaitAsync(effectiveToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ObserveBackgroundFault(connectTask);
            throw new TimeoutException($"La operacion FTP supero el timeout de {credentials.TimeoutSeconds} segundo(s).");
        }
        catch
        {
            ObserveBackgroundFault(connectTask);
            throw;
        }
    }

    private static FtpClient CreateClient(FtpCredentials credentials)
    {
        var host = NormalizeHost(credentials.Host);
        var timeoutMilliseconds = checked(Math.Max(1, credentials.TimeoutSeconds) * 1000);
        var client = new FtpClient(host)
        {
            Port = credentials.Port,
            Credentials = new NetworkCredential(credentials.Username, credentials.Password)
        };

        client.Config.ConnectTimeout = timeoutMilliseconds;
        client.Config.ReadTimeout = timeoutMilliseconds;
        client.Config.DataConnectionConnectTimeout = timeoutMilliseconds;
        client.Config.DataConnectionReadTimeout = timeoutMilliseconds;
        client.Config.SocketKeepAlive = true;

        return client;
    }

    private static void ObserveBackgroundFault(Task task)
    {
        _ = task.ContinueWith(
            completedTask => _ = completedTask.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void TryDisconnect(FtpClient client)
    {
        try
        {
            client.Disconnect();
        }
        catch
        {
        }
    }

    private static string NormalizeHost(string host)
    {
        if (host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            return host["ftp://".Length..];
        }

        return host;
    }

    private sealed class FtpSession : IFtpSession
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly FtpClient _client;
        private readonly int _timeoutSeconds;
        private readonly int _retryCount;
        private bool _disposed;

        public FtpSession(FtpClient client, int timeoutSeconds, int retryCount)
        {
            _client = client;
            _timeoutSeconds = timeoutSeconds;
            _retryCount = retryCount;
        }

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _client.FileExists(NormalizeRemotePath(remotePath)), cancellationToken);

        public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _client.DirectoryExists(NormalizeRemotePath(remotePath)), cancellationToken);

        public Task<DateTime?> GetLastModifiedAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync<DateTime?>(() =>
            {
                var normalizedPath = NormalizeRemotePath(remotePath);
                return _client.FileExists(normalizedPath)
                    ? _client.GetModifiedTime(normalizedPath)
                    : null;
            }, cancellationToken);

        public Task<long?> GetFileSizeAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync<long?>(() =>
            {
                var normalizedPath = NormalizeRemotePath(remotePath);
                if (!_client.FileExists(normalizedPath))
                {
                    return null;
                }

                return _client.GetFileSize(normalizedPath);
            }, cancellationToken);

        public Task<byte[]> DownloadBytesAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                _client.DownloadBytes(out var bytes, NormalizeRemotePath(remotePath));
                return bytes;
            }, cancellationToken);

        public Task DownloadToLocalFileAsync(string remotePath, string localPath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                if (!overwrite && File.Exists(localPath))
                {
                    return;
                }

                EnsureLocalDirectory(localPath);
                var status = _client.DownloadFile(localPath, NormalizeRemotePath(remotePath));
                if (status != FtpStatus.Success)
                {
                    throw new InvalidOperationException($"No se pudo descargar el archivo FTP: {remotePath}");
                }
            }, cancellationToken);

        public Task UploadBytesAsync(byte[] content, string remotePath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                var normalizedPath = NormalizeRemotePath(remotePath);
                if (!overwrite && _client.FileExists(normalizedPath))
                {
                    return;
                }

                EnsureRemoteDirectory(normalizedPath);
                var status = _client.UploadBytes(content, normalizedPath, FtpRemoteExists.Overwrite, true);
                if (status != FtpStatus.Success)
                {
                    throw new InvalidOperationException($"No se pudo subir el archivo FTP: {remotePath}");
                }
            }, cancellationToken);

        public Task UploadFromLocalFileAsync(string localPath, string remotePath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                var normalizedPath = NormalizeRemotePath(remotePath);
                if (!overwrite && _client.FileExists(normalizedPath))
                {
                    return;
                }

                EnsureRemoteDirectory(normalizedPath);
                var status = _client.UploadFile(localPath, normalizedPath, FtpRemoteExists.Overwrite, true);
                if (status != FtpStatus.Success)
                {
                    throw new InvalidOperationException($"No se pudo subir el archivo FTP: {remotePath}");
                }
            }, cancellationToken);

        public async Task<Stream> OpenReadStreamAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeRemotePath(remotePath);
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected(cancellationToken);
                var stream = _client.OpenRead(normalizedPath);
                return new SessionBoundStream(stream, _gate, FinalizeFtpTransfer, null);
            }
            catch
            {
                _gate.Release();
                throw;
            }
        }

        public async Task<Stream> OpenWriteStreamAsync(string remotePath, bool overwrite, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizeRemotePath(remotePath);
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected(cancellationToken);
                if (!overwrite && _client.FileExists(normalizedPath))
                {
                    throw new IOException($"El archivo ya existe en destino: {remotePath}");
                }

                EnsureRemoteDirectory(normalizedPath);
                var stream = _client.OpenWrite(normalizedPath);
                return new SessionBoundStream(stream, _gate, FinalizeFtpTransfer, null);
            }
            catch
            {
                _gate.Release();
                throw;
            }
        }

        public Task<IReadOnlyList<StorageItem>> ListFilesAsync(string remotePath, bool recursive, CancellationToken cancellationToken = default) =>
            RunAsync<IReadOnlyList<StorageItem>>(() =>
            {
                var root = NormalizeRemotePath(remotePath);
                var listing = recursive
                    ? _client.GetListing(root, FtpListOption.Recursive)
                    : _client.GetListing(root);

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
            }, cancellationToken);

        public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                var normalizedSourcePath = NormalizeRemotePath(sourcePath);
                var normalizedDestinationPath = NormalizeRemotePath(destinationPath);

                if (!_client.FileExists(normalizedSourcePath))
                {
                    throw new FileNotFoundException("No se encontro el archivo temporal en FTP.", sourcePath);
                }

                EnsureRemoteDirectory(normalizedDestinationPath);
                if (overwrite && _client.FileExists(normalizedDestinationPath))
                {
                    _client.DeleteFile(normalizedDestinationPath);
                }
                else if (!overwrite && _client.FileExists(normalizedDestinationPath))
                {
                    throw new IOException($"El archivo ya existe en destino: {destinationPath}");
                }

                _client.MoveFile(normalizedSourcePath, normalizedDestinationPath);
            }, cancellationToken);

        public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                var normalizedPath = NormalizeRemotePath(remotePath);
                if (_client.FileExists(normalizedPath))
                {
                    _client.DeleteFile(normalizedPath);
                }
            }, cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _gate.WaitAsync();
            try
            {
                TryDisconnect(_client);
                _client.Dispose();
            }
            finally
            {
                _gate.Release();
                _gate.Dispose();
            }
        }

        private async Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                using var registration = cancellationToken.Register(() => TryDisconnect(_client));
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    EnsureConnected(cancellationToken);
                    return action();
                }, CancellationToken.None);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task RunAsync(Action action, CancellationToken cancellationToken)
        {
            await RunAsync(() =>
            {
                action();
                return true;
            }, cancellationToken);
        }

        private void EnsureConnected(CancellationToken cancellationToken)
        {
            if (_client.IsConnected)
            {
                return;
            }

            var attempts = _retryCount + 1;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    ConnectWithTimeout(cancellationToken);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    TryDisconnect(_client);
                }

                if (attempt < attempts)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(250 * attempt));
                }
            }

            throw new InvalidOperationException(lastException?.Message ?? "No se pudo restablecer la sesion FTP.", lastException);
        }

        private void ConnectWithTimeout(CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));
            var effectiveToken = timeoutCts.Token;

            var connectTask = Task.Run(() =>
            {
                using var registration = effectiveToken.Register(() => TryDisconnect(_client));
                effectiveToken.ThrowIfCancellationRequested();
                _client.AutoConnect();
                effectiveToken.ThrowIfCancellationRequested();
            }, CancellationToken.None);

            try
            {
                connectTask.WaitAsync(effectiveToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                ObserveBackgroundFault(connectTask);
                throw new TimeoutException($"La reconexion FTP supero el timeout de {_timeoutSeconds} segundo(s).");
            }
            catch
            {
                ObserveBackgroundFault(connectTask);
                throw;
            }
        }

        private void FinalizeFtpTransfer()
        {
            _client.GetReply();
        }

        private void EnsureRemoteDirectory(string remotePath)
        {
            var directory = GetRemoteDirectory(remotePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _client.CreateDirectory(directory);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private static void EnsureLocalDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
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

    private sealed class SessionBoundStream : Stream
    {
        private readonly Stream _inner;
        private readonly SemaphoreSlim _gate;
        private readonly Action? _onDispose;
        private readonly Func<ValueTask>? _onDisposeAsync;
        private bool _disposed;

        public SessionBoundStream(Stream inner, SemaphoreSlim gate, Action? onDispose, Func<ValueTask>? onDisposeAsync)
        {
            _inner = inner;
            _gate = gate;
            _onDispose = onDispose;
            _onDisposeAsync = onDisposeAsync;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _inner.WriteAsync(buffer, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        _inner.Dispose();
                        _onDispose?.Invoke();
                    }
                }
                finally
                {
                    _disposed = true;
                    _gate.Release();
                }
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await _inner.DisposeAsync();
                if (_onDisposeAsync is not null)
                {
                    await _onDisposeAsync();
                }
                else
                {
                    _onDispose?.Invoke();
                }
            }
            finally
            {
                _disposed = true;
                _gate.Release();
            }
        }
    }
}
