namespace app_ftp.Services.Models;

public class FtpCredentials
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string HostKeyFingerprint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
    public int RetryCount { get; set; } = 1;
}
