using System.ComponentModel;

namespace app_ftp.Presentacion.ViewModels;

public abstract class SectionViewModelBase : Common.ObservableObject
{
    protected SectionViewModelBase(MainViewModel parent)
    {
        Parent = parent;
        Parent.PropertyChanged += OnParentPropertyChanged;
    }

    protected MainViewModel Parent { get; }

    private void OnParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaiseAllPropertiesChanged();
    }
}
