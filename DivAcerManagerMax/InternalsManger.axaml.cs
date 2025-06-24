using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;

namespace DivAcerManagerMax;

public partial class InternalsManger : Window
{
    private const string logPath = "/var/log/DAMX_Daemon_Log.log";
    private readonly MainWindow _mainWindow;

    public InternalsManger(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        InitializeUiComponents();
    }

    public void InitializeUiComponents()
    {
        DevModeToggleSwitch.IsChecked = MainWindow.AppState.DevMode;
    }

    private void DevModeSwitch_OnClick(object? sender, RoutedEventArgs e)
    {
        _mainWindow.EnableDevMode(DevModeToggleSwitch.IsChecked == true);
    }

    public void ReinitializeDamxGUI()
    {
        _mainWindow.InitializeAsync();
    }

    private void DaemonLogsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("xdg-open", logPath);
    }


    private async void RestartSuiteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("restart_drivers_and_daemon");
        Console.WriteLine("Restart suite command sent");
        await Task.Delay(1000);

        ReinitializeDamxGUI();

        ShowMessagebox("Restarting Suite", "Restarting Suite and refreshing GUI, please wait");
    }


    private async void ForcePredatorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_predator_model");
        Console.WriteLine("Force Predator Model Command Sent");
        await Task.Delay(1000);
        ReinitializeDamxGUI();
        ShowMessagebox("Forcing Predator Model",
            "Restarting Drivers with predator_v4 parameter with daemon and refreshing GUI, please wait");
    }

    private async void ForceNitroButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_nitro_model");
        Console.WriteLine("Force Nitro Model Command Sent");
        await Task.Delay(1000);
        ShowMessagebox("Forcing Nitro Model",
            "Restarting Drivers with nitro_v4 parameter with daemon and refreshing GUI, please wait");
        ReinitializeDamxGUI();
    }


    private async void RestartDaemon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("restart_daemon");
        Console.WriteLine("restart_daemon Command Sent");

        await Task.Delay(1000);
        ReinitializeDamxGUI();

        ShowMessagebox("Restarting Daemon",
            "Restarting Daemon refreshing GUI, please wait");
    }

    private async void ShowMessagebox(string title, string message)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard(title, message);


        var result = await box.ShowWindowDialogAsync(this);
    }

    private async void ForceEnableAll_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_enable_all");
        Console.WriteLine("Force Enable All Features Command Sent");
        await Task.Delay(1000);
        ShowMessagebox("Forcing All Features",
            "Initializing Drivers with enable_all parameter. Restarting daemon and refreshing GUI, please wait");
        ReinitializeDamxGUI();
    }
}