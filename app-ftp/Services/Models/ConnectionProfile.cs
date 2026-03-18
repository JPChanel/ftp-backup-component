using app_ftp.Config;
using app_ftp.Presentacion.Common;

namespace app_ftp.Services.Models;

public class ConnectionProfile : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _name = string.Empty;
    private ConnectionType _type = ConnectionType.None;
    private string _host = string.Empty;
    private int _port;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _basePath = string.Empty;
    private string _privateKeyPath = string.Empty;
    private bool _isEnabled = true;
    private int _timeoutSeconds = 120;
    private int _retryCount = 1;

    public Guid Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public ConnectionType Type { get => _type; set => SetProperty(ref _type, value); }
    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public int Port { get => _port; set => SetProperty(ref _port, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string BasePath { get => _basePath; set => SetProperty(ref _basePath, value); }
    public string PrivateKeyPath { get => _privateKeyPath; set => SetProperty(ref _privateKeyPath, value); }
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }
    public int TimeoutSeconds { get => _timeoutSeconds; set => SetProperty(ref _timeoutSeconds, value); }
    public int RetryCount { get => _retryCount; set => SetProperty(ref _retryCount, value); }

    public string DisplayName => $"{Name} ({TypeLabel})";
    public string TypeLabel => Type switch
    {
        ConnectionType.Ftp => "FTP",
        ConnectionType.Sftp => "SFTP",
        ConnectionType.LocalFolder => "LOCAL",
        _ => "SIN TIPO"
    };
    public string Summary => Type switch
    {
        ConnectionType.LocalFolder => Host,
        ConnectionType.Ftp or ConnectionType.Sftp when !string.IsNullOrWhiteSpace(Host) && Port > 0 => $"{Host}:{Port}",
        _ => Host
    };
    public string PathLabel => string.IsNullOrWhiteSpace(BasePath) ? "/" : BasePath;

    public FtpCredentials ToCredentials()
    {
        return new FtpCredentials
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            PrivateKeyPath = PrivateKeyPath,
            TimeoutSeconds = TimeoutSeconds,
            RetryCount = RetryCount
        };
    }

    public ConnectionProfile Clone()
    {
        return new ConnectionProfile
        {
            Id = Id,
            Name = Name,
            Type = Type,
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            BasePath = BasePath,
            PrivateKeyPath = PrivateKeyPath,
            IsEnabled = IsEnabled,
            TimeoutSeconds = TimeoutSeconds,
            RetryCount = RetryCount
        };
    }
}
