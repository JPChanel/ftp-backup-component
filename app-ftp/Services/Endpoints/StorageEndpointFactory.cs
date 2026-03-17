using app_ftp.Interface;
using app_ftp.Services.Models;

namespace app_ftp.Services.Endpoints;

public class StorageEndpointFactory : IStorageEndpointFactory
{
    private readonly IFtpService _ftpService;
    private readonly ISftpService _sftpService;

    public StorageEndpointFactory(IFtpService ftpService, ISftpService sftpService)
    {
        _ftpService = ftpService;
        _sftpService = sftpService;
    }

    public IStorageEndpoint Create(ConnectionProfile profile)
    {
        return profile.Type switch
        {
            Config.ConnectionType.LocalFolder => new LocalStorageEndpoint(profile),
            Config.ConnectionType.Ftp => new FtpStorageEndpoint(profile, _ftpService),
            Config.ConnectionType.Sftp => new SftpStorageEndpoint(profile, _sftpService),
            _ => throw new InvalidOperationException("Tipo de conexion no soportado.")
        };
    }
}
