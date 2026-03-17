using app_ftp.Services.Models;

namespace app_ftp.Interface;

public interface IStorageEndpointFactory
{
    IStorageEndpoint Create(ConnectionProfile profile);
}
