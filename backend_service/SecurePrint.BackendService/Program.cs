using Microsoft.Extensions.Hosting.WindowsServices;
using SecurePrint.BackendService;
using Serilog;
using System.IO;

var builder = Host.CreateApplicationBuilder(args);


var cfg = builder.Configuration;
var sp = cfg.GetSection("SecurePrint").Get<SecurePrintOptions>() ?? new SecurePrintOptions();
Directory.CreateDirectory(sp.GetLogDirectoryAbsolute(AppContext.BaseDirectory));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(sp.GetLogDirectoryAbsolute(AppContext.BaseDirectory), "backend-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Services.Configure<SecurePrintOptions>(cfg.GetSection("SecurePrint"));
builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<NamedPipeHub>();
builder.Services.AddHostedService<PrintInterceptorWorker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SecurePrint Backend Service";
});

var host = builder.Build();
host.Run();
