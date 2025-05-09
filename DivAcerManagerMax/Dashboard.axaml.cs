using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace DivAcerManagerMax;

public partial class Dashboard : UserControl, INotifyPropertyChanged
{
    // Timer to refresh dynamic system metrics
    private DispatcherTimer _refreshTimer;
    private const int REFRESH_INTERVAL_MS = 2000; // 2 seconds

    // System info properties
    private string _cpuName;
    public string CpuName
    {
        get => _cpuName;
        set => SetProperty(ref _cpuName, value);
    }

    private string _gpuName;
    public string GpuName
    {
        get => _gpuName;
        set => SetProperty(ref _gpuName, value);
    }

    private string _osVersion;
    public string OsVersion
    {
        get => _osVersion;
        set => SetProperty(ref _osVersion, value);
    }

    private string _kernelVersion;
    public string KernelVersion
    {
        get => _kernelVersion;
        set => SetProperty(ref _kernelVersion, value);
    }

    private string _ramTotal;
    public string RamTotal
    {
        get => _ramTotal;
        set => SetProperty(ref _ramTotal, value);
    }

    // Dynamic metrics
    private double _cpuTemp;
    public double CpuTemp
    {
        get => _cpuTemp;
        set => SetProperty(ref _cpuTemp, value);
    }

    private double _gpuTemp;
    public double GpuTemp
    {
        get => _gpuTemp;
        set => SetProperty(ref _gpuTemp, value);
    }

    private double _cpuUsage;
    public double CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    private double _ramUsage;
    public double RamUsage
    {
        get => _ramUsage;
        set => SetProperty(ref _ramUsage, value);
    }

    private double _gpuUsage;
    public double GpuUsage
    {
        get => _gpuUsage;
        set => SetProperty(ref _gpuUsage, value);
    }

    // Add battery-related properties
    private string _batteryStatus;
    public string BatteryStatus
    {
        get => _batteryStatus;
        set => SetProperty(ref _batteryStatus, value);
    }
    
    private string _batteryPercentageString;
    public string BatteryPercentageString
    {
        get => _batteryPercentageString;
        set => SetProperty(ref _batteryPercentageString, value);
    }
    
    private string _batteryTimeRemainingString;
    public string BatteryTimeRemainingString
    {
        get => _batteryTimeRemainingString;
        set => SetProperty(ref _batteryTimeRemainingString, value);
    }

    private bool _hasBattery;
    public bool HasBattery
    {
        get => _hasBattery;
        set => SetProperty(ref _hasBattery, value);
    }

    private GpuType _gpuType = GpuType.Unknown;

    public Dashboard()
    {
        InitializeComponent();
        DataContext = this;

        // Initialize default values for battery properties
        BatteryPercentage.Text = "0";
        BatteryTimeRemaining.Text = "0";
        BatteryStatus = "Unknown";

        // Fetch static system information once at initialization
        InitializeStaticSystemInfo();

        // Setup refresh timer for dynamic metrics
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(REFRESH_INTERVAL_MS)
        };
        _refreshTimer.Tick += RefreshDynamicMetrics;
        _refreshTimer.Start();

        // Initial refresh of dynamic metrics
        RefreshDynamicMetricsAsync();
    }

    private void RefreshDynamicMetrics(object? sender, EventArgs e)
    {
        RefreshDynamicMetricsAsync();
    }

    private async void RefreshDynamicMetricsAsync()
    {
        try
        {
            var metricsData = await Task.Run(() =>
            {
                var data = new MetricsData();
                
                // Update CPU metrics
                data.CpuUsage = GetCpuUsage();
                data.CpuTemp = GetCpuTemperature();
                
                // Update RAM metrics
                data.RamUsage = GetRamUsage();
                
                // Update GPU metrics
                var gpuMetrics = GetGpuMetrics();
                data.GpuTemp = gpuMetrics.temperature;
                data.GpuUsage = gpuMetrics.usage;
                
                // Update battery metrics
                var batteryInfo = GetBatteryInfo();
                data.BatteryPercentage = batteryInfo.percentage;
                data.BatteryStatus = batteryInfo.status;
                data.BatteryTimeRemaining = batteryInfo.timeRemaining;
                
                return data;
            });

            // Update UI from UI thread
            Dispatcher.UIThread.Post(() =>
            {
                // Apply the collected metrics to UI-bound properties
                CpuUsage = metricsData.CpuUsage;
                CpuTemp = metricsData.CpuTemp;
                RamUsage = metricsData.RamUsage;
                GpuTemp = metricsData.GpuTemp;
                GpuUsage = metricsData.GpuUsage;
                BatteryPercentage.Text = metricsData.BatteryPercentage;
                BatteryStatus = metricsData.BatteryStatus;
                BatteryTimeRemaining.Text = metricsData.BatteryTimeRemaining;
            });
        }
        catch (Exception ex)
        {
            // Log exception if needed
            Console.WriteLine($"Error updating metrics: {ex.Message}");
        }
    }

    // Class to hold metrics data
    private class MetricsData
    {
        public double CpuUsage { get; set; }
        public double CpuTemp { get; set; }
        public double RamUsage { get; set; }
        public double GpuTemp { get; set; }
        public double GpuUsage { get; set; }
        public string BatteryPercentage { get; set; } = "0";
        public string BatteryStatus { get; set; } = "Unknown";
        public string BatteryTimeRemaining { get; set; } = "0";
    }

    private void InitializeStaticSystemInfo()
    {
        // Get CPU information
        CpuName = GetCpuName();
        
        // Get GPU information
        DetectGpuType();
        GpuName = GetGpuName();
        
        // Update GPU driver info on UI thread
        string gpuDriver = GetGpuDriverVersion();
        Dispatcher.UIThread.Post(() => 
        {
            GpuDriver.Text = gpuDriver;
        });
        
        // Get OS information
        OsVersion = GetOsVersion();
        KernelVersion = GetKernelVersion();
        
        // Get RAM information
        RamTotal = GetTotalRam();
        
        // Check if system has a battery
        HasBattery = CheckForBattery();
    }

    private string GetCpuName()
    {
        try
        {
            string cpuInfo = File.ReadAllText("/proc/cpuinfo");
            var modelNameMatch = Regex.Match(cpuInfo, @"model name\s+:\s+(.+)");
            if (modelNameMatch.Success)
            {
                return modelNameMatch.Groups[1].Value.Trim();
            }
            return "Unknown CPU";
        }
        catch
        {
            return "CPU Information Unavailable";
        }
    }

    private void DetectGpuType()
    {
        try
        {
            // Check for NVIDIA GPU
            if (Directory.Exists("/sys/class/drm/card0/device/driver/module/nvidia") || 
                RunCommand("lspci", "").Contains("NVIDIA"))
            {
                _gpuType = GpuType.Nvidia;
                return;
            }
            
            // Check for AMD GPU
            if (Directory.Exists("/sys/class/drm/card0/device/driver/module/amdgpu") || 
                RunCommand("lspci", "").Contains("AMD") || 
                RunCommand("lspci", "").Contains("ATI"))
            {
                _gpuType = GpuType.Amd;
                return;
            }
            
            // Default to Intel if not NVIDIA or AMD
            if (RunCommand("lspci", "").Contains("Intel"))
            {
                _gpuType = GpuType.Intel;
                return;
            }

            _gpuType = GpuType.Unknown;
        }
        catch
        {
            _gpuType = GpuType.Unknown;
        }
    }

private string GetGpuName()
{
    try
    {
        switch (_gpuType)
        {
            case GpuType.Nvidia:
                return GetNvidiaGpuName();
            case GpuType.Amd:
                return GetAmdGpuName();
            case GpuType.Intel:
                return GetIntelGpuName();
            default:
                return GetFallbackGpuName();
        }
    }
    catch
    {
        return "GPU Information Unavailable";
    }
}

private string GetNvidiaGpuName()
{
    // Try nvidia-smi first (most reliable)
    string nvidiaSmiOutput = RunCommand("nvidia-smi", "--query-gpu=name --format=csv,noheader");
    if (!string.IsNullOrWhiteSpace(nvidiaSmiOutput))
    {
        return nvidiaSmiOutput.Trim();
    }

    // Fallback to lspci if nvidia-smi fails
    string lspciOutput = RunCommand("lspci", "-vmm");
    var match = Regex.Match(lspciOutput, @"Device:\s+(.+?)(?:\s*\[|\(|$)");
    if (match.Success)
    {
        string rawName = match.Groups[1].Value.Trim();
        return Regex.Replace(rawName, @"\b(G[0-9]{2}|AD[0-9]{3}[A-Z]?)\b", "").Trim(); // Remove chip codes
    }

    return "NVIDIA GPU (Unknown Model)";
}

private string GetAmdGpuName()
{
    // Try ROCm-SMI if available
    string rocmOutput = RunCommand("rocm-smi", "--showproductname");
    if (!string.IsNullOrWhiteSpace(rocmOutput))
    {
        var match = Regex.Match(rocmOutput, @"Product Name:\s+(.+)");
        if (match.Success)
            return match.Groups[1].Value.Trim();
    }

    // Fallback to glxinfo
    string glxOutput = RunCommand("glxinfo", "-B");
    var glxMatch = Regex.Match(glxOutput, @"OpenGL renderer string:\s+(.+)");
    if (glxMatch.Success)
    {
        string renderer = glxMatch.Groups[1].Value;
        return Regex.Replace(renderer, @"(\(.*?\)|LLVM.*|DRM.*)", "").Trim(); // Clean up extra info
    }

    // Fallback to lspci
    string lspciOutput = RunCommand("lspci", "-vmm");
    var lspciMatch = Regex.Match(lspciOutput, @"Device:\s+(.+?)(?:\s*\[|\(|$)");
    if (lspciMatch.Success)
    {
        string rawName = lspciMatch.Groups[1].Value.Trim();
        return Regex.Replace(rawName, @"\b(R[0-9]{3}|GFX[0-9]{3})\b", "").Trim(); // Remove chip codes
    }

    return "AMD GPU (Unknown Model)";
}

private string GetIntelGpuName()
{
    // Try intel_gpu_top if available
    string intelOutput = RunCommand("intel_gpu_top", "-o -");
    if (!string.IsNullOrWhiteSpace(intelOutput))
    {
        var match = Regex.Match(intelOutput, @"GPU:\s+(.+)");
        if (match.Success)
            return match.Groups[1].Value.Trim();
    }

    // Fallback to lspci
    string lspciOutput = RunCommand("lspci", "-vmm");
    var lspciMatch = Regex.Match(lspciOutput, @"Device:\s+(.+?)(?:\s*\[|\(|$)");
    if (lspciMatch.Success)
    {
        string rawName = lspciMatch.Groups[1].Value.Trim();
        return Regex.Replace(rawName, @"\b(Alder Lake|Raptor Lake|Xe)\b", "").Trim(); // Remove chipset names
    }

    return "Intel Graphics (Unknown Model)";
}

private string GetFallbackGpuName()
{
    string lspciOutput = RunCommand("lspci", "-vmm");
    var match = Regex.Match(lspciOutput, @"Device:\s+(.+?)(?:\s*\[|\(|$)");
    return match.Success ? match.Groups[1].Value.Trim() : "Unknown GPU";
}

    private string GetGpuDriverVersion()
    {
        try
        {
            switch (_gpuType)
            {
                case GpuType.Nvidia:
                    string nvidiaOutput = RunCommand("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader");
                    if (!string.IsNullOrWhiteSpace(nvidiaOutput))
                    {
                        return nvidiaOutput.Trim();
                    }
                    break;
                
                case GpuType.Amd:
                    // Try to get AMD driver version
                    string amdOutput = RunCommand("glxinfo", "| grep \"OpenGL version\"");
                    var amdMatch = Regex.Match(amdOutput, @"OpenGL version.*?(\d+\.\d+\.\d+)");
                    if (amdMatch.Success)
                    {
                        return amdMatch.Groups[1].Value;
                    }
                    break;
                
                case GpuType.Intel:
                    // Try to get Intel driver version
                    string intelOutput = RunCommand("glxinfo", "| grep \"OpenGL version\"");
                    var intelMatch = Regex.Match(intelOutput, @"OpenGL version.*?(\d+\.\d+\.\d+)");
                    if (intelMatch.Success)
                    {
                        return intelMatch.Groups[1].Value;
                    }
                    break;
            }

            // Fallback to generic driver version from glxinfo
            string glxOutput = RunCommand("glxinfo", "| grep \"OpenGL version\"");
            var match = Regex.Match(glxOutput, @"OpenGL version.*?(\d+\.\d+\.\d+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "Unknown Driver";
        }
        catch
        {
            return "Driver Information Unavailable";
        }
    }

    private string GetOsVersion()
    {
        try
        {
            if (File.Exists("/etc/os-release"))
            {
                string osRelease = File.ReadAllText("/etc/os-release");
                var prettyNameMatch = Regex.Match(osRelease, @"PRETTY_NAME=""(.+?)""");
                if (prettyNameMatch.Success)
                {
                    return prettyNameMatch.Groups[1].Value;
                }
            }
            
            // Fallback
            string lsbOutput = RunCommand("lsb_release", "-d");
            var lsbMatch = Regex.Match(lsbOutput, @"Description:\s+(.+)");
            if (lsbMatch.Success)
            {
                return lsbMatch.Groups[1].Value;
            }

            return "Unknown Linux Distribution";
        }
        catch
        {
            return "OS Information Unavailable";
        }
    }

    private string GetKernelVersion()
    {
        try
        {
            string output = RunCommand("uname", "-r");
            return output.Trim();
        }
        catch
        {
            return "Kernel Information Unavailable";
        }
    }

    private string GetTotalRam()
    {
        try
        {
            string memInfo = File.ReadAllText("/proc/meminfo");
            var match = Regex.Match(memInfo, @"MemTotal:\s+(\d+) kB");
            if (match.Success)
            {
                long kbytes = long.Parse(match.Groups[1].Value);
                double gbytes = kbytes / (1024.0 * 1024.0);
                return $"{gbytes:F2} GB";
            }
            return "Unknown";
        }
        catch
        {
            return "RAM Information Unavailable";
        }
    }

    private bool CheckForBattery()
    {
        return Directory.Exists("/sys/class/power_supply") &&
               Directory.GetDirectories("/sys/class/power_supply")
                   .Any(dir => File.Exists(Path.Combine(dir, "type")) &&
                               File.ReadAllText(Path.Combine(dir, "type")).Trim() == "Battery");
    }

    private double GetCpuUsage()
    {
        try
        {
            string statBefore = File.ReadAllText("/proc/stat");
            var matchBefore = Regex.Match(statBefore, @"^cpu\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");

            if (matchBefore.Success)
            {
                long user1 = long.Parse(matchBefore.Groups[1].Value);
                long nice1 = long.Parse(matchBefore.Groups[2].Value);
                long system1 = long.Parse(matchBefore.Groups[3].Value);
                long idle1 = long.Parse(matchBefore.Groups[4].Value);

                // Small sleep to measure difference
                System.Threading.Thread.Sleep(100);

                string statAfter = File.ReadAllText("/proc/stat");
                var matchAfter = Regex.Match(statAfter, @"^cpu\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");

                if (matchAfter.Success)
                {
                    long user2 = long.Parse(matchAfter.Groups[1].Value);
                    long nice2 = long.Parse(matchAfter.Groups[2].Value);
                    long system2 = long.Parse(matchAfter.Groups[3].Value);
                    long idle2 = long.Parse(matchAfter.Groups[4].Value);

                    long totalBefore = user1 + nice1 + system1 + idle1;
                    long totalAfter = user2 + nice2 + system2 + idle2;
                    long totalDelta = totalAfter - totalBefore;
                    long idleDelta = idle2 - idle1;

                    double cpuUsage = (1.0 - (idleDelta / (double)totalDelta)) * 100.0;
                    return Math.Round(cpuUsage, 1);
                }
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetCpuTemperature()
    {
        try
        {
            // Try several common temperature sensors paths
            string[] possiblePaths = {
                "/sys/class/thermal/thermal_zone0/temp",
                "/sys/devices/platform/coretemp.0/hwmon/hwmon*/temp1_input",
                "/sys/class/hwmon/hwmon*/temp1_input"
            };

            foreach (string pathPattern in possiblePaths)
            {
                string[] files = Directory.Exists(Path.GetDirectoryName(pathPattern) ?? string.Empty)
                    ? Directory.GetFiles(Path.GetDirectoryName(pathPattern) ?? string.Empty, Path.GetFileName(pathPattern))
                    : Array.Empty<string>();

                if (files.Length > 0)
                {
                    string temperatureStr = File.ReadAllText(files[0]);
                    if (int.TryParse(temperatureStr, out int tempValue))
                    {
                        // Temperature is often reported in millidegrees C
                        double tempC = tempValue / 1000.0;
                        return Math.Round(tempC, 1);
                    }
                }
            }

            // Fallback to lm-sensors if available
            string output = RunCommand("sensors", "");
            var match = Regex.Match(output, @"Package id \d+:\s+\+?(\d+\.\d+)°C");
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double tempC))
                {
                    return Math.Round(tempC, 1);
                }
            }

            // Couldn't get temperature
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetRamUsage()
    {
        try
        {
            string memInfo = File.ReadAllText("/proc/meminfo");
            
            var totalMatch = Regex.Match(memInfo, @"MemTotal:\s+(\d+) kB");
            var availableMatch = Regex.Match(memInfo, @"MemAvailable:\s+(\d+) kB");
            
            if (totalMatch.Success && availableMatch.Success)
            {
                long totalKb = long.Parse(totalMatch.Groups[1].Value);
                long availableKb = long.Parse(availableMatch.Groups[1].Value);
                long usedKb = totalKb - availableKb;
                
                double usagePercentage = (usedKb / (double)totalKb) * 100.0;
                return Math.Round(usagePercentage, 1);
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private (double temperature, double usage) GetGpuMetrics()
    {
        try
        {
            switch (_gpuType)
            {
                case GpuType.Nvidia:
                    return GetNvidiaGpuMetrics();
                case GpuType.Amd:
                    return GetAmdGpuMetrics();
                case GpuType.Intel:
                    return GetIntelGpuMetrics();
                default:
                    return (0, 0);
            }
        }
        catch
        {
            return (0, 0);
        }
    }

    private (double temperature, double usage) GetNvidiaGpuMetrics()
    {
        try
        {
            double temp = 0;
            double usage = 0;

            // Get GPU temperature
            string tempOutput = RunCommand("nvidia-smi", "--query-gpu=temperature.gpu --format=csv,noheader");
            if (double.TryParse(tempOutput.Trim(), out temp))
            {
                // temperature is already in celsius
            }

            // Get GPU utilization
            string utilOutput = RunCommand("nvidia-smi", "--query-gpu=utilization.gpu --format=csv,noheader");
            var utilMatch = Regex.Match(utilOutput, @"(\d+)");
            if (utilMatch.Success && double.TryParse(utilMatch.Groups[1].Value, out usage))
            {
                // usage is already in percentage
            }

            return (temp, usage);
        }
        catch
        {
            return (0, 0);
        }
    }

    private (double temperature, double usage) GetAmdGpuMetrics()
    {
        try
        {
            double temp = 0;
            double usage = 0;

            // Try reading from AMD specific paths
            // For temperature
            string[] possibleTempPaths = {
                "/sys/class/drm/card0/device/hwmon/hwmon*/temp1_input",
                "/sys/class/hwmon/hwmon*/temp1_input"
            };

            foreach (string pathPattern in possibleTempPaths)
            {
                if (Directory.Exists(Path.GetDirectoryName(pathPattern) ?? string.Empty))
                {
                    string[] files = Directory.GetFiles(
                        Path.GetDirectoryName(pathPattern) ?? string.Empty, 
                        Path.GetFileName(pathPattern).Replace("*", "").Replace("?", ""),
                        SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        if (File.Exists(file))
                        {
                            string tempStr = File.ReadAllText(file);
                            if (int.TryParse(tempStr.Trim(), out int tempValue))
                            {
                                temp = tempValue / 1000.0; // Convert from milliCelsius to Celsius
                                break;
                            }
                        }
                    }
                }
            }

            // For usage, we can try to read from the GPU load file if it exists
            string[] possibleUsagePaths = {
                "/sys/class/drm/card0/device/gpu_busy_percent",
                "/sys/class/hwmon/hwmon*/device/gpu_busy_percent"
            };

            foreach (string path in possibleUsagePaths)
            {
                if (File.Exists(path))
                {
                    string usageStr = File.ReadAllText(path);
                    if (int.TryParse(usageStr.Trim(), out int usageValue))
                    {
                        usage = usageValue;
                        break;
                    }
                }
            }

            // If we couldn't get values, try radeontop
            if (temp == 0 || usage == 0)
            {
                string radeontopOutput = RunCommand("radeontop", "-d- -l1");
                var tempMatch = Regex.Match(radeontopOutput, @"Temperature:\s+(\d+)");
                var usageMatch = Regex.Match(radeontopOutput, @"GPU\s+(\d+)%");

                if (tempMatch.Success && temp == 0)
                {
                    if (double.TryParse(tempMatch.Groups[1].Value, out double tempValue))
                    {
                        temp = tempValue;
                    }
                }

                if (usageMatch.Success && usage == 0)
                {
                    if (double.TryParse(usageMatch.Groups[1].Value, out double usageValue))
                    {
                        usage = usageValue;
                    }
                }
            }

            return (temp, usage);
        }
        catch
        {
            return (0, 0);
        }
    }

    private (double temperature, double usage) GetIntelGpuMetrics()
    {
        try
        {
            double temp = 0;
            double usage = 0;

            // Intel GPU metrics are harder to get - try some common paths
            // For temperature
            string[] possibleTempPaths = {
                "/sys/class/thermal/thermal_zone*/temp",
                "/sys/class/hwmon/hwmon*/temp1_input"
            };

            // Try to find a thermal zone that might be the GPU
            foreach (string pathPattern in possibleTempPaths)
            {
                if (Directory.Exists(Path.GetDirectoryName(pathPattern) ?? string.Empty))
                {
                    string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(pathPattern) ?? string.Empty);
                    
                    foreach (string dir in dirs)
                    {
                        string typeFile = Path.Combine(dir, "type");
                        if (File.Exists(typeFile) && File.ReadAllText(typeFile).Contains("gpu"))
                        {
                            string tempFile = Path.Combine(dir, "temp");
                            if (File.Exists(tempFile))
                            {
                                string tempStr = File.ReadAllText(tempFile);
                                if (int.TryParse(tempStr.Trim(), out int tempValue))
                                {
                                    temp = tempValue / 1000.0; // Convert from milliCelsius to Celsius
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // For usage, we might be able to use the intel_gpu_top tool
            string intelOutput = RunCommand("intel_gpu_top", "-o -");
            var match = Regex.Match(intelOutput, @"Render/3D.*?(\d+)%");
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double usageValue))
                {
                    usage = usageValue;
                }
            }

            return (temp, usage);
        }
        catch
        {
            return (0, 0);
        }
    }

    private (string percentage, string status, string timeRemaining) GetBatteryInfo()
    {
        if (!HasBattery)
        {
            return ("0", "No Battery", "0");
        }

        try
        {
            string percentage = "0";
            string status = "Unknown";
            string timeRemaining = "0";

            var batteryDirs = Directory.GetDirectories("/sys/class/power_supply")
                .Where(dir => File.Exists(Path.Combine(dir, "type")) && 
                             File.ReadAllText(Path.Combine(dir, "type")).Trim() == "Battery")
                .ToList();

            if (batteryDirs.Any())
            {
                string batteryDir = batteryDirs.First();

                // Get capacity
                string capacityFile = Path.Combine(batteryDir, "capacity");
                if (File.Exists(capacityFile))
                {
                    string capacityStr = File.ReadAllText(capacityFile);
                    if (int.TryParse(capacityStr.Trim(), out int capacity))
                    {
                        percentage = capacity.ToString();
                    }
                }

                // Get status (charging/discharging)
                string statusFile = Path.Combine(batteryDir, "status");
                if (File.Exists(statusFile))
                {
                    status = File.ReadAllText(statusFile).Trim();
                }

                // Try to estimate time remaining
                string energyNowFile = Path.Combine(batteryDir, "energy_now");
                string powerNowFile = Path.Combine(batteryDir, "power_now");
                string energyFullFile = Path.Combine(batteryDir, "energy_full");
                
                // Fallbacks for different naming schemes
                if (!File.Exists(energyNowFile))
                    energyNowFile = Path.Combine(batteryDir, "charge_now");
                if (!File.Exists(powerNowFile))
                    powerNowFile = Path.Combine(batteryDir, "current_now");
                if (!File.Exists(energyFullFile))
                    energyFullFile = Path.Combine(batteryDir, "charge_full");
                
                if (File.Exists(energyNowFile) && File.Exists(powerNowFile) && File.Exists(energyFullFile))
                {
                    if (double.TryParse(File.ReadAllText(energyNowFile).Trim(), out double energyNow) &&
                        double.TryParse(File.ReadAllText(powerNowFile).Trim(), out double powerNow) &&
                        double.TryParse(File.ReadAllText(energyFullFile).Trim(), out double energyFull))
                    {
                        if (powerNow > 0)
                        {
                            if (status == "Discharging")
                            {
                                // Time remaining until empty in hours
                                timeRemaining = (energyNow / powerNow).ToString("F2");
                            }
                            else if (status == "Charging")
                            {
                                // Time remaining until full in hours
                                timeRemaining = ((energyFull - energyNow) / powerNow).ToString("F2");
                            }
                        }
                    }
                }
            }

            return (percentage, status, timeRemaining);
        }
        catch
        {
            return ("0", "Error", "0");
        }
    }

    // Helper method to run shell commands
    private string RunCommand(string command, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    // Enum for GPU types
    private enum GpuType
    {
        Unknown,
        Nvidia,
        Amd,
        Intel
    }
}