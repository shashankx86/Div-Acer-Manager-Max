using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;

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

    public void ReinitializeDamxGUI()
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

        ReinitializeDamxGUI();

        ShowMessagebox("Restarting Suite", "Restarting Suite and refreshing GUI, please wait");
    }


    private async void ForcePredatorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_predator_model");
        Console.WriteLine("Force Predator Model Command Sent");
        await Task.Delay(1000);
        ReinitializeDamxGUI();
    }

    private async void ForceNitroButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("force_nitro_model");
        Console.WriteLine("Force Nitro Model Command Sent");
        await Task.Delay(1000);

        ReinitializeDamxGUI();
    }


    private void RestartDaemon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainWindow._client.IsConnected) _mainWindow._client.SendCommandAsync("restart_daemon");
    }

    private async void ShowMessagebox(string title, string message)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard(title, message);

        var result = await box.ShowAsync();
    }
}