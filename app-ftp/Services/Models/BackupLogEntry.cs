using System.Text.Json.Serialization;

namespace app_ftp.Services.Models;

public class BackupLogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ExecutionDetails { get; set; } = string.Empty;
    public string? ExecutionDetailsFilePath { get; set; }
    [JsonIgnore]
    public string? ExecutionDetailsFullPath { get; set; }
    public int FilesTransferred { get; set; }
    public int FilesSkipped { get; set; }
    public int SourceFilesDeleted { get; set; }
    public long BytesTransferred { get; set; }
    public string? ErrorDetail { get; set; }

    public string RouteSummary => $"{SourceName} -> {DestinationName}";
    public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm");
    public string SizeSummary => BytesTransferred switch
    {
        > 1024L * 1024L * 1024L => $"{BytesTransferred / 1024d / 1024d / 1024d:0.##} GB",
        > 1024L * 1024L => $"{BytesTransferred / 1024d / 1024d:0.##} MB",
        > 1024L => $"{BytesTransferred / 1024d:0.##} KB",
        _ => $"{BytesTransferred} B"
    };
}
