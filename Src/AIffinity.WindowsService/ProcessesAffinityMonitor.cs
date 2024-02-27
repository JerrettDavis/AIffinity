using System.Diagnostics;
using AIffinity.WindowsService.Models;
using Microsoft.Extensions.Options;

namespace AIffinity.WindowsService;

#pragma warning disable CA1416

public class ProcessesAffinityMonitor : BackgroundService
{
    private readonly ILogger<ProcessesAffinityMonitor> _logger;
    private Dictionary<string, ProcessSetting> _processesDictionary;
    private IEnumerable<ProcessSetting> _processes;
    private DateTime _lastHeartbeat = DateTime.MinValue;

    public ProcessesAffinityMonitor(
        ILogger<ProcessesAffinityMonitor> logger,
        IOptionsMonitor<AppSettings> monitor)
    {
        _logger = logger;
        
        var settings = monitor.CurrentValue;

        _processes = settings.Processes;
        _processesDictionary = settings.Processes.ToDictionary(x => x.Name, x => x);

        monitor.OnChange(s =>
        {
            _logger.LogInformation("Settings changed");
            _processesDictionary = s.Processes.ToDictionary(x => x.Name, x => x);
            _processes = s.Processes.ToHashSet();
        });
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WorkerHeartbeat();

            var processes = Process.GetProcesses()
                .AsParallel()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .Where(p => MatchesMonitoredProcess(_processes, p));
            
            await Parallel.ForEachAsync(
                processes, 
                stoppingToken, 
                async (process,token) => await SetAffinity(process, token));

            await Task.Delay(1000, stoppingToken);
        }
    }
    
    private void WorkerHeartbeat()
    {
        // Only log once an hour
        if (DateTime.Now - _lastHeartbeat < TimeSpan.FromHours(1)) return;
        
        _lastHeartbeat = DateTime.Now;
        _logger.LogInformation("Processes Affinity Monitor running at: {time}", DateTimeOffset.Now);
    }

    private bool MatchesMonitoredProcess(
        IEnumerable<ProcessSetting> processSettings,
        Process p) =>
        processSettings
            .AsParallel()
            .Any(x =>
            {
                try
                {
                    if (!p.ProcessName.Equals(x.Name,
                            StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (p.MainModule?.FileName.Equals(x.Path,
                            StringComparison.OrdinalIgnoreCase) == false)
                        return false;

                    var affinityMask = p.ProcessorAffinity.ToInt64();
                    var configuredAffinityMask = Convert.ToInt64(x.Affinity, 16);

                    return affinityMask != configuredAffinityMask;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error checking process {ProcessName}", p.ProcessName);

                    return false;
                }
            });

    private Task SetAffinity(Process process, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Matching process: '{ProcessName}' with Affinity: '{Affinity}' and PID: '{PID}'",
            process.ProcessName,
            process.ProcessorAffinity.ToInt64(),
            process.Id);

        // Set the processor affinity
        var targetAffinity = _processesDictionary[process.ProcessName].Affinity;
        var newAffinity = Convert.ToInt64(targetAffinity, 16);
        
        return Task.Run(() =>
        {
            process.ProcessorAffinity = new IntPtr(newAffinity);

            _logger.LogInformation(
                "Set Process '{ProcessName}' Affinity: '{Affinity}' for PID: '{PID}'",
                process.ProcessName,
                newAffinity,
                process.Id);
        }, cancellationToken);
    }
}
#pragma warning restore CA1416