using System.Diagnostics;

namespace AIffinity.WindowsService;

public static class InstallationExtensions
{
    /// <summary>
    /// Handles the installation of the service
    /// </summary>
    /// <param name="args">The command line arguments</param>
    /// <returns> True if the installation was handled, false otherwise</returns>
    public static async Task<bool> HandleInstallation(this string[] args)
    {
        if (args.Length > 0)
        {
            var install = args.Contains("install");
            var uninstall = args.Contains("uninstall");
            
            if (install)
                await InstallService();
            else if (uninstall)
                await UninstallService();
            else
                Console.WriteLine("Invalid argument");

            return true;
        }
        
        return false;
    }
    
    private static async Task InstallService()
    {
        // Path to the executable
        var exePath = System.AppContext.BaseDirectory + "AIffinity.WindowsService.exe";
        // Install the service using sc command
        var processInfo = new ProcessStartInfo("sc", $"create AIffinity.WindowsService binPath= \"{exePath}\" start= auto");
        var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            Console.WriteLine("Service installed successfully.");
            
            // Start the service
            processInfo = new ProcessStartInfo("sc", "start AIffinity.WindowsService");
            process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                Console.WriteLine("Service started successfully.");
            } else
            {
                Console.WriteLine("Failed to start service.");
            }
        } else
        {
            Console.WriteLine("Failed to install service.");
        }
    }
    
    private static async Task UninstallService()
    {
        // Stop the service using sc command
        var processInfo = new ProcessStartInfo("sc", "stop AIffinity.WindowsService");
        var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            Console.WriteLine("Service stopped successfully.");
        } else
        {
            Console.WriteLine("Failed to stop service.");
        }
        
        // Uninstall the service using sc command
        processInfo = new ProcessStartInfo("sc", "delete AIffinity.WindowsService");
        process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            Console.WriteLine("Service uninstalled successfully.");
        } else
        {
            Console.WriteLine("Failed to uninstall service.");
        }
    }
}