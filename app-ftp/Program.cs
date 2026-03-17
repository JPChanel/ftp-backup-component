using System;
using Velopack;

namespace app_ftp;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            VelopackApp.Build()
                .SetAutoApplyOnStartup(false)
                .Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting Velopack: {ex.Message}");
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
