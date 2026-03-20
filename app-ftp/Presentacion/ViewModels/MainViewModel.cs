using app_ftp.Config;
using app_ftp.Interface;
using app_ftp.Presentacion.Common;
using app_ftp.Services;
using app_ftp.Services.Models;
using app_ftp.Services.Updates;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace app_ftp.Presentacion.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ConnectionStore _connectionStore;
    private readonly SettingsStore _settingsStore;
    private readonly LogStore _logStore;
    private readonly BackupOrchestrator _backupOrchestrator;
    private readonly IConnectionTester _connectionTester;
    private readonly MainThreadNotifier _notifier;
    private readonly CheckForUpdatesUseCase _checkUpdatesUseCase;
    private readonly DownloadUpdateUseCase _downloadUpdateUseCase;
    private readonly InstallUpdateUseCase _installUpdateUseCase;
    private UiSection _selectedSection = UiSection.Dashboard;
    private ConnectionProfile? _selectedSourceConnection;
    private ConnectionProfile? _selectedDestinationConnection;
    private string _sourcePath = string.Empty;
    private string _destinationPath = string.Empty;
    private string _backupNotes = string.Empty;
    private DateTime? _filterFromDate;
    private DateTime? _filterToDate;
    private string _statusMessage = string.Empty;
    private bool _statusIsError;
    private string _logSearchText = string.Empty;
    private BackupLogEntry? _selectedLog;
    private bool _isRunningBackup;
    private bool _isTestingConnection;
    private CancellationTokenSource? _backupCancellationSource;
    private ConnectionProfile _editableConnection = new();
    private bool _deleteSourceAfterCopy;
    private bool _isConnectionEditorOpen;
    private bool _isBackupConsoleOpen;
    private string _backupConsoleStatus = string.Empty;
    private bool _useFilterTime;

    // Update Properties
    private string _appVersion = string.Empty;
    private bool _isUpdateAvailable;
    private Velopack.UpdateInfo? _availableUpdateInfo;
    private bool _isDownloadingUpdate;
    private int _updateDownloadProgress;
    private string _updateDownloadStatus = string.Empty;
    private string _testConnectionStatusText = string.Empty;
    private bool? _lastTestConnectionSucceeded;
    private BackupProgressEntry? _liveConsoleEntry;

    public MainViewModel(
        ConnectionStore connectionStore,
        SettingsStore settingsStore,
        LogStore logStore,
        BackupOrchestrator backupOrchestrator,
        IConnectionTester connectionTester,
        MainThreadNotifier notifier,
        CheckForUpdatesUseCase checkUpdatesUseCase,
        DownloadUpdateUseCase downloadUpdateUseCase,
        InstallUpdateUseCase installUpdateUseCase)
    {
        _connectionStore = connectionStore;
        _settingsStore = settingsStore;
        _logStore = logStore;
        _backupOrchestrator = backupOrchestrator;
        _connectionTester = connectionTester;
        _notifier = notifier;
        _checkUpdatesUseCase = checkUpdatesUseCase;
        _downloadUpdateUseCase = downloadUpdateUseCase;
        _installUpdateUseCase = installUpdateUseCase;

        Connections = new ObservableCollection<ConnectionProfile>(_connectionStore.Load());
        Settings = _settingsStore.Load();
        Logs = new ObservableCollection<BackupLogEntry>(_logStore.Load().OrderByDescending(x => x.Timestamp));
        FilteredLogs = new ObservableCollection<BackupLogEntry>(Logs);
        Dashboard = new DashboardViewModel(this);
        ConnectionsSection = new ConnectionsViewModel(this);
        LogsSection = new LogsViewModel(this);

        SelectedSourceConnection = Connections.FirstOrDefault();
        SelectedDestinationConnection = Connections.Skip(1).FirstOrDefault() ?? Connections.FirstOrDefault();

        ShowDashboardCommand = new RelayCommand(() => SetSection(UiSection.Dashboard));
        ShowConnectionsCommand = new RelayCommand(() => SetSection(UiSection.Connections));
        ShowLogsCommand = new RelayCommand(() => SetSection(UiSection.Logs));
        ClearStatusCommand = new RelayCommand(() => StatusMessage = string.Empty);
        CreateConnectionCommand = new RelayCommand(CreateConnection);
        EditConnectionCommand = new RelayCommand(EditConnection);
        DeleteConnectionCommand = new RelayCommand(DeleteConnection);
        SaveConnectionCommand = new RelayCommand(SaveConnection);
        ResetConnectionEditorCommand = new RelayCommand(CreateConnection);
        CloseConnectionEditorCommand = new RelayCommand(CloseConnectionEditor);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, () => !_isTestingConnection);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RunBackupCommand = new AsyncRelayCommand(RunBackupAsync, () => !_isRunningBackup);
        CancelBackupCommand = new RelayCommand(CancelBackup);

        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
        InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync);

        _notifier.StatusPublished += (message, isError) =>
        {
            StatusMessage = message;
            _statusIsError = isError;
            RaiseStatusColors();
        };

        AttachEditableConnectionHandlers(_editableConnection);
        UpdateDashboard();
        SetSection(UiSection.Dashboard);

        AppVersion = $"v{_checkUpdatesUseCase.GetCurrentVersion()}";
        _ = CheckForUpdatesAsync();
    }

    public ObservableCollection<ConnectionProfile> Connections { get; }
    public ObservableCollection<BackupLogEntry> Logs { get; }
    public ObservableCollection<BackupLogEntry> FilteredLogs { get; }
    public ObservableCollection<BackupProgressEntry> BackupConsoleEntries { get; } = [];
    public AppSettings Settings { get; }
    public DashboardViewModel Dashboard { get; }
    public ConnectionsViewModel ConnectionsSection { get; }
    public LogsViewModel LogsSection { get; }

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowConnectionsCommand { get; }
    public ICommand ShowLogsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ClearStatusCommand { get; }
    public ICommand CreateConnectionCommand { get; }
    public ICommand EditConnectionCommand { get; }
    public ICommand DeleteConnectionCommand { get; }
    public ICommand SaveConnectionCommand { get; }
    public ICommand ResetConnectionEditorCommand { get; }
    public ICommand CloseConnectionEditorCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand RunBackupCommand { get; }
    public ICommand CancelBackupCommand { get; }
    public ICommand CloseBackupConsoleCommand => new RelayCommand(() => IsBackupConsoleOpen = false);
    public ICommand CheckForUpdatesCommand { get; }
    public ICommand InstallUpdateCommand { get; }

    public ConnectionProfile? SelectedSourceConnection
    {
        get => _selectedSourceConnection;
        set
        {
            if (SetProperty(ref _selectedSourceConnection, value))
            {
                OnPropertyChanged(nameof(AvailableDestinationConnections));
                OnPropertyChanged(nameof(SourceRootDisplay));
                OnPropertyChanged(nameof(SourceRoutePreview));
                EnsureDistinctDestinationSelection();
            }
        }
    }

    public ConnectionProfile? SelectedDestinationConnection
    {
        get => _selectedDestinationConnection;
        set
        {
            if (SetProperty(ref _selectedDestinationConnection, value))
            {
                OnPropertyChanged(nameof(DestinationRootDisplay));
                OnPropertyChanged(nameof(DestinationRoutePreview));
            }
        }
    }


    public IEnumerable<ConnectionProfile> AvailableDestinationConnections =>
        Connections.Where(connection => SelectedSourceConnection is null || connection.Id != SelectedSourceConnection.Id);

    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(SourceRoutePreview));
            }
        }
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(DestinationRoutePreview));
            }
        }
    }

    public string BackupNotes
    {
        get => _backupNotes;
        set => SetProperty(ref _backupNotes, value);
    }

    public DateTime? FilterFromDate
    {
        get => _filterFromDate;
        set => SetProperty(ref _filterFromDate, value);
    }

    public DateTime? FilterToDate
    {
        get => _filterToDate;
        set => SetProperty(ref _filterToDate, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public Brush StatusBannerBackground => CreateBrush(_statusIsError ? "#FFF1F1" : "#EEF8F1");
    public Brush StatusBannerBorder => CreateBrush(_statusIsError ? "#F4CACA" : "#B6E0C2");
    public Brush StatusBannerForeground => CreateBrush(_statusIsError ? "#B63B48" : "#21784A");

    public string HeaderTitle => _selectedSection switch
    {
        UiSection.Connections => "Gestion de Conexiones",
        UiSection.Logs => "Registro de Eventos",
        _ => "Dashboard de Backups"
    };

    public string HeaderSubtitle => _selectedSection switch
    {
        UiSection.Connections => "Conexiones configurables para origen y destino.",
        UiSection.Logs => "Monitoreo y trazabilidad de cada transferencia.",
        _ => "Estado actual del motor de backup y ejecucion manual."
    };

    public bool IsDashboardVisible => _selectedSection == UiSection.Dashboard;
    public bool IsConnectionsVisible => _selectedSection == UiSection.Connections;
    public bool IsLogsVisible => _selectedSection == UiSection.Logs;

    public Brush DashboardButtonBackground => GetMenuBackground(UiSection.Dashboard);
    public Brush ConnectionsButtonBackground => GetMenuBackground(UiSection.Connections);
    public Brush LogsButtonBackground => GetMenuBackground(UiSection.Logs);
    public Brush DashboardButtonBorderBrush => GetMenuBorderBrush(UiSection.Dashboard);
    public Brush ConnectionsButtonBorderBrush => GetMenuBorderBrush(UiSection.Connections);
    public Brush LogsButtonBorderBrush => GetMenuBorderBrush(UiSection.Logs);
    public bool IsDashboardSelected => _selectedSection == UiSection.Dashboard;
    public bool IsConnectionsSelected => _selectedSection == UiSection.Connections;
    public bool IsLogsSelected => _selectedSection == UiSection.Logs;
    public Brush DashboardButtonForeground => GetMenuForeground(UiSection.Dashboard);
    public Brush ConnectionsButtonForeground => GetMenuForeground(UiSection.Connections);
    public Brush LogsButtonForeground => GetMenuForeground(UiSection.Logs);
    public Brush DashboardButtonAccentBrush => GetMenuAccentBrush(UiSection.Dashboard);
    public Brush ConnectionsButtonAccentBrush => GetMenuAccentBrush(UiSection.Connections);
    public Brush LogsButtonAccentBrush => GetMenuAccentBrush(UiSection.Logs);
    public double DashboardButtonAccentWidth => GetMenuAccentWidth(UiSection.Dashboard);
    public double ConnectionsButtonAccentWidth => GetMenuAccentWidth(UiSection.Connections);
    public double LogsButtonAccentWidth => GetMenuAccentWidth(UiSection.Logs);
    public Brush DashboardIconBackground => GetMenuIconBackground(UiSection.Dashboard);
    public Brush ConnectionsIconBackground => GetMenuIconBackground(UiSection.Connections);
    public Brush LogsIconBackground => GetMenuIconBackground(UiSection.Logs);
    public Brush DashboardIconBorderBrush => GetMenuIconBorderBrush(UiSection.Dashboard);
    public Brush ConnectionsIconBorderBrush => GetMenuIconBorderBrush(UiSection.Connections);
    public Brush LogsIconBorderBrush => GetMenuIconBorderBrush(UiSection.Logs);

    public int SuccessCount => Logs.Count(log => log.Status == "SUCCESS");
    public int FailedCount => Logs.Count(log => log.Status is "ERROR" or "PARTIAL");
    public string TotalTransferredText => ByteSizeFormatter.Format(Logs.Sum(log => log.BytesTransferred));
    public string RunBackupButtonText => _isRunningBackup ? "Procesando..." : "Iniciar Proceso de Backup";
    public bool IsRunningBackup => _isRunningBackup;
    public bool CanCloseBackupConsole => !_isRunningBackup;
    public bool IsBackupConsoleOpen
    {
        get => _isBackupConsoleOpen;
        set => SetProperty(ref _isBackupConsoleOpen, value);
    }
    public string BackupConsoleStatus
    {
        get => _backupConsoleStatus;
        set => SetProperty(ref _backupConsoleStatus, value);
    }
    public string TestConnectionButtonText => _isTestingConnection ? "Probando..." : "Probar conexion";
    public string TestConnectionStatusText
    {
        get => _testConnectionStatusText;
        set => SetProperty(ref _testConnectionStatusText, value);
    }
    public Brush TestConnectionButtonBackground => CreateBrush(_lastTestConnectionSucceeded switch
    {
        true => "#16A34A",
        false => "#DC2626",
        _ => "#FFFFFF"
    });
    public Brush TestConnectionButtonForeground => CreateBrush(_lastTestConnectionSucceeded.HasValue ? "#FFFFFF" : "#334155");
    public Brush TestConnectionStatusForeground => CreateBrush(_lastTestConnectionSucceeded switch
    {
        true => "#15803D",
        false => "#B91C1C",
        _ => "#64748B"
    });

    public string LogSearchText
    {
        get => _logSearchText;
        set
        {
            if (SetProperty(ref _logSearchText, value))
            {
                ApplyLogFilter();
            }
        }
    }

    public BackupLogEntry? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    public ConnectionProfile EditableConnection
    {
        get => _editableConnection;
        set
        {
            if (SetProperty(ref _editableConnection, value))
            {
                AttachEditableConnectionHandlers(value);
                RaiseConnectionEditorState();
                OnPropertyChanged(nameof(ConnectionEditorTitle));
            }
        }
    }

    public string ConnectionEditorTitle => EditableConnection.Id == Guid.Empty ? "Nueva Conexion" : "Editor de Conexion";
    public bool IsConnectionEditorOpen
    {
        get => _isConnectionEditorOpen;
        set => SetProperty(ref _isConnectionEditorOpen, value);
    }
    public bool IsLocalConnectionType => EditableConnection.Type == ConnectionType.LocalFolder;
    public bool IsRemoteConnectionType => EditableConnection.Type is ConnectionType.Ftp or ConnectionType.Sftp;
    public bool IsSftpConnectionType => EditableConnection.Type == ConnectionType.Sftp;
    public string SourceRootDisplay => BuildConnectionRootDisplay(SelectedSourceConnection);
    public string DestinationRootDisplay => BuildConnectionRootDisplay(SelectedDestinationConnection);
    public string SourceRoutePreview => BuildRoutePreview(SourcePath);
    public string DestinationRoutePreview => BuildRoutePreview(DestinationPath);
    public bool DeleteSourceAfterCopy
    {
        get => _deleteSourceAfterCopy;
        set => SetProperty(ref _deleteSourceAfterCopy, value);
    }

    public bool UseFilterTime
    {
        get => _useFilterTime;
        set => SetProperty(ref _useFilterTime, value);
    }

    public string AppVersion
    {
        get => _appVersion;
        set => SetProperty(ref _appVersion, value);
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        set => SetProperty(ref _isUpdateAvailable, value);
    }

    public bool IsDownloadingUpdate
    {
        get => _isDownloadingUpdate;
        set
        {
            if (SetProperty(ref _isDownloadingUpdate, value))
            {
                OnPropertyChanged(nameof(InstallUpdateButtonText));
                OnPropertyChanged(nameof(IsUpdateProgressVisible));
                OnPropertyChanged(nameof(IsUpdateProgressIndeterminate));
            }
        }
    }

    public int UpdateDownloadProgress
    {
        get => _updateDownloadProgress;
        set
        {
            if (SetProperty(ref _updateDownloadProgress, value))
            {
                OnPropertyChanged(nameof(UpdateDownloadProgressText));
                OnPropertyChanged(nameof(IsUpdateProgressVisible));
                OnPropertyChanged(nameof(IsUpdateProgressIndeterminate));
            }
        }
    }

    public string UpdateDownloadStatus
    {
        get => _updateDownloadStatus;
        set
        {
            if (SetProperty(ref _updateDownloadStatus, value))
            {
                OnPropertyChanged(nameof(HasUpdateStatus));
                OnPropertyChanged(nameof(IsUpdateProgressVisible));
            }
        }
    }

    public string InstallUpdateButtonText => IsDownloadingUpdate ? "DESCARGANDO..." : "ACTUALIZAR";
    public string UpdateDownloadProgressText => UpdateDownloadProgress <= 0 && !IsDownloadingUpdate ? string.Empty : $"{UpdateDownloadProgress}%";
    public bool HasUpdateStatus => !string.IsNullOrWhiteSpace(UpdateDownloadStatus);
    public bool IsUpdateProgressVisible => IsDownloadingUpdate || HasUpdateStatus || UpdateDownloadProgress > 0;
    public bool IsUpdateProgressIndeterminate => IsDownloadingUpdate && UpdateDownloadProgress <= 0;

    private void SetSection(UiSection section)
    {
        _selectedSection = section;
        OnPropertyChanged(nameof(IsDashboardVisible));
        OnPropertyChanged(nameof(IsConnectionsVisible));
        OnPropertyChanged(nameof(IsLogsVisible));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        RaiseMenuStyles();
    }

    private void CreateConnection()
    {
        EditableConnection = new ConnectionProfile
        {
            Id = Guid.Empty,
            Port = 21
        };
        IsConnectionEditorOpen = true;
    }

    private void EditConnection(object? parameter)
    {
        if (parameter is not ConnectionProfile connection)
        {
            return;
        }

        EditableConnection = connection.Clone();
        IsConnectionEditorOpen = true;
    }

    private void DeleteConnection(object? parameter)
    {
        if (parameter is not ConnectionProfile connection)
        {
            return;
        }

        Connections.Remove(connection);
        PersistConnections();
        StatusMessage = $"Conexion eliminada: {connection.Name}";
        _statusIsError = false;
        RaiseStatusColors();
    }

    private void SaveConnection()
    {
        if (EditableConnection.Type == ConnectionType.None)
        {
            _notifier.PublishError("Debes seleccionar un tipo de conexion.");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditableConnection.Name) || string.IsNullOrWhiteSpace(EditableConnection.Host))
        {
            _notifier.PublishError("Nombre y host/ruta son obligatorios.");
            return;
        }

        if (EditableConnection.Type == ConnectionType.LocalFolder)
        {
            EditableConnection.Port = 0;
        }
        else if (EditableConnection.Port <= 0)
        {
            _notifier.PublishError("El puerto es obligatorio para conexiones FTP y SFTP.");
            return;
        }

        if (EditableConnection.TimeoutSeconds <= 0)
        {
            _notifier.PublishError("El timeout debe ser mayor a 0 segundos.");
            return;
        }

        if (EditableConnection.RetryCount < 0)
        {
            _notifier.PublishError("Los reintentos no pueden ser negativos.");
            return;
        }

        if (EditableConnection.Id == Guid.Empty)
        {
            EditableConnection.Id = Guid.NewGuid();
            Connections.Add(EditableConnection.Clone());
        }
        else
        {
            var existing = Connections.FirstOrDefault(item => item.Id == EditableConnection.Id);
            if (existing is not null)
            {
                var index = Connections.IndexOf(existing);
                Connections[index] = EditableConnection.Clone();
            }
            else
            {
                Connections.Add(EditableConnection.Clone());
            }
        }

        PersistConnections();
        StatusMessage = $"Conexion guardada: {EditableConnection.Name}";
        _statusIsError = false;
        RaiseStatusColors();
        CloseConnectionEditor();
    }

    private void CloseConnectionEditor()
    {
        EditableConnection = new ConnectionProfile
        {
            Id = Guid.Empty,
            Port = 21
        };
        ResetTestConnectionVisualState();
        IsConnectionEditorOpen = false;
    }

    private void SaveSettings()
    {
        _settingsStore.Save(Settings);
        if (!string.IsNullOrWhiteSpace(Settings.LocalBackupRoot))
        {
            Directory.CreateDirectory(Settings.LocalBackupRoot);
        }
        StatusMessage = "Configuracion guardada.";
        _statusIsError = false;
        RaiseStatusColors();
    }

    private async Task TestConnectionAsync()
    {
        var validationMessage = ValidateEditableConnectionForTest();
        if (validationMessage is not null)
        {
            _lastTestConnectionSucceeded = false;
            TestConnectionStatusText = validationMessage;
            RaiseTestConnectionVisualState();
            _notifier.PublishError(validationMessage);
            return;
        }

        _isTestingConnection = true;
        OnPropertyChanged(nameof(TestConnectionButtonText));
        ((AsyncRelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();

        try
        {
            var result = await _connectionTester.TestAsync(EditableConnection.Clone());
            _lastTestConnectionSucceeded = result.Success;
            TestConnectionStatusText = result.Message;
            RaiseTestConnectionVisualState();
            if (result.Success)
            {
                _notifier.PublishSuccess(result.Message);
            }
            else
            {
                _notifier.PublishError(result.Message);
            }
        }
        finally
        {
            _isTestingConnection = false;
            OnPropertyChanged(nameof(TestConnectionButtonText));
            ((AsyncRelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();
        }
    }

    private async Task RunBackupAsync()
    {
        if (SelectedSourceConnection is null || SelectedDestinationConnection is null)
        {
            _notifier.PublishError("Debes seleccionar origen y destino.");
            return;
        }

        var normalizedFromDate = NormalizeFilterFromDate();
        var normalizedToDate = NormalizeFilterToDate();

        if (normalizedFromDate.HasValue && normalizedToDate.HasValue && normalizedFromDate > normalizedToDate)
        {
            _notifier.PublishError("La fecha inicial no puede ser mayor que la fecha final.");
            return;
        }

        if (SelectedSourceConnection.Id != Guid.Empty
            && SelectedSourceConnection.Id == SelectedDestinationConnection.Id
            && string.Equals(NormalizeUserRoutePath(SourcePath), NormalizeUserRoutePath(DestinationPath), StringComparison.OrdinalIgnoreCase))
        {
            _notifier.PublishError("Origen y destino no pueden ser la misma ruta.");
            return;
        }

        _isRunningBackup = true;
        _backupCancellationSource = new CancellationTokenSource();
        BackupConsoleEntries.Clear();
        _liveConsoleEntry = null;
        IsBackupConsoleOpen = true;
        BackupConsoleStatus = "Iniciando backup...";
        OnPropertyChanged(nameof(RunBackupButtonText));
        OnPropertyChanged(nameof(IsRunningBackup));
        OnPropertyChanged(nameof(CanCloseBackupConsole));
        ((AsyncRelayCommand)RunBackupCommand).RaiseCanExecuteChanged();

        try
        {
            var request = new BackupExecutionRequest
            {
                Source = SelectedSourceConnection,
                Destination = SelectedDestinationConnection,
                SourcePath = NormalizeUserRoutePath(SourcePath),
                DestinationPath = NormalizeUserRoutePath(DestinationPath),
                OverwriteExisting = true,
                DeleteSourceAfterCopy = DeleteSourceAfterCopy,
                FilterFromDate = normalizedFromDate,
                FilterToDate = normalizedToDate,
                Notes = BackupNotes
            };

            var progress = new Progress<BackupProgressEntry>(HandleConsoleProgress);

            var result = await _backupOrchestrator.ExecuteAsync(request, progress, _backupCancellationSource.Token);
            ClearLiveConsoleEntry();
            Logs.Insert(0, result);

            while (Logs.Count > Settings.MaxVisibleLogs)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }

            _logStore.Save(Logs);
            ApplyLogFilter();
            UpdateDashboard();

            if (result.Status == "SUCCESS")
            {
                BackupConsoleStatus = "Backup completado";
                _notifier.PublishSuccess($"Backup completado: {result.FilesTransferred} archivo(s).");
            }
            else if (result.Status == "PARTIAL")
            {
                BackupConsoleStatus = "Backup completado con omisiones.";
                _notifier.PublishError(result.Message);
            }
            else if (result.Status == "CANCELLED")
            {
                BackupConsoleStatus = "Backup cancelado por el usuario.";
                _notifier.PublishError(result.Message);
            }
            else
            {
                BackupConsoleStatus = $"Backup finalizado con error: {result.Message}";
                _notifier.PublishError(result.Message);
            }
        }
        catch (OperationCanceledException)
        {
            ClearLiveConsoleEntry();
            AppendConsoleEntry("SISTEMA", "CANCELACION SOLICITADA");
            BackupConsoleStatus = "Backup cancelado por el usuario.";
            _notifier.PublishError("Operacion cancelada por el usuario.");
        }
        finally
        {
            _backupCancellationSource?.Dispose();
            _backupCancellationSource = null;
            _isRunningBackup = false;
            OnPropertyChanged(nameof(RunBackupButtonText));
            OnPropertyChanged(nameof(IsRunningBackup));
            OnPropertyChanged(nameof(CanCloseBackupConsole));
            ((AsyncRelayCommand)RunBackupCommand).RaiseCanExecuteChanged();
        }
    }

    private void CancelBackup()
    {
        if (!_isRunningBackup || _backupCancellationSource is null || _backupCancellationSource.IsCancellationRequested)
        {
            return;
        }

        _backupCancellationSource.Cancel();
        AppendConsoleEntry("SISTEMA", "SOLICITANDO CANCELACION...");
        BackupConsoleStatus = "Cancelando backup en curso...";
        StatusMessage = "Cancelando backup en curso...";
        _statusIsError = true;
        RaiseStatusColors();
    }

    private void HandleConsoleProgress(BackupProgressEntry entry)
    {
        if (entry.Status == "EXPLORANDO DIRECTORIO")
        {
            UpdateLiveConsoleEntry(entry.FileName, entry.Status);
            return;
        }

        if (entry.Status.StartsWith("ERROR EXPLORANDO DIRECTORIO", StringComparison.Ordinal))
        {
            ClearLiveConsoleEntry();
            AppendConsoleEntry(entry.FileName, entry.Status);
            return;
        }

        AppendConsoleEntry(entry.FileName, entry.Status);
    }

    private void AppendConsoleEntry(string fileName, string status)
    {
        InsertConsoleEntry(CreateConsoleEntry(fileName, status));
    }

    private void UpdateLiveConsoleEntry(string fileName, string status)
    {
        if (_liveConsoleEntry is null)
        {
            _liveConsoleEntry = CreateConsoleEntry(fileName, status);
            BackupConsoleEntries.Add(_liveConsoleEntry);
        }
        else
        {
            _liveConsoleEntry.Timestamp = DateTime.Now;
            _liveConsoleEntry.FileName = fileName;
            _liveConsoleEntry.Status = status;
        }

        BackupConsoleStatus = $"{_liveConsoleEntry.TimestampText} - {fileName}";
    }

    private void ClearLiveConsoleEntry()
    {
        if (_liveConsoleEntry is null)
        {
            return;
        }

        BackupConsoleEntries.Remove(_liveConsoleEntry);
        _liveConsoleEntry = null;
    }

    private void InsertConsoleEntry(BackupProgressEntry entry)
    {
        if (_liveConsoleEntry is not null && BackupConsoleEntries.Remove(_liveConsoleEntry))
        {
            BackupConsoleEntries.Add(entry);
            BackupConsoleEntries.Add(_liveConsoleEntry);
        }
        else
        {
            BackupConsoleEntries.Add(entry);
        }

        BackupConsoleStatus = $"{entry.TimestampText} - {entry.Status}";
    }

    private static BackupProgressEntry CreateConsoleEntry(string fileName, string status)
    {
        return new BackupProgressEntry
        {
            Timestamp = DateTime.Now,
            FileName = fileName,
            Status = status
        };
    }

    private void PersistConnections()
    {
        _connectionStore.Save(Connections);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(AvailableDestinationConnections));
        if (SelectedSourceConnection is null)
        {
            SelectedSourceConnection = Connections.FirstOrDefault();
        }

        EnsureDistinctDestinationSelection();
    }

    private void ApplyLogFilter()
    {
        FilteredLogs.Clear();
        foreach (var log in Logs.Where(log =>
                     string.IsNullOrWhiteSpace(LogSearchText)
                     || log.Id.Contains(LogSearchText, StringComparison.OrdinalIgnoreCase)
                     || log.Message.Contains(LogSearchText, StringComparison.OrdinalIgnoreCase)
                     || log.RouteSummary.Contains(LogSearchText, StringComparison.OrdinalIgnoreCase)
                     || log.Status.Contains(LogSearchText, StringComparison.OrdinalIgnoreCase)))
        {
            FilteredLogs.Add(log);
        }
    }

    private void UpdateDashboard()
    {
        OnPropertyChanged(nameof(SuccessCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(TotalTransferredText));
    }

    private Brush GetMenuBackground(UiSection section) => CreateBrush(_selectedSection == section ? "#3949AB" : "#F8FAFF");
    private Brush GetMenuBorderBrush(UiSection section) => CreateBrush(_selectedSection == section ? "#31409A" : "#E6ECFF");
    private Brush GetMenuForeground(UiSection section) => CreateBrush(_selectedSection == section ? "#FFFFFF" : "#334155");
    private Brush GetMenuAccentBrush(UiSection section) => CreateBrush(_selectedSection == section ? "#C7D2FE" : "#00000000");
    private double GetMenuAccentWidth(UiSection section) => _selectedSection == section ? 4 : 0;
    private Brush GetMenuIconBackground(UiSection section) => CreateBrush(_selectedSection == section ? "#31409A" : "#FFFFFF");
    private Brush GetMenuIconBorderBrush(UiSection section) => CreateBrush(_selectedSection == section ? "#5C6BC0" : "#D8E1FF");

    private void RaiseMenuStyles()
    {
        OnPropertyChanged(nameof(DashboardButtonBackground));
        OnPropertyChanged(nameof(ConnectionsButtonBackground));
        OnPropertyChanged(nameof(LogsButtonBackground));
        OnPropertyChanged(nameof(DashboardButtonBorderBrush));
        OnPropertyChanged(nameof(ConnectionsButtonBorderBrush));
        OnPropertyChanged(nameof(LogsButtonBorderBrush));
        OnPropertyChanged(nameof(IsDashboardSelected));
        OnPropertyChanged(nameof(IsConnectionsSelected));
        OnPropertyChanged(nameof(IsLogsSelected));
        OnPropertyChanged(nameof(DashboardButtonForeground));
        OnPropertyChanged(nameof(ConnectionsButtonForeground));
        OnPropertyChanged(nameof(LogsButtonForeground));
        OnPropertyChanged(nameof(DashboardButtonAccentBrush));
        OnPropertyChanged(nameof(ConnectionsButtonAccentBrush));
        OnPropertyChanged(nameof(LogsButtonAccentBrush));
        OnPropertyChanged(nameof(DashboardButtonAccentWidth));
        OnPropertyChanged(nameof(ConnectionsButtonAccentWidth));
        OnPropertyChanged(nameof(LogsButtonAccentWidth));
        OnPropertyChanged(nameof(DashboardIconBackground));
        OnPropertyChanged(nameof(ConnectionsIconBackground));
        OnPropertyChanged(nameof(LogsIconBackground));
        OnPropertyChanged(nameof(DashboardIconBorderBrush));
        OnPropertyChanged(nameof(ConnectionsIconBorderBrush));
        OnPropertyChanged(nameof(LogsIconBorderBrush));
    }

    private void RaiseStatusColors()
    {
        OnPropertyChanged(nameof(StatusBannerBackground));
        OnPropertyChanged(nameof(StatusBannerBorder));
        OnPropertyChanged(nameof(StatusBannerForeground));
    }

    private void AttachEditableConnectionHandlers(ConnectionProfile connection)
    {
        connection.PropertyChanged -= OnEditableConnectionPropertyChanged;
        connection.PropertyChanged += OnEditableConnectionPropertyChanged;
    }

    private void OnEditableConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ResetTestConnectionVisualState();

        if (e.PropertyName == nameof(ConnectionProfile.Type))
        {
            if (EditableConnection.Type == ConnectionType.LocalFolder)
            {
                EditableConnection.Port = 0;
                EditableConnection.Username = string.Empty;
                EditableConnection.Password = string.Empty;
                EditableConnection.PrivateKeyPath = string.Empty;
            }

            if (EditableConnection.Type == ConnectionType.Ftp)
            {
                EditableConnection.PrivateKeyPath = string.Empty;
            }

            if (EditableConnection.Type == ConnectionType.None)
            {
                EditableConnection.Port = 0;
                EditableConnection.Username = string.Empty;
                EditableConnection.Password = string.Empty;
                EditableConnection.PrivateKeyPath = string.Empty;
                EditableConnection.BasePath = string.Empty;
                EditableConnection.Host = string.Empty;
            }

            RaiseConnectionEditorState();
        }
    }

    private void RaiseConnectionEditorState()
    {
        OnPropertyChanged(nameof(IsLocalConnectionType));
        OnPropertyChanged(nameof(IsRemoteConnectionType));
        OnPropertyChanged(nameof(IsSftpConnectionType));
    }

    private void RaiseTestConnectionVisualState()
    {
        OnPropertyChanged(nameof(TestConnectionButtonBackground));
        OnPropertyChanged(nameof(TestConnectionButtonForeground));
        OnPropertyChanged(nameof(TestConnectionStatusForeground));
    }

    private void ResetTestConnectionVisualState()
    {
        _lastTestConnectionSucceeded = null;
        TestConnectionStatusText = string.Empty;
        RaiseTestConnectionVisualState();
    }

    private void EnsureDistinctDestinationSelection()
    {
        if (SelectedSourceConnection is null)
        {
            if (SelectedDestinationConnection is null)
            {
                SelectedDestinationConnection = Connections.FirstOrDefault();
            }

            return;
        }

        if (SelectedDestinationConnection is not null && SelectedDestinationConnection.Id != SelectedSourceConnection.Id)
        {
            return;
        }

        SelectedDestinationConnection = AvailableDestinationConnections.FirstOrDefault();
    }

    private string? ValidateEditableConnectionForTest()
    {
        if (EditableConnection.Type == ConnectionType.None)
        {
            return "Debes seleccionar un tipo de conexion.";
        }

        if (string.IsNullOrWhiteSpace(EditableConnection.Name))
        {
            return "El nombre de la conexion es obligatorio.";
        }

        if (EditableConnection.Type == ConnectionType.LocalFolder)
        {
            return string.IsNullOrWhiteSpace(EditableConnection.Host)
                ? "La ruta local es obligatoria."
                : null;
        }

        if (string.IsNullOrWhiteSpace(EditableConnection.Host))
        {
            return "El host es obligatorio.";
        }

        if (EditableConnection.Port <= 0)
        {
            return "El puerto es obligatorio.";
        }

        if (EditableConnection.TimeoutSeconds <= 0)
        {
            return "El timeout debe ser mayor a 0 segundos.";
        }

        if (EditableConnection.RetryCount < 0)
        {
            return "Los reintentos no pueden ser negativos.";
        }

        if (string.IsNullOrWhiteSpace(EditableConnection.Username))
        {
            return "El usuario es obligatorio.";
        }

        if (EditableConnection.Type == ConnectionType.Sftp)
        {
            if (string.IsNullOrWhiteSpace(EditableConnection.Password) && string.IsNullOrWhiteSpace(EditableConnection.PrivateKeyPath))
            {
                return "Debes indicar password o llave privada para SFTP.";
            }

            return null;
        }

        return string.IsNullOrWhiteSpace(EditableConnection.Password)
            ? "El password es obligatorio."
            : null;
    }

    private static string NormalizeUserRoutePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Replace('\\', '/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        normalized = normalized.Trim();
        normalized = normalized.Trim('/');

        return normalized is "." ? string.Empty : normalized;
    }

    private static string BuildRoutePreview(string path)
    {
        var normalized = NormalizeUserRoutePath(path);
        return string.IsNullOrWhiteSpace(normalized) ? "/" : $"/{normalized}";
    }

    private static string BuildConnectionRootDisplay(ConnectionProfile? profile)
    {
        if (profile is null)
        {
            return "Sin conexion seleccionada.";
        }

        return profile.Type switch
        {
            ConnectionType.LocalFolder => $"Ruta raiz local: {profile.Host}",
            ConnectionType.Ftp or ConnectionType.Sftp => $"Ruta base remota: {profile.PathLabel}",
            _ => "Sin ruta base configurada."
        };
    }

    private DateTime? NormalizeFilterFromDate()
    {
        if (!FilterFromDate.HasValue)
        {
            return null;
        }

        return UseFilterTime
            ? FilterFromDate.Value
            : FilterFromDate.Value.Date;
    }

    private DateTime? NormalizeFilterToDate()
    {
        if (!FilterToDate.HasValue)
        {
            return null;
        }

        return UseFilterTime
            ? FilterToDate.Value
            : FilterToDate.Value.Date.AddDays(1).AddTicks(-1);
    }

    private static Brush CreateBrush(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;

    private async Task CheckForUpdatesAsync()
    {
        var updateInfo = await _checkUpdatesUseCase.ExecuteAsync();
        if (updateInfo != null)
        {
            _availableUpdateInfo = updateInfo;
            IsUpdateAvailable = true;
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (_availableUpdateInfo == null) return;

        IsDownloadingUpdate = true;
        UpdateDownloadProgress = 0;
        UpdateDownloadStatus = "Preparando descarga de la actualizacion...";

        bool downloaded = await _downloadUpdateUseCase.ExecuteAsync(_availableUpdateInfo, progress =>
        {
            PublishUpdateProgress(progress);
        });

        if (downloaded)
        {
            UpdateDownloadProgress = 100;
            UpdateDownloadStatus = "Descarga completa. Instalando actualizacion...";
            _installUpdateUseCase.Execute(_availableUpdateInfo);
            IsDownloadingUpdate = false;
        }
        else
        {
            UpdateDownloadStatus = "No se pudo descargar la actualizacion. Revisa tu internet e intentalo otra vez.";
            IsDownloadingUpdate = false;
            _notifier.PublishError(UpdateDownloadStatus);
        }
    }

    private void PublishUpdateProgress(int progress)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            ApplyUpdateProgress(progress);
            return;
        }

        dispatcher.Invoke(() => ApplyUpdateProgress(progress));
    }

    private void ApplyUpdateProgress(int progress)
    {
        UpdateDownloadProgress = Math.Clamp(progress, 0, 100);
        UpdateDownloadStatus = UpdateDownloadProgress switch
        {
            <= 0 => "Conectando con el servidor de actualizaciones...",
            >= 100 => "Descarga finalizada. Preparando instalacion...",
            _ => $"Descargando actualizacion... {UpdateDownloadProgress}%"
        };
    }
}
