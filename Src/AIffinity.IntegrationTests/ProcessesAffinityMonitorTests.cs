using System.Diagnostics;
using AIffinity.WindowsService;
using AIffinity.WindowsService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AIffinity.IntegrationTests;

public class ProcessesAffinityMonitorTests
{
    [Fact]
    public async Task Worker_WhenRun_ShouldLogHeartbeat()
    {
        // Arrange
        var logger = new Mock<ILogger<ProcessesAffinityMonitor>>();
        var monitor = new Mock<IOptionsMonitor<AppSettings>>();
        var settings = new AppSettings
        {
            Processes = new List<ProcessSetting>
            {
                new()
                {
                    Name = "regedit",
                    Path = @"C:\Windows\regedit.exe",
                    Affinity = "0x1"
                }
            }
        };
        monitor.Setup(x => x.CurrentValue).Returns(settings);

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(logger.Object)
            .AddSingleton(monitor.Object)
            .AddSingleton<ProcessesAffinityMonitor>()
            .AddHostedService<ProcessesAffinityMonitor>();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetRequiredService<ProcessesAffinityMonitor>();

        // Act
        await hostedService.StartAsync(CancellationToken.None);
        await Task.Delay(2000); // Wait for the service to start
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processes Affinity Monitor running")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!));
    }
    
    [Fact]
    public async Task Worker_WhenRun_ShouldChangerRegeditAffinity()
    {
        if (!OperatingSystem.IsWindows())
            return;
        // Arrange
        var logger = new Mock<ILogger<ProcessesAffinityMonitor>>();
        var monitor = new Mock<IOptionsMonitor<AppSettings>>();
        var settings = new AppSettings
        {
            Processes = new List<ProcessSetting>
            {
                new()
                {
                    Name = "regedit",
                    Path = @"C:\Windows\regedit.exe",
                    Affinity = "0x1"
                }
            }
        };
        monitor.Setup(x => x.CurrentValue).Returns(settings);

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(logger.Object)
            .AddSingleton(monitor.Object)
            .AddSingleton<ProcessesAffinityMonitor>()
            .AddHostedService<ProcessesAffinityMonitor>();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetRequiredService<ProcessesAffinityMonitor>();

        // Start a new regedit process
        var regeditProcess = new Process
        {
            StartInfo =
            {
                FileName = @"C:\Windows\regedit.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        regeditProcess.Start();
        
        Assert.NotNull(regeditProcess);

        // Get the initial processor affinity of the regedit process
        var initialAffinity = regeditProcess.ProcessorAffinity.ToInt64();

        // Act
        await hostedService.StartAsync(CancellationToken.None);
        await Task.Delay(2000); // Wait for the service to start and change the processor affinity

        // Refresh the regedit process to get the updated processor affinity
        regeditProcess.Refresh();
        var updatedAffinity = regeditProcess.ProcessorAffinity.ToInt64();

        // Stop the regedit process and the service
        regeditProcess.Kill();
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        Assert.NotEqual(initialAffinity, updatedAffinity);
    }
    
}