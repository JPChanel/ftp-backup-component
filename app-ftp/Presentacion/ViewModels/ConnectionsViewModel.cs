using app_ftp.Services.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace app_ftp.Presentacion.ViewModels;

public class ConnectionsViewModel : SectionViewModelBase
{
    public ConnectionsViewModel(MainViewModel parent) : base(parent) { }

    public ObservableCollection<ConnectionProfile> Connections => Parent.Connections;
    public ConnectionProfile EditableConnection { get => Parent.EditableConnection; set => Parent.EditableConnection = value; }
    public string ConnectionEditorTitle => Parent.ConnectionEditorTitle;
    public bool IsConnectionEditorOpen => Parent.IsConnectionEditorOpen;
    public bool IsLocalConnectionType => Parent.IsLocalConnectionType;
    public bool IsRemoteConnectionType => Parent.IsRemoteConnectionType;
    public bool IsSftpConnectionType => Parent.IsSftpConnectionType;
    public ICommand CreateConnectionCommand => Parent.CreateConnectionCommand;
    public ICommand EditConnectionCommand => Parent.EditConnectionCommand;
    public ICommand DeleteConnectionCommand => Parent.DeleteConnectionCommand;
    public ICommand SaveConnectionCommand => Parent.SaveConnectionCommand;
    public ICommand ResetConnectionEditorCommand => Parent.ResetConnectionEditorCommand;
    public ICommand CloseConnectionEditorCommand => Parent.CloseConnectionEditorCommand;
    public ICommand TestConnectionCommand => Parent.TestConnectionCommand;
    public string TestConnectionButtonText => Parent.TestConnectionButtonText;
}
