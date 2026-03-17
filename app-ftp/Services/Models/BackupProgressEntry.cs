namespace app_ftp.Services.Models;

public class BackupProgressEntry
{
    public DateTime Timestamp { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string TimestampText => Timestamp.ToString("HH:mm:ss");
}
