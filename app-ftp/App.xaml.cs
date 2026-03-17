using app_ftp.Config;
using app_ftp.Presentacion.Views;
using System.Windows;
using System.Windows.Threading;

namespace app_ftp;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Services = AppServices.Create();
        RegisterGlobalExceptionHandlers();

        var mainWindow = new MainWindow
        {
            DataContext = Services.MainViewModel
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Services.ExceptionMiddleware.Handle(e.Exception, "UI thread");
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Services.ExceptionMiddleware.Handle(exception, "AppDomain");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Services.ExceptionMiddleware.Handle(e.Exception, "TaskScheduler");
        e.SetObserved();
    }
}
