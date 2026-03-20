using app_ftp.Presentacion.Common;
using app_ftp.Presentacion.Shared.Controls.DataTable;
using app_ftp.Services.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace app_ftp.Presentacion.ViewModels;

public class LogsViewModel : SectionViewModelBase
{
    private bool _isLogDetailOpen;
    private string _detailSearchText = string.Empty;
    private string _visibleExecutionDetails = "Sin detalle disponible.";
    private const int MaxVisibleDetailLines = 5000;

    public LogsViewModel(MainViewModel parent) : base(parent)
    {
        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new ColDef<BackupLogEntry> { Key=x=> x.Id, Header = "ID", Width = "110", Command = VerDetalleCommand, Priority = 1 },
            new ColDef<BackupLogEntry> { Key=x=> x.Operation, Header = "Tarea", Width = "110", Priority = 2 },
            new ColDef<BackupLogEntry> { Key=x=> x.RouteSummary, Header = "Origen / Destino", Width = "180", Priority = 1 },
            new ColDef<BackupLogEntry> { Key=x=> x.FilesTransferred, Header = "Archivos", Width = "120", Type = DataTableColumnType.Number, Align = "Center", Priority = 1 },
            new ColDef<BackupLogEntry> { Key=x=> x.SizeSummary, Header = "Tamano", Width = "110", Priority = 2 },
            new ColDef<BackupLogEntry> { Key=x=> x.TimestampText, Header = "Fecha", Width = "150", Format = "dd/MM/yyyy HH:mm", Type = DataTableColumnType.Date, Priority = 2 },
            new ColDef<BackupLogEntry> { Key=x=> x.Status, Header = "Estado", Variant = CellDisplayVariant.Outline, ColorSelector = x => x.Status == "SUCCESS" ? "#66bb6a" : "#ffa726", Width = "110", Priority = 1 },
            new ColDef<BackupLogEntry> { Key=x=> x.Message, Header = "Resumen", Width = "*", Priority = 1 }
        };

        TableViewModel = new DataTableViewModel<BackupLogEntry>();
        Parent.FilteredLogs.CollectionChanged += (_, _) => RefreshTableData();
        RefreshTableData();
        UpdateVisibleExecutionDetails();
    }

    public int SuccessCount => Parent.SuccessCount;
    public int FailedCount => Parent.FailedCount;
    public string TotalTransferredText => Parent.TotalTransferredText;
    public ObservableCollection<BackupLogEntry> FilteredLogs => Parent.FilteredLogs;
    public string LogSearchText { get => Parent.LogSearchText; set => Parent.LogSearchText = value; }

    public BackupLogEntry? SelectedLog
    {
        get => Parent.SelectedLog;
        set
        {
            Parent.SelectedLog = value;
            OnPropertyChanged(nameof(SelectedLog));
            OnPropertyChanged(nameof(CanOpenLogFile));
            UpdateVisibleExecutionDetails();
        }
    }

    public DataTableViewModel<BackupLogEntry> TableViewModel { get; }
    public ObservableCollection<DataTableColumn> TableColumns { get; }

    public bool IsLogDetailOpen
    {
        get => _isLogDetailOpen;
        set => SetProperty(ref _isLogDetailOpen, value);
    }

    public string DetailSearchText
    {
        get => _detailSearchText;
        set
        {
            if (SetProperty(ref _detailSearchText, value))
            {
                UpdateVisibleExecutionDetails();
            }
        }
    }

    public string VisibleExecutionDetails
    {
        get => _visibleExecutionDetails;
        private set => SetProperty(ref _visibleExecutionDetails, value);
    }

    public bool CanOpenLogFile => !string.IsNullOrWhiteSpace(SelectedLog?.ExecutionDetailsFullPath);

    public ICommand VerDetalleCommand => new RelayCommand(OpenDetail);
    public ICommand CloseLogDetailCommand => new RelayCommand(CloseLogDetail);
    public ICommand OpenLogFileLocationCommand => new RelayCommand(OpenLogFileLocation, _ => CanOpenLogFile);

    private void RefreshTableData()
    {
        TableViewModel.SetData(FilteredLogs);
    }

    private void CloseLogDetail()
    {
        IsLogDetailOpen = false;
        DetailSearchText = string.Empty;
        SelectedLog = null;
        TableViewModel.SelectedItem = null;
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is BackupLogEntry log)
        {
            SelectedLog = log;
        }

        if (SelectedLog is null)
        {
            return;
        }

        EnsureExecutionDetailsLoaded(SelectedLog);
        DetailSearchText = string.Empty;
        UpdateVisibleExecutionDetails();
        IsLogDetailOpen = true;
    }

    private void OpenLogFileLocation(object? _)
    {
        var filePath = SelectedLog?.ExecutionDetailsFullPath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (!File.Exists(filePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true
        });
    }

    private void UpdateVisibleExecutionDetails()
    {
        if (SelectedLog is not null)
        {
            EnsureExecutionDetailsLoaded(SelectedLog);
        }

        var source = SelectedLog?.ExecutionDetails;
        if (string.IsNullOrWhiteSpace(source))
        {
            VisibleExecutionDetails = "Sin detalle disponible.";
            return;
        }

        var term = DetailSearchText?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            VisibleExecutionDetails = source;
            return;
        }

        var lines = source
            .Split(Environment.NewLine)
            .Where(line => line.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        VisibleExecutionDetails = lines.Count == 0
            ? $"No se encontraron coincidencias para: {term}"
            : string.Join(Environment.NewLine, lines);
    }

    private static void EnsureExecutionDetailsLoaded(BackupLogEntry log)
    {
        if (!string.IsNullOrWhiteSpace(log.ExecutionDetails))
        {
            return;
        }

        var filePath = log.ExecutionDetailsFullPath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        var builder = new StringBuilder();
        var visibleLines = 0;

        foreach (var line in File.ReadLines(filePath))
        {
            if (!ShouldKeepDetailLine(line))
            {
                continue;
            }

            builder.AppendLine(line);
            visibleLines++;

            if (visibleLines >= MaxVisibleDetailLines)
            {
                builder.AppendLine($"... detalle truncado a {MaxVisibleDetailLines} lineas para mantener rendimiento.");
                break;
            }
        }

        log.ExecutionDetails = builder.ToString().Trim();
    }

    private static bool ShouldKeepDetailLine(string line)
    {
        return line.Contains("| COPIADO |", StringComparison.Ordinal)
            || line.Contains("| OMITIDO |", StringComparison.Ordinal)
            || line.Contains("| ORIGEN ELIMINADO |", StringComparison.Ordinal)
            || line.Contains("| ERROR |", StringComparison.Ordinal)
            || line.Contains("| ERROR LEYENDO ARCHIVO:", StringComparison.Ordinal)
            || line.Contains("| ERROR EXPLORANDO DIRECTORIO:", StringComparison.Ordinal);
    }
}
