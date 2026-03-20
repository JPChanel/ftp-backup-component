using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace app_ftp.Services.Models;

public class BackupProgressEntry : INotifyPropertyChanged
{
    private DateTime _timestamp;
    private string _fileName = string.Empty;
    private string _status = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (_timestamp == value)
            {
                return;
            }

            _timestamp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimestampText));
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (string.Equals(_fileName, value, StringComparison.Ordinal))
            {
                return;
            }

            _fileName = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (string.Equals(_status, value, StringComparison.Ordinal))
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public string TimestampText => Timestamp.ToString("HH:mm:ss");

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
