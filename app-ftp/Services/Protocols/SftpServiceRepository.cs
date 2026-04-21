using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Protocols;

public class SftpServiceRepository : ISftpService
{
    public async Task<ISftpSession> OpenSessionAsync(FtpCredentials credentials, CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(0, credentials.RetryCount) + 1;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var protocol = new SftpProtocol(credentials);

            try
            {
                await ConnectAsync(protocol, credentials, cancellationToken);
                return new SftpSession(protocol, Math.Max(1, credentials.TimeoutSeconds));
            }
            catch (OperationCanceledException)
            {
                protocol.Close();
                throw;
            }
            catch (Exception ex)
            {
                protocol.Close();
                lastException = ex;
            }

            if (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(lastException?.Message ?? "No se pudo abrir la sesion SFTP.", lastException);
    }

    private static async Task ConnectAsync(SftpProtocol protocol, FtpCredentials credentials, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, credentials.TimeoutSeconds)));
        var effectiveToken = timeoutCts.Token;

        var connectTask = Task.Run(() =>
        {
            using var registration = effectiveToken.Register(protocol.Close);
            effectiveToken.ThrowIfCancellationRequested();
            protocol.Connect(Math.Max(1, credentials.TimeoutSeconds));
            effectiveToken.ThrowIfCancellationRequested();
        }, CancellationToken.None);

        try
        {
            await connectTask.WaitAsync(effectiveToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ObserveBackgroundFault(connectTask);
            throw new TimeoutException($"La operacion SFTP supero el timeout de {credentials.TimeoutSeconds} segundo(s).");
        }
        catch
        {
            ObserveBackgroundFault(connectTask);
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

    private sealed class SftpSession : ISftpSession
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly SftpProtocol _protocol;
        private readonly int _timeoutSeconds;
        private bool _disposed;

        public SftpSession(SftpProtocol protocol, int timeoutSeconds)
        {
            _protocol = protocol;
            _timeoutSeconds = timeoutSeconds;
        }

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.FileExists(remotePath), cancellationToken);

        public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.DirectoryExists(remotePath), cancellationToken);

        public Task<DateTime?> GetLastModifiedAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.GetLastWriteTime(remotePath), cancellationToken);

        public Task<long?> GetFileSizeAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.GetFileSize(remotePath), cancellationToken);

        public Task<byte[]> DownloadBytesAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.DownloadFileBytes(remotePath), cancellationToken);

        public async Task DownloadToLocalFileAsync(string remotePath, string localPath, bool overwrite, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();

                if (!overwrite && File.Exists(localPath))
                {
                    return;
                }

                EnsureLocalDirectory(localPath);
                await _protocol.DownloadFileAsync(remotePath, localPath, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task UploadBytesAsync(byte[] content, string remotePath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                if (!overwrite && _protocol.FileExists(remotePath))
                {
                    return;
                }

                _protocol.CreateDirectory(remotePath);
                _ = _protocol.UploadByte(content, remotePath, overwrite);
            }, cancellationToken);

        public async Task UploadFromLocalFileAsync(string localPath, string remotePath, bool overwrite, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();

                if (!overwrite && _protocol.FileExists(remotePath))
                {
                    return;
                }

                _protocol.CreateDirectory(remotePath);
                await _protocol.UploadFileAsync(localPath, remotePath, overwrite, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<Stream> OpenReadStreamAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();
                var stream = _protocol.OpenRead(remotePath);
                return new SessionBoundStream(stream, _gate);
            }
            catch
            {
                _gate.Release();
                throw;
            }
        }

        public async Task<Stream> OpenWriteStreamAsync(string remotePath, bool overwrite, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();
                var stream = _protocol.OpenWrite(remotePath, overwrite);
                return new SessionBoundStream(stream, _gate);
            }
            catch
            {
                _gate.Release();
                throw;
            }
        }

        public async IAsyncEnumerable<StorageItem> ListFilesAsync(string remotePath, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();
                
                await foreach (var item in _protocol.ListFilesAsync(remotePath, recursive, cancellationToken))
                {
                    yield return item;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> HasEntriesAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                EnsureConnected();
                return await _protocol.HasEntriesAsync(remotePath, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                if (!_protocol.FileExists(sourcePath))
                {
                    throw new FileNotFoundException("No se encontro el archivo temporal en SFTP.", sourcePath);
                }

                if (overwrite && _protocol.FileExists(destinationPath))
                {
                    _ = _protocol.DeleteFile(destinationPath);
                }
                else if (!overwrite && _protocol.FileExists(destinationPath))
                {
                    throw new IOException($"El archivo ya existe en destino: {destinationPath}");
                }

                _protocol.CreateDirectory(destinationPath);
                _ = _protocol.RenameFile(sourcePath, destinationPath);
            }, cancellationToken);

        public Task<bool> TrySetLastModifiedAsync(string remotePath, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            RunAsync(() => _protocol.TrySetLastWriteTimeUtc(remotePath, NormalizeUtc(modifiedAt)), cancellationToken);

        public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) =>
            RunAsync(() =>
            {
                _ = _protocol.DeleteFile(remotePath);
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
                _protocol.Close();
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
                using var registration = cancellationToken.Register(_protocol.Close);
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    EnsureConnected();
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

        private void EnsureConnected()
        {
            if (!_protocol.IsConnected)
            {
                _protocol.Connect(_timeoutSeconds);
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

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
            };
        }
    }

    private sealed class SessionBoundStream : Stream
    {
        private readonly Stream _inner;
        private readonly SemaphoreSlim _gate;
        private bool _disposed;

        public SessionBoundStream(Stream inner, SemaphoreSlim gate)
        {
            _inner = inner;
            _gate = gate;
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
            }
            finally
            {
                _disposed = true;
                _gate.Release();
            }
        }
    }
}
