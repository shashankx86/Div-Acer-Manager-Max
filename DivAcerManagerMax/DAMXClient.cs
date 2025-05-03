using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DivAcerManagerMax
{
    /// <summary>
    /// Client for communicating with the DAMX-Daemon over Unix socket
    /// </summary>
    public class DAMXClient : IDisposable
    {
        private const string SocketPath = "/var/run/DAMX.sock";
        private Socket _socket;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public DAMXClient()
        {
            _isConnected = false;
        }

        /// <summary>
        /// Connect to the DAMX-Daemon Unix socket
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    return true;
                }

                _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(SocketPath);
                
                await _socket.ConnectAsync(endpoint);
                _isConnected = true;
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to daemon: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the DAMX-Daemon Unix socket
        /// </summary>
        public void Disconnect()
        {
            if (_isConnected)
            {
                try
                {
                    _socket?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during disconnect: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                }
            }
        }

        /// <summary>
        /// Send a command to the DAMX-Daemon and receive response
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="parameters">Optional parameters</param>
        /// <returns>Response from daemon as a JsonDocument</returns>
        public async Task<JsonDocument> SendCommandAsync(string command, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
                if (!_isConnected)
                {
                    throw new InvalidOperationException("Not connected to daemon");
                }
            }

            var request = new
            {
                command,
                @params = parameters ?? new Dictionary<string, object>()
            };

            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            try
            {
                // Send request
                await _socket.SendAsync(requestBytes, SocketFlags.None);

                // Receive response
                var buffer = new byte[4096];
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                
                if (received > 0)
                {
                    var responseJson = Encoding.UTF8.GetString(buffer, 0, received);
                    return JsonDocument.Parse(responseJson);
                }
                else
                {
                    throw new IOException("No data received from daemon");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error communicating with daemon: {ex.Message}");
                _isConnected = false;
                throw;
            }
        }

        /// <summary>
        /// Get all settings from the DAMX-Daemon
        /// </summary>
        /// <returns>All settings as a JsonDocument</returns>
        public async Task<DAMXSettings> GetAllSettingsAsync()
        {
            var response = await SendCommandAsync("get_all_settings");
            var success = response.RootElement.GetProperty("success").GetBoolean();
            
            if (success)
            {
                var data = response.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<DAMXSettings>(data.GetRawText());
            }
            else
            {
                var error = response.RootElement.GetProperty("error").GetString();
                throw new Exception($"Failed to get settings: {error}");
            }
        }

        /// <summary>
        /// Set thermal profile
        /// </summary>
        /// <param name="profile">Profile name</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetThermalProfileAsync(string profile)
        {
            var parameters = new Dictionary<string, object>
            {
                { "profile", profile }
            };
            
            var response = await SendCommandAsync("set_thermal_profile", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set fan speeds
        /// </summary>
        /// <param name="cpu">CPU fan speed (0-100)</param>
        /// <param name="gpu">GPU fan speed (0-100)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetFanSpeedAsync(int cpu, int gpu)
        {
            var parameters = new Dictionary<string, object>
            {
                { "cpu", cpu },
                { "gpu", gpu }
            };
            
            var response = await SendCommandAsync("set_fan_speed", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set backlight timeout
        /// </summary>
        /// <param name="enabled">Enable or disable timeout</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetBacklightTimeoutAsync(bool enabled)
        {
            var parameters = new Dictionary<string, object>
            {
                { "enabled", enabled }
            };
            
            var response = await SendCommandAsync("set_backlight_timeout", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set battery calibration
        /// </summary>
        /// <param name="enabled">Start or stop calibration</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetBatteryCalibrationAsync(bool enabled)
        {
            var parameters = new Dictionary<string, object>
            {
                { "enabled", enabled }
            };
            
            var response = await SendCommandAsync("set_battery_calibration", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set battery limiter
        /// </summary>
        /// <param name="enabled">Enable or disable battery limit</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetBatteryLimiterAsync(bool enabled)
        {
            var parameters = new Dictionary<string, object>
            {
                { "enabled", enabled }
            };
            
            var response = await SendCommandAsync("set_battery_limiter", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set boot animation sound
        /// </summary>
        /// <param name="enabled">Enable or disable boot sound</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetBootAnimationSoundAsync(bool enabled)
        {
            var parameters = new Dictionary<string, object>
            {
                { "enabled", enabled }
            };
            
            var response = await SendCommandAsync("set_boot_animation_sound", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set LCD override
        /// </summary>
        /// <param name="enabled">Enable or disable LCD override</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetLcdOverrideAsync(bool enabled)
        {
            var parameters = new Dictionary<string, object>
            {
                { "enabled", enabled }
            };
            
            var response = await SendCommandAsync("set_lcd_override", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set USB charging level
        /// </summary>
        /// <param name="level">USB charging level (0, 10, 20, or 30)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetUsbChargingAsync(int level)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level", level }
            };
            
            var response = await SendCommandAsync("set_usb_charging", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set keyboard per-zone mode colors
        /// </summary>
        /// <param name="zone1">Zone 1 color (hex RGB)</param>
        /// <param name="zone2">Zone 2 color (hex RGB)</param>
        /// <param name="zone3">Zone 3 color (hex RGB)</param>
        /// <param name="zone4">Zone 4 color (hex RGB)</param>
        /// <param name="brightness">Brightness (0-100)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetPerZoneModeAsync(string zone1, string zone2, string zone3, string zone4, int brightness)
        {
            var parameters = new Dictionary<string, object>
            {
                { "zone1", zone1 },
                { "zone2", zone2 },
                { "zone3", zone3 },
                { "zone4", zone4 },
                { "brightness", brightness }
            };
            
            var response = await SendCommandAsync("set_per_zone_mode", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        /// <summary>
        /// Set keyboard lighting effect
        /// </summary>
        /// <param name="mode">Effect mode (0-7)</param>
        /// <param name="speed">Effect speed (0-9)</param>
        /// <param name="brightness">Brightness (0-100)</param>
        /// <param name="direction">Direction (1=right to left, 2=left to right)</param>
        /// <param name="red">Red component (0-255)</param>
        /// <param name="green">Green component (0-255)</param>
        /// <param name="blue">Blue component (0-255)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetFourZoneModeAsync(int mode, int speed, int brightness, int direction, int red, int green, int blue)
        {
            var parameters = new Dictionary<string, object>
            {
                { "mode", mode },
                { "speed", speed },
                { "brightness", brightness },
                { "direction", direction },
                { "red", red },
                { "green", green },
                { "blue", blue }
            };
            
            var response = await SendCommandAsync("set_four_zone_mode", parameters);
            return response.RootElement.GetProperty("success").GetBoolean();
        }

        public void Dispose()
        {
            Disconnect();
            _socket?.Dispose();
        }
    }

    /// <summary>
    /// Models for DAMX settings
    /// </summary>
    public class DAMXSettings
    {
        [JsonPropertyName("laptop_type")]
        public string LaptopType { get; set; }

        [JsonPropertyName("has_four_zone_kb")]
        public bool HasFourZoneKb { get; set; }

        [JsonPropertyName("thermal_profile")]
        public ThermalProfileSettings ThermalProfile { get; set; }

        [JsonPropertyName("backlight_timeout")]
        public string BacklightTimeout { get; set; }

        [JsonPropertyName("battery_calibration")]
        public string BatteryCalibration { get; set; }

        [JsonPropertyName("battery_limiter")]
        public string BatteryLimiter { get; set; }

        [JsonPropertyName("boot_animation_sound")]
        public string BootAnimationSound { get; set; }

        [JsonPropertyName("fan_speed")]
        public FanSpeedSettings FanSpeed { get; set; }

        [JsonPropertyName("lcd_override")]
        public string LcdOverride { get; set; }

        [JsonPropertyName("usb_charging")]
        public string UsbCharging { get; set; }

        [JsonPropertyName("per_zone_mode")]
        public string PerZoneMode { get; set; }

        [JsonPropertyName("four_zone_mode")]
        public string FourZoneMode { get; set; }
    }

    public class ThermalProfileSettings
    {
        [JsonPropertyName("current")]
        public string Current { get; set; }

        [JsonPropertyName("available")]
        public List<string> Available { get; set; }
    }

    public class FanSpeedSettings
    {
        [JsonPropertyName("cpu")]
        public string Cpu { get; set; }

        [JsonPropertyName("gpu")]
        public string Gpu { get; set; }
    }
}