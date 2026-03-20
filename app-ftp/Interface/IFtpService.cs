using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IFtpService
{
    Task<IFtpSession> OpenSessionAsync(FtpCredentials credentials, CancellationToken cancellationToken = default);
}
