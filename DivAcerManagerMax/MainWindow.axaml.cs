using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MsBox.Avalonia;

namespace DivAcerManagerMax;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public readonly DAMXClient _client;

    private readonly string _effectColor = "#0078D7";
    private readonly string ProjectVersion = "0.8.7";
    private Button _applyKeyboardColorsButton;
    private RadioButton _autoFanSpeedRadioButton;

    private CheckBox _backlightTimeoutCheckBox;
    private RadioButton _balancedProfileButton;
    private CheckBox _batteryLimitCheckBox;
    private CheckBox _bootAnimAndSoundCheckBox;
    private TextBlock _calibrationStatusTextBlock;
    private Slider _cpuFanSlider;
    private int _cpuFanSpeed = 50;
    private TextBlock _cpuFanTextBlock;

    private Grid _daemonErrorGrid;
    private Slider _gpuFanSlider;
    private int _gpuFanSpeed = 70;
    private TextBlock _gpuFanTextBlock;

    private bool _isCalibrating;
    private bool _isConnected;

    private bool _isManualFanControl;
    private int _keyboardBrightness = 100;
    private Slider _keyBrightnessSlider;
    private TextBlock _keyBrightnessText;
    private CheckBox _lcdOverrideCheckBox;
    private RadioButton _leftToRightRadioButton;
    private ColorPicker _lightEffectColorPicker;
    private Button _lightingEffectsApplyButton;

    private ComboBox _lightingModeComboBox;
    private int _lightingSpeed = 5;
    private Slider _lightingSpeedSlider;
    private TextBlock _lightSpeedTextBlock;

    // UI Controls
    private RadioButton _lowPowerProfileButton;

    private RadioButton _manualFanSpeedRadioButton, _maxFanSpeedRadioButton;

    private TextBlock _modelNameText,
        _laptopTypeText,
        _daemonVersionText,
        _kernelInfoText,
        _supportedFeaturesTextBlock,
        _guiVersionTextBlock;

    private RadioButton _performanceProfileButton;
    private Button _pickColor1Button;
    private Button _pickColor2Button;
    private Button _pickColor3Button;
    private Button _pickColor4Button;
    private PowerSourceDetection _powerDetection;
    private ToggleSwitch _powerToggleSwitch;
    private RadioButton _quietProfileButton;
    private RadioButton _rightToLeftRadioButton;
    private Button _setManualSpeedButton;
    public DAMXSettings _settings;

    private Button _startCalibrationButton;
    private Button _stopCalibrationButton;

    private TextBlock _thermalProfileInfoText;
    private RadioButton _turboProfileButton;
    private Button _usbChargeButton;

    private ComboBox _usbChargingComboBox;

    private Border _zone1Border;

    private ColorPicker _zone1ColorPicker, _zone2ColorPicker, _zone3ColorPicker, _zone4ColorPicker;
    private Border _zone2Border;
    private Border _zone3Border;


    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _client = new DAMXClient();

        // Delay initialization until the window is loaded
        Loaded += MainWindow_Loaded;
    }

    public bool IsCalibrating
    {
        get => _isCalibrating;
        set => SetField(ref _isCalibrating, value);
    }


    public void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Find all the UI controls
        FindControls();

        // Now that controls are found, attach event handlers
        AttachEventHandlers();

        // Initialize application logic
        InitializeAsync();
    }


    private void FindControls()
    {
        // Find all controls by name
        // Thermal Profile controls
        _lowPowerProfileButton = this.FindControl<RadioButton>("LowPowerProfileButton");
        _quietProfileButton = this.FindControl<RadioButton>("QuietProfileButton");
        _balancedProfileButton = this.FindControl<RadioButton>("BalancedProfileButton");
        _performanceProfileButton = this.FindControl<RadioButton>("PerformanceProfileButton");
        _turboProfileButton = this.FindControl<RadioButton>("TurboProfileButton");

        // Find the power status toggle switch by name
        _powerToggleSwitch = this.FindControl<ToggleSwitch>("PluggedInToggleSwitch");

        if (_powerToggleSwitch != null)
        {
            // Initialize power source detection
            _powerDetection = new PowerSourceDetection(_powerToggleSwitch);

            // Make sure UI elements are properly updated when the toggle changes
            _powerToggleSwitch.PropertyChanged += (s, args) =>
            {
                if (args.Property.Name == "IsChecked") UpdateUIBasedOnPowerSource();
            };

            // Initial UI update
            UpdateUIBasedOnPowerSource();
        }


        // Fan control controls
        _manualFanSpeedRadioButton = this.FindControl<RadioButton>("ManualFanSpeedRadioButton");
        _maxFanSpeedRadioButton = this.FindControl<RadioButton>("MaxFanSpeedRadioButton");
        _cpuFanSlider = this.FindControl<Slider>("CpuFanSlider");
        _gpuFanSlider = this.FindControl<Slider>("GpuFanSlider");
        _cpuFanTextBlock = this.FindControl<TextBlock>("CpuFanTextBlock");
        _gpuFanTextBlock = this.FindControl<TextBlock>("GpuFanTextBlock");
        _setManualSpeedButton = this.FindControl<Button>("ManualFanSpeedRadioButton");
        _autoFanSpeedRadioButton = this.FindControl<RadioButton>("AutoFanSpeedRadioButton");

        // Battery calibration controls
        _startCalibrationButton = this.FindControl<Button>("StartCalibrationButton");
        _stopCalibrationButton = this.FindControl<Button>("StopCalibrationButton");
        _calibrationStatusTextBlock = this.FindControl<TextBlock>("CalibrationStatusTextBlock");
        _batteryLimitCheckBox = this.FindControl<CheckBox>("BatteryLimitCheckBox");

        // USB charging controls
        _usbChargingComboBox = this.FindControl<ComboBox>("UsbChargingComboBox");
        _usbChargeButton = this.FindControl<Button>("UsbChargeButton");

        // Keyboard lighting zone controls
        _zone1Border = this.FindControl<Border>("Zone1Border");
        _zone2Border = this.FindControl<Border>("Zone2Border");
        _zone3Border = this.FindControl<Border>("Zone3Border");

        _zone1ColorPicker = this.FindControl<ColorPicker>("Zone1ColorPicker");
        _zone2ColorPicker = this.FindControl<ColorPicker>("Zone2ColorPicker");
        _zone3ColorPicker = this.FindControl<ColorPicker>("Zone3ColorPicker");
        _zone4ColorPicker = this.FindControl<ColorPicker>("Zone4ColorPicker");

        _keyBrightnessSlider = this.FindControl<Slider>("KeyBrightnessSlider");
        _keyBrightnessText = this.FindControl<TextBlock>("KeyBrightnessText");
        _applyKeyboardColorsButton = this.FindControl<Button>("ApplyKeyboardColorsButton");

        // Lighting effects controls
        _lightingModeComboBox = this.FindControl<ComboBox>("LightingModeComboBox");
        _lightingSpeedSlider = this.FindControl<Slider>("LightingSpeedSlider");
        _lightSpeedTextBlock = this.FindControl<TextBlock>("LightSpeedTextBlock");
        _lightEffectColorPicker = this.FindControl<ColorPicker>("LightEffectColorPicker");
        _leftToRightRadioButton = this.FindControl<RadioButton>("LeftToRightRadioButton");
        _rightToLeftRadioButton = this.FindControl<RadioButton>("RightToLeftRadioButton");
        _lightingEffectsApplyButton = this.FindControl<Button>("LightingEffectsApplyButton");

        // System settings controls
        _backlightTimeoutCheckBox = this.FindControl<CheckBox>("BacklightTimeoutCheckBox");
        _lcdOverrideCheckBox = this.FindControl<CheckBox>("LcdOverrideCheckBox");
        _bootAnimAndSoundCheckBox = this.FindControl<CheckBox>("BootAnimAndSoundCheckBox");

        //Info Texts
        _thermalProfileInfoText = this.FindControl<TextBlock>("ThermalProfileInfoText");

        //About Texts
        _modelNameText = this.FindControl<TextBlock>("ModelNameText");
        _laptopTypeText = this.FindControl<TextBlock>("LaptopTypeText");
        _supportedFeaturesTextBlock = this.FindControl<TextBlock>("SupportedFeaturesTextBlock");
        _daemonVersionText = this.FindControl<TextBlock>("DaemonVersionText");
        _guiVersionTextBlock = this.FindControl<TextBlock>("ProjectVersionText");
        _guiVersionTextBlock.Text = $"v{ProjectVersion}";
        _kernelInfoText = this.FindControl<TextBlock>("KernelInfoText");

        //Error Message
        _daemonErrorGrid = this.FindControl<Grid>("DaemonErrorGrid");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    private void AttachEventHandlers()
    {
        // Thermal Profile handlers
        _lowPowerProfileButton.IsCheckedChanged += ProfileButton_Checked;
        _quietProfileButton.IsCheckedChanged += ProfileButton_Checked;
        _balancedProfileButton.IsCheckedChanged += ProfileButton_Checked;
        _performanceProfileButton.IsCheckedChanged += ProfileButton_Checked;
        _turboProfileButton.IsCheckedChanged += ProfileButton_Checked;

        // Fan control handlers
        _manualFanSpeedRadioButton.Click += ManualFanControlRadioBox_Click;
        _cpuFanSlider.PropertyChanged += CpuFanSlider_ValueChanged;
        _gpuFanSlider.PropertyChanged += GpuFanSlider_ValueChanged;
        _autoFanSpeedRadioButton.Click += AutoFanSpeedRadioButtonClick;

        // Battery calibration handlers
        _startCalibrationButton.Click += StartCalibrationButton_Click;
        _stopCalibrationButton.Click += StopCalibrationButton_Click;
        _batteryLimitCheckBox.Click += BatteryLimitCheckBox_Click;

        // USB charging handler
        _usbChargeButton.Click += UsbChargeButton_Click;


        _keyBrightnessSlider.PropertyChanged += KeyboardBrightnessSlider_ValueChanged;
        _applyKeyboardColorsButton.Click += ApplyKeyboardColorsButton_Click;

        // Lighting effects handlers
        _lightingSpeedSlider.PropertyChanged += LightingSpeedSlider_ValueChanged;
        _lightingEffectsApplyButton.Click += LightingEffectsApplyButton_Click;

        // System settings handlers
        _backlightTimeoutCheckBox.Click += BacklightTimeoutCheckBox_Click;
        _lcdOverrideCheckBox.Click += LcdOverrideCheckBox_Click;
        _bootAnimAndSoundCheckBox.Click += BootSoundCheckBox_Click;
    }

    // Method to update UI visibility based on available features
    private void UpdateUIElementVisibility()
    {
        if (_settings == null) return;

        // Get references to feature-specific panels/containers
        var thermalProfilePanel = this.FindControl<Border>("ThermalProfilePanel");
        var fanControlPanel = this.FindControl<Border>("FanControlPanel");
        var batteryTab = this.FindControl<TabItem>("BatteryPanel");
        var usbChargingPanel = this.FindControl<Border>("UsbChargingPanel");
        var keyboardLightingTab = this.FindControl<TabItem>("KeyboardLightingPanel");
        var zoneColorControlPanel = this.FindControl<Border>("ZoneColorControlPanel");
        var keyboardEffectsPanel = this.FindControl<Border>("KeyboardEffectsPanel");
        var systemSettingsTab = this.FindControl<TabItem>("SystemSettingsPanel");

        // Show/hide features based on availability
        if (thermalProfilePanel != null)
            thermalProfilePanel.IsVisible = _client.IsFeatureAvailable("thermal_profile") || AppState.DevMode;

        if (fanControlPanel != null)
            fanControlPanel.IsVisible = _client.IsFeatureAvailable("fan_speed") || AppState.DevMode;

        if (batteryTab != null)
        {
            var hasBatteryFeatures = _client.IsFeatureAvailable("battery_calibration") ||
                                     _client.IsFeatureAvailable("battery_limiter");
            batteryTab.IsVisible = hasBatteryFeatures;

            // Further fine-tuning of battery panel elements
            var calibrationControls = this.FindControl<Border>("CalibrationControls");
            var limiterControls = this.FindControl<Border>("LimiterControls");

            if (calibrationControls != null)
                calibrationControls.IsVisible = _client.IsFeatureAvailable("battery_calibration") || AppState.DevMode;

            if (limiterControls != null)
                limiterControls.IsVisible = _client.IsFeatureAvailable("battery_limiter") || AppState.DevMode;
        }

        // Handle keyboard lighting features
        var hasKeyboardFeatures = _client.IsFeatureAvailable("backlight_timeout") ||
                                  _client.IsFeatureAvailable("per_zone_mode") ||
                                  _client.IsFeatureAvailable("four_zone_mode");

        keyboardLightingTab.IsVisible = hasKeyboardFeatures;

        if (zoneColorControlPanel != null)
            keyboardLightingTab.IsVisible = _client.IsFeatureAvailable("per_zone_mode") || AppState.DevMode;

        if (keyboardEffectsPanel != null)
            keyboardEffectsPanel.IsVisible = _client.IsFeatureAvailable("four_zone_mode") || AppState.DevMode;

        if (usbChargingPanel != null)
            usbChargingPanel.IsVisible = _client.IsFeatureAvailable("usb_charging") || AppState.DevMode;

        // Handle system settings
        if (systemSettingsTab != null)
        {
            var hasSystemSettings =
                _client.IsFeatureAvailable("lcd_override") ||
                _client.IsFeatureAvailable("boot_animation_sound");


            // Further fine-tuning of system settings elements
            var backlightControls = this.FindControl<Border>("BacklightTimeoutControls");
            var lcdControls = this.FindControl<Border>("LcdOverrideControls");
            var bootSoundControls = this.FindControl<Border>("BootSoundControls");

            if (backlightControls != null)
                backlightControls.IsVisible = _client.IsFeatureAvailable("backlight_timeout") || AppState.DevMode;

            if (lcdControls != null)
                lcdControls.IsVisible = _client.IsFeatureAvailable("lcd_override") || AppState.DevMode;

            if (bootSoundControls != null)
                bootSoundControls.IsVisible = _client.IsFeatureAvailable("boot_animation_sound") || AppState.DevMode;
        }
    }

    private void UpdateUIBasedOnPowerSource()
    {
        var powerToggleSwitch = this.FindControl<ToggleSwitch>("PluggedInToggleSwitch");

        // Update visibility of profile options based on power source
        var isPluggedIn = powerToggleSwitch?.IsChecked ?? false;

        //To Hide the thermal buttons not required
        _lowPowerProfileButton.IsVisible = _lowPowerProfileButton.IsEnabled && _powerToggleSwitch.IsChecked == false;
        Console.WriteLine("Switch Enabled: " + _lowPowerProfileButton.IsEnabled);
        Console.WriteLine("Power: " + _powerToggleSwitch.IsChecked);

        _quietProfileButton.IsVisible = _quietProfileButton.IsEnabled && isPluggedIn;
        _balancedProfileButton.IsVisible = _balancedProfileButton.IsEnabled;
        _performanceProfileButton.IsVisible = _performanceProfileButton.IsEnabled && isPluggedIn;
        _turboProfileButton.IsVisible = _turboProfileButton.IsEnabled && isPluggedIn;

        // If the currently selected profile is now invisible, select the balanced profile
        if (_balancedProfileButton != null &&
            ((_lowPowerProfileButton != null && _lowPowerProfileButton.IsChecked == true &&
              !_lowPowerProfileButton.IsVisible) ||
             (_quietProfileButton != null && _quietProfileButton.IsChecked == true && !_quietProfileButton.IsVisible) ||
             (_performanceProfileButton != null && _performanceProfileButton.IsChecked == true &&
              !_performanceProfileButton.IsVisible) ||
             (_turboProfileButton != null && _turboProfileButton.IsChecked == true && !_turboProfileButton.IsVisible)))
            _balancedProfileButton.IsChecked = true;
    }

    public async void InitializeAsync()
    {
        try
        {
            _isConnected = await _client.ConnectAsync();
            if (_isConnected)
            {
                await LoadSettingsAsync();
                _daemonErrorGrid.IsVisible = false;
            }
            else
            {
                await ShowMessageBox(
                    "Error Connecting to Daemon",
                    "Failed to connect to DAMX daemon. The Daemon may be initializing please wait.");
                _daemonErrorGrid.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error while initializing", $"Error initializing: {ex.Message}");
            _daemonErrorGrid.IsVisible = true;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            Console.WriteLine("Loading settings...");
            _settings = await _client.GetAllSettingsAsync();

            if (_settings == null)
            {
                Console.WriteLine("Settings object is null - creating default settings");
                _settings = new DAMXSettings();
            }

            Console.WriteLine("Applying settings to UI...");
            ApplySettingsToUI();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadSettingsAsync: {ex}");
            await ShowMessageBox("Error while loading settings", $"Error loading settings: {ex.Message}");

            // Create default settings if loading fails
            _settings = new DAMXSettings();
            ApplySettingsToUI();
        }
    }


    private void UpdateProfileButtons()
    {
        if (_settings == null || _settings.ThermalProfile == null)
            return;

        var isPluggedIn = _powerToggleSwitch?.IsChecked ?? false;

        // Initialize profile configuration
        var profileConfigs =
            new Dictionary<string, (RadioButton button, string description, bool showOnBattery, bool showOnAC)>
            {
                {
                    "low-power",
                    (_lowPowerProfileButton,
                        "Prioritizes energy efficiency, reduces performance to extend battery life.", true, false)
                },
                { "quiet", (_quietProfileButton, "Minimizes noise, prioritizes low power and cooling.", false, true) },
                {
                    "balanced",
                    (_balancedProfileButton, "Optimal mix of performance and noise for everyday tasks.", true, true)
                },
                {
                    "balanced-performance",
                    (_performanceProfileButton, "Maximizes speed for demanding workloads, higher fan noise", false,
                        true)
                },
                {
                    "performance",
                    (_turboProfileButton, "Unleashes peak power for extreme tasks, loudest fans.", false, true)
                }
            };

        // Reset all buttons
        foreach (var config in profileConfigs.Values)
        {
            config.button.IsVisible = false;
            config.button.IsEnabled = false;
        }

        // Configure available profiles
        foreach (var profile in _settings.ThermalProfile.Available)
        {
            var profileKey = profile.ToLower();
            if (profileConfigs.TryGetValue(profileKey, out var config))
            {
                var shouldShow = isPluggedIn ? config.showOnAC : config.showOnBattery;
                config.button.IsEnabled = true;
                config.button.IsVisible = shouldShow || AppState.DevMode;
            }
        }

        // Set active profile
        if (!string.IsNullOrEmpty(_settings.ThermalProfile.Current))
        {
            var currentProfileKey = _settings.ThermalProfile.Current.ToLower();
            if (profileConfigs.TryGetValue(currentProfileKey, out var config) &&
                config.button.IsEnabled)
            {
                config.button.IsChecked = true;
                _thermalProfileInfoText.Text = config.description;
            }
        }
    }


    private void ApplySettingsToUI()
    {
        UpdateProfileButtons();

        // Apply backlight timeout setting (with null check)
        var backlightTimeoutEnabled =
            (_settings.BacklightTimeout ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
        _backlightTimeoutCheckBox.IsChecked = backlightTimeoutEnabled;

        // Apply battery settings (with null checks)
        var batteryLimiterEnabled = (_settings.BatteryLimiter ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
        _batteryLimitCheckBox.IsChecked = batteryLimiterEnabled;

        // Apply battery calibration status (with null check)
        var isCalibrating = (_settings.BatteryCalibration ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
        IsCalibrating = isCalibrating;
        _startCalibrationButton.IsEnabled = !isCalibrating;
        _stopCalibrationButton.IsEnabled = isCalibrating;
        _calibrationStatusTextBlock.Text = isCalibrating ? "Status: Calibrating" : "Status: Not calibrating";

        // Apply boot animation sound setting (with null check)
        var bootSoundEnabled = (_settings.BootAnimationSound ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
        _bootAnimAndSoundCheckBox.IsChecked = bootSoundEnabled;

        // Apply LCD override setting (with null check)
        var lcdOverrideEnabled = (_settings.LcdOverride ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
        _lcdOverrideCheckBox.IsChecked = lcdOverrideEnabled;

        // Apply USB charging level (with null check)
        var usbChargingIndex = 0;
        switch (_settings.UsbCharging ?? "0")
        {
            case "10":
                usbChargingIndex = 1;
                break;
            case "20":
                usbChargingIndex = 2;
                break;
            case "30":
                usbChargingIndex = 3;
                break;
        }

        _usbChargingComboBox.SelectedIndex = usbChargingIndex;

        // Apply fan speeds (with null checks)
        if (int.TryParse(_settings.FanSpeed?.Cpu ?? "0", out var cpuSpeed))
        {
            _cpuFanSpeed = cpuSpeed;
            _cpuFanSlider.Value = cpuSpeed;
            if (_cpuFanSlider.Value != 0)
                _cpuFanTextBlock.Text = $"{cpuSpeed}%";
            else
                _cpuFanTextBlock.Text = "Auto";
        }

        if (int.TryParse(_settings.FanSpeed?.Gpu ?? "0", out var gpuSpeed))
        {
            _gpuFanSpeed = gpuSpeed;
            _gpuFanSlider.Value = gpuSpeed;
            _gpuFanTextBlock.Text = $"{gpuSpeed}%";

            if (_gpuFanSlider.Value == 0)
                _gpuFanTextBlock.Text = "Auto";
        }

        // Determine manual/auto mode based on current fan speeds
        var isManualMode = cpuSpeed > 0 || gpuSpeed > 0;
        _isManualFanControl = isManualMode;
        _manualFanSpeedRadioButton.IsChecked = isManualMode;
        _autoFanSpeedRadioButton.IsChecked = !isManualMode;

        // Apply keyboard lighting settings
        ApplyKeyboardSettings();


        _lightEffectColorPicker.Color = Color.Parse(_effectColor);
        _keyBrightnessText.Text = $"{_keyboardBrightness}%";
        _lightSpeedTextBlock.Text = _lightingSpeed.ToString();

        //Update System Info
        _daemonVersionText.Text = $"v{_settings.Version}";
        _laptopTypeText.Text = _settings.LaptopType;
        _supportedFeaturesTextBlock.Text =
            _supportedFeaturesTextBlock.Text = string.Join(", ", _settings.AvailableFeatures);
        ;
        _modelNameText.Text = GetLinuxLaptopModel();

        // Update UI visibility based on available features
        UpdateUIElementVisibility();
    }

    private string GetLinuxLaptopModel()
    {
        try
        {
            // Try to read the product name from sysfs (works on most Linux systems)
            if (File.Exists("/sys/class/dmi/id/product_name"))
                return File.ReadAllText("/sys/class/dmi/id/product_name").Trim();

            // Fallback: Try to use dmidecode (requires root permissions)
            var startInfo = new ProcessStartInfo
            {
                FileName = "dmidecode",
                Arguments = "-s system-product-name",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                return process.StandardOutput.ReadToEnd().Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting laptop model: {ex.Message}");
            return "Unknown";
        }
    }

    private void ApplyKeyboardSettings()
    {
        if (_settings.HasFourZoneKb)
        {
            // TODO: Parse and apply the keyboard lighting settings from
            // _settings.PerZoneMode and _settings.FourZoneMode
        }
    }

    private void EnableAllFeatures()
    {
    }

    private async Task ShowMessageBox(string title, string message)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard(title, message);

        var result = await box.ShowWindowDialogAsync(this);
    }


    public void DeveloperMode_OnClick(object? sender, RoutedEventArgs e)
    {
        EnableDevMode(true);
    }

    public void EnableDevMode(bool toEnable)
    {
        AppState.DevMode = toEnable;
        _powerToggleSwitch.IsHitTestVisible = toEnable;
        ApplySettingsToUI();
    }

    private void RetryConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InitializeAsync();
    }


    private void UpdatesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("xdg-open", "https://github.com/PXDiv/Div-Acer-Manager-Max/releases");
    }

    private void StarProject_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("xdg-open", "https://github.com/PXDiv/Div-Acer-Manager-Max/");
    }

    private void IssuePageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("xdg-open", "https://github.com/PXDiv/Div-Acer-Manager-Max/issues");
    }

    private void InternalsMangerWindow_OnClick(object? sender, RoutedEventArgs e)
    {
        var InternalsManagerWindow = new InternalsManger(this);
        InternalsManagerWindow.ShowDialog(this); // or ShowDialog(this) for modal
    }

    public static class AppState
    {
        public static bool DevMode { get; set; }
    }

    #region Event Handlers

    // Unified thermal profile handler
    private async void ProfileButton_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isConnected) return;

        var button = sender as RadioButton;
        if (button?.IsChecked != true) return;

        var profile = button.Name switch
        {
            "LowPowerProfileButton" => "low-power",
            "QuietProfileButton" => "quiet",
            "BalancedProfileButton" => "balanced",
            "PerformanceProfileButton" => "balanced-performance",
            "TurboProfileButton" => "performance",
            _ => "balanced"
        };

        // Apply the thermal profile
        await _client.SetThermalProfileAsync(profile);

        // If Quiet Mode, force Auto fan control
        if (profile == "quiet")
        {
            await _client.SetFanSpeedAsync(0, 0); // Set to auto
            _isManualFanControl = false;
            if (!AppState.DevMode)
            {
                _manualFanSpeedRadioButton.IsChecked = false;
                _autoFanSpeedRadioButton.IsChecked = true;
                _manualFanSpeedRadioButton.IsEnabled = false;
                _maxFanSpeedRadioButton.IsEnabled = false;
            }

            _thermalProfileInfoText.Text = "Minimizes noise, prioritizes low power and cooling.";
        }
        else
        {
            // Re-enable manual controls (if they were enabled before)
            _maxFanSpeedRadioButton.IsEnabled = true;
            _manualFanSpeedRadioButton.IsEnabled = true;
        }

        if (profile == "low-power")
            _thermalProfileInfoText.Text = "Prioritizes energy efficiency, reduces performance to extend battery life.";

        if (profile == "balanced")
            _thermalProfileInfoText.Text = "Optimal mix of performance and noise for everyday tasks.";

        if (profile == "balanced-performance")
            _thermalProfileInfoText.Text = "Maximizes speed for demanding workloads, higher fan noise";

        if (profile == "performance")
            _thermalProfileInfoText.Text = "Unleashes peak power for extreme tasks, loudest fans.";

        await Task.Delay(1000);
        LoadSettingsAsync();
    }

    // Fan Control Handlers
    private void ManualFanControlRadioBox_Click(object sender, RoutedEventArgs e)
    {
    }

    private void CpuFanSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            // Properly convert double to int
            _cpuFanSpeed = Convert.ToInt32(e.NewValue);
            _cpuFanTextBlock.Text = $"{_cpuFanSpeed}%";
            if (_cpuFanSlider.Value == 0)
                _cpuFanTextBlock.Text = "Auto";
        }
    }

    private void GpuFanSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            // Properly convert double to int
            _gpuFanSpeed = Convert.ToInt32(e.NewValue);
            _gpuFanTextBlock.Text = $"{_gpuFanSpeed}%";
            if (_gpuFanSlider.Value == 0)
                _gpuFanTextBlock.Text = "Auto";
        }
    }

    private async void SetManualSpeedButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isConnected) await _client.SetFanSpeedAsync(_cpuFanSpeed, _gpuFanSpeed);
    }

    private async void AutoFanSpeedRadioButtonClick(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetFanSpeedAsync(0, 0);

            _manualFanSpeedRadioButton.IsChecked = false;
            _isManualFanControl = false;

            // Reset to default values based on current profile
            await LoadSettingsAsync(); // Reload settings to get default values
        }
    }

    private async void MaxFanSpeedRadioButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isConnected) await _client.SetFanSpeedAsync(100, 100);
    }

    // Battery Calibration Handlers
    private async void StartCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetBatteryCalibrationAsync(true);
            _startCalibrationButton.IsEnabled = false;
            _stopCalibrationButton.IsEnabled = true;
            _calibrationStatusTextBlock.Text = "Status: Calibrating";
        }
    }

    private async void StopCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetBatteryCalibrationAsync(false);
            _startCalibrationButton.IsEnabled = true;
            _stopCalibrationButton.IsEnabled = false;
            _calibrationStatusTextBlock.Text = "Status: Not calibrating";
        }
    }

    // Battery Limiter Handler
    private async void BatteryLimitCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            var checkBox = sender as CheckBox;
            var isEnabled = checkBox?.IsChecked ?? false;
            await _client.SetBatteryLimiterAsync(isEnabled);
        }
    }

    // USB Charging Handler
    private async void UsbChargeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            var selectedIndex = _usbChargingComboBox.SelectedIndex;
            var level = 0;

            switch (selectedIndex)
            {
                case 1:
                    level = 10;
                    break;
                case 2:
                    level = 20;
                    break;
                case 3:
                    level = 30;
                    break;
            }

            await _client.SetUsbChargingAsync(level);
        }
    }

    private void KeyboardBrightnessSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            // Properly convert double to int
            _keyboardBrightness = Convert.ToInt32(e.NewValue);
            _keyBrightnessText.Text = $"{_keyboardBrightness}%";
        }
    }

    private async void ApplyKeyboardColorsButton_Click(object sender, RoutedEventArgs e)
    {
        /*if (_isConnected && _settings.HasFourZoneKb)
        {
            await _client.SetPerZoneModeAsync(
                _zone1Color,
                _zone2Color,
                _zone3Color,
                _zone4Color,
                _keyboardBrightness
            );
        }*/

        Console.WriteLine("Zone1:" + _zone1ColorPicker.Color.ToString().Substring(3) + " Zone2:" +
                          _zone2ColorPicker.Color.ToString().Substring(3) + " Zone3:" +
                          _zone3ColorPicker.Color.ToString().Substring(3) + " Zone4:" +
                          _zone4ColorPicker.Color.ToString().Substring(3) +
                          " Brightness: " + _keyboardBrightness);

        if (_isConnected && _settings.HasFourZoneKb)
            await _client.SetPerZoneModeAsync(
                _zone1ColorPicker.Color.ToString().Substring(3),
                _zone2ColorPicker.Color.ToString().Substring(3),
                _zone3ColorPicker.Color.ToString().Substring(3),
                _zone4ColorPicker.Color.ToString().Substring(3),
                _keyboardBrightness
            );
    }

    // Lighting Effects Handlers
    private void LightingSpeedSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            // Properly convert double to int
            _lightingSpeed = Convert.ToInt32(e.NewValue);
            _lightSpeedTextBlock.Text = _lightingSpeed.ToString();
        }
    }

    private async void LightingEffectsApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if ((_isConnected && _settings.HasFourZoneKb) || AppState.DevMode)
        {
            var mode = _lightingModeComboBox.SelectedIndex;
            var direction = _leftToRightRadioButton.IsChecked == true ? 1 : 2;

            // Parse the hex color
            var color = _lightEffectColorPicker.Color;

            Console.WriteLine(_lightEffectColorPicker.Color +
                              " Color Sent: R:" + color.R +
                              " G:" + color.G +
                              " B:" + color.B);

            await _client.SetFourZoneModeAsync(
                mode,
                _lightingSpeed,
                _keyboardBrightness,
                direction,
                color.R,
                color.G,
                color.B
            );
        }
    }


    // Backlight Timeout Handler
    private async void BacklightTimeoutCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            var checkBox = sender as CheckBox;
            var isEnabled = checkBox?.IsChecked ?? false;
            await _client.SetBacklightTimeoutAsync(isEnabled);
        }
    }

    // LCD Override Handler
    private async void LcdOverrideCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            var checkBox = sender as CheckBox;
            var isEnabled = checkBox?.IsChecked ?? false;
            await _client.SetLcdOverrideAsync(isEnabled);
        }
    }

    // Boot Sound Handler
    private async void BootSoundCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            var checkBox = sender as CheckBox;
            var isEnabled = checkBox?.IsChecked ?? false;
            await _client.SetBootAnimationSoundAsync(isEnabled);
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}