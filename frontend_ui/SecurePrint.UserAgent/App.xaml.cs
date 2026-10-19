using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

namespace SecurePrint.UserAgent;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Keep app alive
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Hidden window (prevents app from exiting)
        var hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Visibility = Visibility.Hidden
        };
        hiddenWindow.Show();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<UserAgentOptions>(
                    ctx.Configuration.GetSection("SecurePrintUserAgent"));

                services.AddSingleton<TrayIcon>();
                services.AddSingleton<PipeClient>();
                services.AddSingleton<JobPopupCoordinator>();

                // Worker that shows popup
                services.AddHostedService<UserAgentWorker>();
            })
            .Build();

        await _host.StartAsync();

        // Debug output (safe, no ILogger conflict)
        System.Console.WriteLine("UserAgent started successfully");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
                await _host.StopAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}