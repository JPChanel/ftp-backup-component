namespace app_ftp.Services.Models;

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
}
