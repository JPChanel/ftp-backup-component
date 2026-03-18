using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IConnectionTester
{
    Task<ConnectionTestResult> TestAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
}
