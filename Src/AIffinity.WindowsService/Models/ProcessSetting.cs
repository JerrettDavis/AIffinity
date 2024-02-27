namespace AIffinity.WindowsService.Models;

public class ProcessSetting
{
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string Affinity { get; set; } = null!;
}