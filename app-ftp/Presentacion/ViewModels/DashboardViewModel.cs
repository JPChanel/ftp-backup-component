using app_ftp.Services.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace app_ftp.Presentacion.ViewModels;

public class DashboardViewModel : SectionViewModelBase
{
    public DashboardViewModel(MainViewModel parent) : base(parent) { }

    public int SuccessCount => Parent.SuccessCount;
    public int FailedCount => Parent.FailedCount;
    public string TotalTransferredText => Parent.TotalTransferredText;
    public string RunBackupButtonText => Parent.RunBackupButtonText;
    public bool IsRunningBackup => Parent.IsRunningBackup;
    public bool CanCloseBackupConsole => Parent.CanCloseBackupConsole;
    public bool IsBackupConsoleOpen { get => Parent.IsBackupConsoleOpen; set => Parent.IsBackupConsoleOpen = value; }
    public string BackupConsoleStatus { get => Parent.BackupConsoleStatus; set => Parent.BackupConsoleStatus = value; }
    public ObservableCollection<BackupProgressEntry> BackupConsoleEntries => Parent.BackupConsoleEntries;
    public IEnumerable<ConnectionProfile> Connections => Parent.Connections;
    public IEnumerable<ConnectionProfile> AvailableDestinationConnections => Parent.AvailableDestinationConnections;
    public ConnectionProfile? SelectedSourceConnection { get => Parent.SelectedSourceConnection; set => Parent.SelectedSourceConnection = value; }
    public ConnectionProfile? SelectedDestinationConnection { get => Parent.SelectedDestinationConnection; set => Parent.SelectedDestinationConnection = value; }
    public string SourcePath { get => Parent.SourcePath; set => Parent.SourcePath = value; }
    public string DestinationPath { get => Parent.DestinationPath; set => Parent.DestinationPath = value; }
    public bool DeleteSourceAfterCopy { get => Parent.DeleteSourceAfterCopy; set => Parent.DeleteSourceAfterCopy = value; }
    public DateTime? FilterFromDate { get => Parent.FilterFromDate; set => Parent.FilterFromDate = value; }
    public DateTime? FilterToDate { get => Parent.FilterToDate; set => Parent.FilterToDate = value; }
    public string BackupNotes { get => Parent.BackupNotes; set => Parent.BackupNotes = value; }
    public ICommand RunBackupCommand => Parent.RunBackupCommand;
    public ICommand CancelBackupCommand => Parent.CancelBackupCommand;
    public ICommand CloseBackupConsoleCommand => Parent.CloseBackupConsoleCommand;
}
