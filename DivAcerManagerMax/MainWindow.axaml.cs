using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DivAcerManagerMax;

namespace DivAcerManagerMax
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DAMXClient _client;
        private DAMXSettings _settings;
        private bool _isManualFanControl;
        private int _cpuFanSpeed = 50;
        private int _gpuFanSpeed = 70;
        private int _lightingSpeed = 5;
        private bool _isConnected = false;

        // Color properties for the four keyboard zones
        private string _zone1Color = "#4287f5";
        private string _zone2Color = "#ff5733";
        private string _zone3Color = "#33ff57";
        private string _zone4Color = "#ff33a6";
        private string _effectColor = "#0078D7";
        private int _keyboardBrightness = 100;

        // UI Controls
        private RadioButton _lowPowerProfileButton;
        private RadioButton _quietProfileButton;
        private RadioButton _balancedProfileButton;
        private RadioButton _performanceProfileButton;
        private RadioButton _turboProfileButton;
        
        private RadioButton _manualFanSpeedRadioButton;
        private Slider _cpuFanSlider;
        private Slider _gpuFanSlider;
        private TextBlock _cpuFanTextBlock;
        private TextBlock _gpuFanTextBlock;
        private Button _setManualSpeedButton;
        private RadioButton _autoFanSpeedRadioButton;
        
        private Button _startCalibrationButton;
        private Button _stopCalibrationButton;
        private TextBlock _calibrationStatusTextBlock;
        private CheckBox _batteryLimitCheckBox;
        
        private ComboBox _usbChargingComboBox;
        private Button _usbChargeButton;
        
        private Border _zone1Border;
        private Border _zone2Border;
        private Border _zone3Border;
        private Border _zone4Border;
        private Button _pickColor1Button;
        private Button _pickColor2Button;
        private Button _pickColor3Button;
        private Button _pickColor4Button;
        private Slider _keyBrightnessSlider;
        private TextBlock _keyBrightnessText;
        private Button _applyKeyboardColorsButton;
        
        private ComboBox _lightingModeComboBox;
        private Slider _lightingSpeedSlider;
        private TextBlock _lightSpeedTextBlock;
        private Border _effectColorBorder;
        private Button _lightingEffectsColorPickButton;
        private RadioButton _leftToRightRadioButton;
        private RadioButton _rightToLeftRadioButton;
        private Button _lightingEffectsApplyButton;
        
        private CheckBox _backlightTimeoutCheckBox;
        private CheckBox _lcdOverrideCheckBox;
        private CheckBox _bootAnimAndSoundCheckBox;
        
        private TextBlock  _thermalProfileInfoText;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _client = new DAMXClient();
            
            // Delay initialization until the window is loaded
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
            
            // Fan control controls
            _manualFanSpeedRadioButton = this.FindControl<RadioButton>("ManualFanSpeedRadioButton");
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
            _zone4Border = this.FindControl<Border>("Zone4Border");
            _pickColor1Button = this.FindControl<Button>("PickColor1Button");
            _pickColor2Button = this.FindControl<Button>("PickColor2Button");
            _pickColor3Button = this.FindControl<Button>("PickColor3Button");
            _pickColor4Button = this.FindControl<Button>("PickColor4Button");
            _keyBrightnessSlider = this.FindControl<Slider>("KeyBrightnessSlider");
            _keyBrightnessText = this.FindControl<TextBlock>("KeyBrightnessText");
            _applyKeyboardColorsButton = this.FindControl<Button>("ApplyKeyboardColorsButton");
            
            // Lighting effects controls
            _lightingModeComboBox = this.FindControl<ComboBox>("LightingModeComboBox");
            _lightingSpeedSlider = this.FindControl<Slider>("LightingSpeedSlider");
            _lightSpeedTextBlock = this.FindControl<TextBlock>("LightSpeedTextBlock");
            _effectColorBorder = this.FindControl<Border>("EffectColorBorder");
            _lightingEffectsColorPickButton = this.FindControl<Button>("LightingEffectsColorPickButton");
            _leftToRightRadioButton = this.FindControl<RadioButton>("LeftToRightRadioButton");
            _rightToLeftRadioButton = this.FindControl<RadioButton>("RightToLeftRadioButton");
            _lightingEffectsApplyButton = this.FindControl<Button>("LightingEffectsApplyButton");
            
            // System settings controls
            _backlightTimeoutCheckBox = this.FindControl<CheckBox>("BacklightTimeoutCheckBox");
            _lcdOverrideCheckBox = this.FindControl<CheckBox>("LcdOverrideCheckBox");
            _bootAnimAndSoundCheckBox = this.FindControl<CheckBox>("BootAnimAndSoundCheckBox");
            
            //Info Texts
            _thermalProfileInfoText = this.FindControl<TextBlock>("ThermalProfileInfoText");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private bool _isCalibrating = false;
        public bool IsCalibrating
        {
            get => _isCalibrating;
            set => SetField(ref _isCalibrating, value);
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

            // Keyboard lighting zone color handlers
            _pickColor1Button.Click += PickColor1Button_Click;
            _pickColor2Button.Click += PickColor2Button_Click;
            _pickColor3Button.Click += PickColor3Button_Click;
            _pickColor4Button.Click += PickColor4Button_Click;
            _keyBrightnessSlider.PropertyChanged += KeyboardBrightnessSlider_ValueChanged;
            _applyKeyboardColorsButton.Click += ApplyKeyboardColorsButton_Click;

            // Lighting effects handlers
            _lightingSpeedSlider.PropertyChanged += LightingSpeedSlider_ValueChanged;
            _lightingEffectsColorPickButton.Click += LightingEffectsColorPickButton_Click;
            _lightingEffectsApplyButton.Click += LightingEffectsApplyButton_Click;

            // System settings handlers
            _backlightTimeoutCheckBox.Click += BacklightTimeoutCheckBox_Click;
            _lcdOverrideCheckBox.Click += LcdOverrideCheckBox_Click;
            _bootAnimAndSoundCheckBox.Click += BootSoundCheckBox_Click;
            
        }

        private async void InitializeAsync()
        {
            try
            {
                _isConnected = await _client.ConnectAsync();
                if (_isConnected)
                {
                    await LoadSettingsAsync();
                }
                else
                {
                    await ShowErrorDialogAsync("Failed to connect to DAMX daemon. Make sure the service is running.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Error initializing: {ex.Message}");
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                _settings = await _client.GetAllSettingsAsync();
                
                
                ApplySettingsToUI();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Error loading settings: {ex.Message}");
            }
        }

        private void ApplySettingsToUI()
        {
            // Apply thermal profile
            switch (_settings.ThermalProfile.Current.ToLower())
            {
                case "quiet":
                    _quietProfileButton.IsChecked = true;
                    // Disable manual fan controls in Silent Mode

                    _manualFanSpeedRadioButton.IsEnabled = false;
                    _autoFanSpeedRadioButton.IsChecked = true;
                    break;
                case "balanced":
                    _balancedProfileButton.IsChecked = true;
                    break;
                case "balanced-performance":
                    _performanceProfileButton.IsChecked = true;
                    break;
                case "performance":
                    _turboProfileButton.IsChecked = true;
                    break;
            }

            // Apply backlight timeout setting
            bool backlightTimeoutEnabled = _settings.BacklightTimeout.ToLower() == "enabled";
            _backlightTimeoutCheckBox.IsChecked = backlightTimeoutEnabled;

            // Apply battery settings
            bool batteryLimiterEnabled = _settings.BatteryLimiter.Equals("1", StringComparison.OrdinalIgnoreCase);
            _batteryLimitCheckBox.IsChecked = batteryLimiterEnabled;

            // Apply battery calibration status
            bool isCalibrating = _settings.BatteryCalibration.Equals("1", StringComparison.OrdinalIgnoreCase);
            IsCalibrating = isCalibrating;
            _startCalibrationButton.IsEnabled = !isCalibrating;
            _stopCalibrationButton.IsEnabled = isCalibrating;
            _calibrationStatusTextBlock.Text = isCalibrating ? "Status: Calibrating" : "Status: Not calibrating";


            // Apply boot animation sound setting
            bool bootSoundEnabled = _settings.BootAnimationSound.ToLower() == "enabled";
            _bootAnimAndSoundCheckBox.IsChecked = bootSoundEnabled;

            // Apply LCD override setting
            bool lcdOverrideEnabled = _settings.LcdOverride.ToLower() == "enabled";
            _lcdOverrideCheckBox.IsChecked = lcdOverrideEnabled;

            // Apply USB charging level
            int usbChargingIndex = 0;
            switch (_settings.UsbCharging)
            {
                case "disabled":
                    usbChargingIndex = 0;
                    break;
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

            // Apply fan speeds
            if (int.TryParse(_settings.FanSpeed.Cpu, out int cpuSpeed))
            {
                Console.WriteLine("CPU Speed: " + cpuSpeed);
                _cpuFanSpeed = cpuSpeed;
                _cpuFanSlider.Value = cpuSpeed;
                _cpuFanTextBlock.Text = $"{cpuSpeed}%";
            }

            if (int.TryParse(_settings.FanSpeed.Gpu, out int gpuSpeed))
            {
                _gpuFanSpeed = gpuSpeed;
                _gpuFanSlider.Value = gpuSpeed;
                _gpuFanTextBlock.Text = $"{gpuSpeed}%";
            }

            // NEW: Determine manual/auto mode based on current fan speeds
            bool isManualMode = cpuSpeed > 0 || gpuSpeed > 0;
            _isManualFanControl = isManualMode;
            _manualFanSpeedRadioButton.IsChecked = isManualMode;
            _autoFanSpeedRadioButton.IsChecked = !isManualMode;

            // Apply keyboard lighting settings
            ApplyKeyboardSettings();
            
            // Update UI with initial values
            _zone1Border.Background = new SolidColorBrush(Color.Parse(_zone1Color));
            _zone2Border.Background = new SolidColorBrush(Color.Parse(_zone2Color));
            _zone3Border.Background = new SolidColorBrush(Color.Parse(_zone3Color));
            _zone4Border.Background = new SolidColorBrush(Color.Parse(_zone4Color));
            _effectColorBorder.Background = new SolidColorBrush(Color.Parse(_effectColor));
            _keyBrightnessText.Text = $"{_keyboardBrightness}%";
            _lightSpeedTextBlock.Text = _lightingSpeed.ToString();
        }

        private void ApplyKeyboardSettings()
        {
            if (_settings.HasFourZoneKb)
            {
                // TODO: Parse and apply the keyboard lighting settings from
                // _settings.PerZoneMode and _settings.FourZoneMode
            }
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            await new Window
            {
                Title = "Error",
                Content = new TextBlock { Text = message, Margin = new Thickness(20), TextWrapping = TextWrapping.Wrap },
                Width = 400,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }.ShowDialog(this);
        }

        #region Event Handlers

        // Unified thermal profile handler
        private async void ProfileButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isConnected) return;

            var button = sender as RadioButton;
            if (button?.IsChecked != true) return;

            string profile = button.Name switch
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
                _manualFanSpeedRadioButton.IsChecked = false;
                _autoFanSpeedRadioButton.IsChecked = true;
                _manualFanSpeedRadioButton.IsEnabled = false;
                _thermalProfileInfoText.Text = "Minimizes noise, prioritizes low power and cooling.";
            }
            else
            {
                // Re-enable manual controls (if they were enabled before)
                _manualFanSpeedRadioButton.IsEnabled = true;
            }
            
            if (profile == "low-power")
            {
                _thermalProfileInfoText.Text = "Prioritizes energy efficiency, reduces performance to extend battery life.";
            }

            if (profile == "balanced")
            {
                _thermalProfileInfoText.Text = "Optimal mix of performance and noise for everyday tasks.";
            }

            if (profile == "balanced-performance")
            {
                _thermalProfileInfoText.Text = "Maximizes speed for demanding workloads, higher fan noise";
            }

            if (profile == "performance")
            {
                _thermalProfileInfoText.Text = "Unleashes peak power for extreme tasks, loudest fans.";
            }
            
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
            }
        }

        private void GpuFanSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty)
            {
                // Properly convert double to int
                _gpuFanSpeed = Convert.ToInt32(e.NewValue);
                _gpuFanTextBlock.Text = $"{_gpuFanSpeed}%";
            }
        }

        private async void SetManualSpeedButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                await _client.SetFanSpeedAsync(_cpuFanSpeed, _gpuFanSpeed);
            }
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
                bool isEnabled = checkBox?.IsChecked ?? false;
                await _client.SetBatteryLimiterAsync(isEnabled);
            }
        }

        // USB Charging Handler
        private async void UsbChargeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                int selectedIndex = _usbChargingComboBox.SelectedIndex;
                int level = 0;
                
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

        // Keyboard Lighting Zone Color Handlers
        private void PickColor1Button_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement color picker dialog
            // For now, we'll just use a predefined color
            _zone1Color = "#4287f5";
            _zone1Border.Background = new SolidColorBrush(Color.Parse(_zone1Color));
        }

        private void PickColor2Button_Click(object sender, RoutedEventArgs e)
        {
            _zone2Color = "#ff5733";
            _zone2Border.Background = new SolidColorBrush(Color.Parse(_zone2Color));
        }

        private void PickColor3Button_Click(object sender, RoutedEventArgs e)
        {
            _zone3Color = "#33ff57";
            _zone3Border.Background = new SolidColorBrush(Color.Parse(_zone3Color));
        }

        private void PickColor4Button_Click(object sender, RoutedEventArgs e)
        {
            _zone4Color = "#ff33a6";
            _zone4Border.Background = new SolidColorBrush(Color.Parse(_zone4Color));
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
            if (_isConnected && _settings.HasFourZoneKb)
            {
                await _client.SetPerZoneModeAsync(
                    _zone1Color,
                    _zone2Color,
                    _zone3Color,
                    _zone4Color,
                    _keyboardBrightness
                );
            }
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

        private void LightingEffectsColorPickButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement color picker dialog
            // For now, we'll just use a predefined color
            _effectColor = "#0078D7";
            _effectColorBorder.Background = new SolidColorBrush(Color.Parse(_effectColor));
        }

        private async void LightingEffectsApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected && _settings.HasFourZoneKb)
            {
                int mode = _lightingModeComboBox.SelectedIndex;
                int direction = _leftToRightRadioButton.IsChecked == true ? 2 : 1;
                
                // Parse the hex color
                Color color = Color.Parse(_effectColor);
                
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
                bool isEnabled = checkBox?.IsChecked ?? false;
                await _client.SetBacklightTimeoutAsync(isEnabled);
            }
        }

        // LCD Override Handler
        private async void LcdOverrideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                var checkBox = sender as CheckBox;
                bool isEnabled = checkBox?.IsChecked ?? false;
                await _client.SetLcdOverrideAsync(isEnabled);
            }
        }

        // Boot Sound Handler
        private async void BootSoundCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                var checkBox = sender as CheckBox;
                bool isEnabled = checkBox?.IsChecked ?? false;
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
}