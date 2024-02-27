# AIffinity

Monitors running processes and sets processor affinity for configured processes 
on a per-core basis in Windows.

## Motivation

I've had issues with certain applications crashing under various intensive workloads.
After some tinkering, I started to suspect that these crashes might all have something
to do with the way Windows schedules threads on multi-core systems, and more specifically
the way it handles the processor affinity of threads across P-Cores and E-Cores. After ruling 
out other potential causes, I decided to write a small utility to monitor running processes
and set their processor affinity to a specific configuration on the fly.


## Prerequisites
1. Windows 10 or later
2. Dotnet 8.0 or later
3. The courage to use command line tools

## Installation
1. Clone the repository
2. Open a terminal and navigate to the root of the repository
3. Run `dotnet build`
4. Run `dotnet publish Src/AIffinity.WindowsService -c Release -r win-x64 -o Publish`
5. Run `cd Publish`
6. Run `AIffinity.WindowsService.exe install`
7. The service should now be installed and running. You can check the status of the service by checking the Windows Services application.

## Configuration

The configuration file is located at `<Publish>/appsettings.json`. The configuration file is a JSON file with the following structure:

```json
{
  "Logging": {
    // Logging configuration removed for brevity 
  },
  "PollingIntervalMs": 1000,
  "Processes": [
    {
      "Name": "regedit",
      "Path": "C:\\Windows\\regedit.exe",
      "Affinity": "0xFFFF"
    }
  ]
}
```
- The `PollingIntervalMs` property is the interval at which the service will poll the system for running processes. 
- The `Processes` property is an array of objects that represent the processes that the service will monitor. 
- The `Name` property is the name of the process. 
- The `Path` property is the full path to the executable
- The `Affinity` property is a hexadecimal string that represents the affinity mask for the process. 
The affinity mask is a 16-bit mask that represents the processor affinity for the process. 
The least significant bit represents the first core, and the most significant bit represents the last core. 
For example, the affinity mask `0xFFFF` represents the first 16 cores of the system, while the affinity mask `0x1` represents the first core of the system.

The application is configured to monitor the appsettings.json file for changes. 
If the file is modified, the service will reload the configuration automatically.
