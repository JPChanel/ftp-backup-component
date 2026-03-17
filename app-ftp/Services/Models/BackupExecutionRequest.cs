namespace app_ftp.Services.Models;

public class BackupExecutionRequest
{
    public ConnectionProfile Source { get; set; } = null!;
    public ConnectionProfile Destination { get; set; } = null!;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; }
    public bool DeleteSourceAfterCopy { get; set; }
    public DateTime? FilterFromDate { get; set; }
    public DateTime? FilterToDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
