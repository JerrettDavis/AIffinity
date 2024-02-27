using AIffinity.WindowsService;
using AIffinity.WindowsService.Models;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;


// If the service is being installed or uninstalled, handle it and return
if (await args.HandleInstallation())
{
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddWindowsService(options => { options.ServiceName = "AIffinity.WindowsServices"; });


if (OperatingSystem.IsWindows())
{
    builder.Services.AddHostedService<ProcessesAffinityMonitor>();

    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);
}

var host = builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while running the service");
}