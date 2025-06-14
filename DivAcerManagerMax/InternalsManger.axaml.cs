using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DivAcerManagerMax;

public partial class InternalsManger : Window
{
    private readonly MainWindow _mainWindow;

    public InternalsManger(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void DevModeSwitch_OnClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.EnableDevMode(DevModeToggleSwitch.IsChecked == true);
    }

    public void ReinitializeGUI()
    {
        _mainWindow.InitializeAsync();
    }

    private void DaemonLogsButton_OnClick(object? sender, RoutedEventArgs e)
    {
    }

    private async void RestartSuiteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("restart_drivers_and_daemon");
        Console.WriteLine("Restart suite command sent");
        await Task.Delay(1000);

        ReinitializeGUI();
    }


    private async void ForcePredatorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_predator_model");
        Console.WriteLine("Force Predator Model Command Sent");
        await Task.Delay(1000);
        ReinitializeGUI();
    }

    private async void ForceNitroButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_nitro_model");
        Console.WriteLine("Force Nitro Model Command Sent");
        await Task.Delay(1000);
        ReinitializeGUI();
    }

    private async void UnloadLinuwuDriver()
    {
        Console.WriteLine("Unloading driver");
        RunCommand("pkexec", "rmmod linuwu-sense");
        Console.WriteLine("Unload command sent");
    }

    private async void LoadLinuwuDriver()
    {
        Console.WriteLine("Loading driver");
        RunCommand("pkexec", "modprobe linuwu-sense");
        Console.WriteLine("Load command sent");
    }

    private async void LoadLinuwuDriver(string arguments)
    {
        Console.WriteLine($"Loading driver with params: {arguments}");
        RunCommand("pkexec", $"modprobe linuwu-sense {arguments}");
        Console.WriteLine("Load with params command sent");
    }

    private async void RestartDaemon()
    {
        Console.WriteLine("Restarting daemon");
        RunCommand("pkexec", "systemctl restart damx-daemon");
        Console.WriteLine("Restart daemon command sent");
    }

    private void RunCommand(string command, string args)
    {
        try
        {
            Console.WriteLine($"Running: {command} {args}");

            // Launch through x-terminal-emulator or gnome-terminal
            var terminalCommand =
                $"x-terminal-emulator -e '{command} {args}' || gnome-terminal -- {command} {args} || xterm -e '{command} {args}'";

            Process.Start(new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = $"-c \"{terminalCommand}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            });

            Console.WriteLine("Command started in terminal");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}