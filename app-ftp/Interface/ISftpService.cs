using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface ISftpService
{
    Task<ISftpSession> OpenSessionAsync(FtpCredentials credentials, CancellationToken cancellationToken = default);
}
