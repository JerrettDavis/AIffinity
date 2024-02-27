namespace AIffinity.WindowsService.Models;

public class AppSettings
{
    public int PollingIntervalMs { get; set; }
    public IEnumerable<ProcessSetting> Processes { get; set; } = null!;
}