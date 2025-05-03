#!/usr/bin/env python3
# DAMX-Daemon - Manage Acer laptop features as root service communicating with Linuwu-sense drivers
# Compatible with Predator and Nitro laptops

import os
import sys
import json
import time
import argparse
import logging
import logging.handlers
import socket
import threading
import signal
import configparser
import traceback
from pathlib import Path
from enum import Enum
from typing import Dict, List, Tuple, Union, Optional

# Constants
VERSION = "1.0.0"
SOCKET_PATH = "/var/run/DAMX.sock"
LOG_PATH = "/var/log/DAMX_Daemon_Log.log"
CONFIG_PATH = "/etc/DAMX_Daemon/config.ini"
PID_FILE = "/var/run/DAMX-Daemon.pid"

# Check if running as root
if os.geteuid() != 0:
    print("This daemon must run as root. Please use sudo or run as root.")
    sys.exit(1)

# Configure logging
log = logging.getLogger("DAMXDaemon")
log.setLevel(logging.INFO)
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

# Console handler
console_handler = logging.StreamHandler()
console_handler.setFormatter(formatter)
log.addHandler(console_handler)

# File handler with rotation
file_handler = logging.handlers.RotatingFileHandler(
    LOG_PATH, maxBytes=1024*1024*5, backupCount=5)
file_handler.setFormatter(formatter)
log.addHandler(file_handler)

class LaptopType(Enum):
    UNKNOWN = 0
    PREDATOR = 1
    NITRO = 2

class DAMXManager:
    """Manages all the DAMX-Daemon features"""
    
    def __init__(self):
        self.laptop_type = self._detect_laptop_type()
        self.base_path = self._get_base_path()
        self.has_four_zone_kb = self._check_four_zone_kb()
        
        log.info(f"Detected laptop type: {self.laptop_type.name}")
        log.info(f"Base path: {self.base_path}")
        log.info(f"Four-zone keyboard: {'Yes' if self.has_four_zone_kb else 'No'}")
        
        # Check if paths exist
        if not os.path.exists(self.base_path):
            log.error(f"Base path does not exist: {self.base_path}")
            raise FileNotFoundError(f"Base path does not exist: {self.base_path}")
            
    def _detect_laptop_type(self) -> LaptopType:
        """Detect whether this is a Predator or Nitro laptop"""
        predator_path = "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/predator_sense"
        nitro_path = "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/nitro_sense"
        
        if os.path.exists(predator_path):
            return LaptopType.PREDATOR
        elif os.path.exists(nitro_path):
            return LaptopType.NITRO
        else:
            return LaptopType.UNKNOWN
            
    def _get_base_path(self) -> str:
        """Get the base path for VFS access based on laptop type"""
        if self.laptop_type == LaptopType.PREDATOR:
            return "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/predator_sense"
        elif self.laptop_type == LaptopType.NITRO:
            return "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/nitro_sense"
        else:
            return ""
            
    def _check_four_zone_kb(self) -> bool:
        """Check if four-zone keyboard is available"""
        if self.laptop_type != LaptopType.UNKNOWN:
            kb_path = "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/four_zoned_kb"
            return os.path.exists(kb_path)
        return False
        
    def _read_file(self, path: str) -> str:
        """Read from a VFS file"""
        try:
            with open(path, 'r') as f:
                return f.read().strip()
        except Exception as e:
            log.error(f"Failed to read from {path}: {e}")
            return ""
            
    def _write_file(self, path: str, value: str) -> bool:
        """Write to a VFS file"""
        try:
            with open(path, 'w') as f:
                f.write(str(value))
            return True
        except Exception as e:
            log.error(f"Failed to write to {path}: {e}")
            return False
            
    def get_thermal_profile(self) -> str:
        """Get current thermal profile"""
        return self._read_file("/sys/firmware/acpi/platform_profile")
        
    def set_thermal_profile(self, profile: str) -> bool:
        """Set thermal profile"""
        available_profiles = self.get_thermal_profile_choices()
        if profile not in available_profiles:
            log.error(f"Invalid thermal profile: {profile}. Available profiles: {available_profiles}")
            return False
            
        return self._write_file("/sys/firmware/acpi/platform_profile", profile)
        
    def get_thermal_profile_choices(self) -> List[str]:
        """Get available thermal profiles"""
        choices = self._read_file("/sys/firmware/acpi/platform_profile_choices")
        return choices.split() if choices else []
        
    def get_backlight_timeout(self) -> str:
        """Get backlight timeout status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "backlight_timeout"))
        
    def set_backlight_timeout(self, enabled: bool) -> bool:
        """Set backlight timeout status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
        return self._write_file(
            os.path.join(self.base_path, "backlight_timeout"), 
            "1" if enabled else "0"
        )
        
    def get_battery_calibration(self) -> str:
        """Get battery calibration status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "battery_calibration"))
        
    def set_battery_calibration(self, enabled: bool) -> bool:
        """Start or stop battery calibration"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
        return self._write_file(
            os.path.join(self.base_path, "battery_calibration"), 
            "1" if enabled else "0"
        )
        
    def get_battery_limiter(self) -> str:
        """Get battery limiter status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "battery_limiter"))
        
    def set_battery_limiter(self, enabled: bool) -> bool:
        """Set battery limiter status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
        return self._write_file(
            os.path.join(self.base_path, "battery_limiter"), 
            "1" if enabled else "0"
        )
        
    def get_boot_animation_sound(self) -> str:
        """Get boot animation sound status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "boot_animation_sound"))
        
    def set_boot_animation_sound(self, enabled: bool) -> bool:
        """Set boot animation sound status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
        return self._write_file(
            os.path.join(self.base_path, "boot_animation_sound"), 
            "1" if enabled else "0"
        )
        
    def get_fan_speed(self) -> Tuple[str, str]:
        """Get CPU and GPU fan speeds"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ("", "")
        
        file_path = os.path.join(self.base_path, "fan_speed")
        print(f"Reading from: {file_path}")  # Debug
        
        try:
            with open(file_path, 'r') as f:
                speeds = f.read().strip()
                print(f"Raw content: '{speeds}'")  # Debug
                
                if "," in speeds:
                    cpu, gpu = speeds.split(",", 1)
                    return (cpu.strip(), gpu.strip())
        except Exception as e:
            print(f"Error reading fan speed: {e}")
        
        return ("0", "0")  # Fallback
        
    def set_fan_speed(self, cpu: int, gpu: int) -> bool:
        """Set CPU and GPU fan speeds"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
            
        # Validate values
        if not (0 <= cpu <= 100 and 0 <= gpu <= 100):
            log.error(f"Invalid fan speeds. Values must be between 0 and 100: cpu={cpu}, gpu={gpu}")
            return False
            
        return self._write_file(
            os.path.join(self.base_path, "fan_speed"), 
            f"{cpu},{gpu}"
        )
        
    def get_lcd_override(self) -> str:
        """Get LCD override status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "lcd_override"))
        
    def set_lcd_override(self, enabled: bool) -> bool:
        """Set LCD override status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
        return self._write_file(
            os.path.join(self.base_path, "lcd_override"), 
            "1" if enabled else "0"
        )
        
    def get_usb_charging(self) -> str:
        """Get USB charging status"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return ""
        return self._read_file(os.path.join(self.base_path, "usb_charging"))
        
    def set_usb_charging(self, level: int) -> bool:
        """Set USB charging level (0, 10, 20, 30)"""
        if self.laptop_type == LaptopType.UNKNOWN:
            return False
            
        # Validate values
        if level not in [0, 10, 20, 30]:
            log.error(f"Invalid USB charging level. Must be 0, 10, 20, or 30: {level}")
            return False
            
        return self._write_file(
            os.path.join(self.base_path, "usb_charging"), 
            str(level)
        )
        
    def get_per_zone_mode(self) -> str:
        """Get per-zone mode configuration"""
        if not self.has_four_zone_kb:
            return ""
        return self._read_file("/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/four_zoned_kb/per_zone_mode")
        
    def set_per_zone_mode(self, zone1: str, zone2: str, zone3: str, zone4: str, brightness: int) -> bool:
        """Set per-zone mode configuration
        
        Args:
            zone1-zone4: RGB hex values (e.g., "4287f5")
            brightness: 0-100
        """
        if not self.has_four_zone_kb:
            return False
            
        # Validate hex values
        for i, zone in enumerate([zone1, zone2, zone3, zone4], 1):
            try:
                # Check if valid hex color
                int(zone, 16)
                if len(zone) != 6:
                    log.error(f"Invalid hex color for zone {i}: {zone}. Must be 6 characters.")
                    return False
            except ValueError:
                log.error(f"Invalid hex color for zone {i}: {zone}")
                return False
                
        # Validate brightness
        if not (0 <= brightness <= 100):
            log.error(f"Invalid brightness. Must be between 0 and 100: {brightness}")
            return False
            
        value = f"{zone1},{zone2},{zone3},{zone4},{brightness}"
        return self._write_file(
            "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/four_zoned_kb/per_zone_mode", 
            value
        )
        
    def get_four_zone_mode(self) -> str:
        """Get four-zone mode configuration"""
        if not self.has_four_zone_kb:
            return ""
        return self._read_file("/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/four_zoned_kb/four_zone_mode")
        
    def set_four_zone_mode(self, mode: int, speed: int, brightness: int, 
                           direction: int, red: int, green: int, blue: int) -> bool:
        """Set four-zone mode configuration
        
        Args:
            mode: 0-7 (lighting effect type)
            speed: 0-9 (effect speed)
            brightness: 0-100 (light intensity)
            direction: 1-2 (1=right to left, 2=left to right)
            red, green, blue: 0-255 (RGB color values)
        """
        if not self.has_four_zone_kb:
            return False
            
        # Validate values
        if not (0 <= mode <= 7):
            log.error(f"Invalid mode. Must be between 0 and 7: {mode}")
            return False
            
        if not (0 <= speed <= 9):
            log.error(f"Invalid speed. Must be between 0 and 9: {speed}")
            return False
            
        if not (0 <= brightness <= 100):
            log.error(f"Invalid brightness. Must be between 0 and 100: {brightness}")
            return False
            
        if direction not in [1, 2]:
            log.error(f"Invalid direction. Must be 1 or 2: {direction}")
            return False
            
        if not all(0 <= color <= 255 for color in [red, green, blue]):
            log.error(f"Invalid RGB values. Must be between 0 and 255: {red},{green},{blue}")
            return False
            
        value = f"{mode},{speed},{brightness},{direction},{red},{green},{blue}"
        return self._write_file(
            "/sys/module/linuwu_sense/drivers/platform:acer-wmi/acer-wmi/four_zoned_kb/four_zone_mode", 
            value
        )

    def get_all_settings(self) -> Dict:
        """Get all DAMX-Daemon settings as a dictionary"""
        settings = {
            "laptop_type": self.laptop_type.name,
            "has_four_zone_kb": self.has_four_zone_kb,
            "thermal_profile": {
                "current": self.get_thermal_profile(),
                "available": self.get_thermal_profile_choices()
            }
        }
        
        if self.laptop_type != LaptopType.UNKNOWN:
            cpu_fan, gpu_fan = self.get_fan_speed()
            
            settings.update({
                "backlight_timeout": self.get_backlight_timeout(),
                "battery_calibration": self.get_battery_calibration(),
                "battery_limiter": self.get_battery_limiter(),
                "boot_animation_sound": self.get_boot_animation_sound(),
                "fan_speed": {
                    "cpu": cpu_fan,
                    "gpu": gpu_fan
                },
                "lcd_override": self.get_lcd_override(),
                "usb_charging": self.get_usb_charging()
            })
            
        if self.has_four_zone_kb:
            settings.update({
                "per_zone_mode": self.get_per_zone_mode(),
                "four_zone_mode": self.get_four_zone_mode()
            })
            
        return settings


class DaemonServer:
    """Unix Socket server for IPC with the GUI client"""
    
    def __init__(self, manager: DAMXManager):
        self.manager = manager
        self.socket = None
        self.running = False
        self.clients = []
        
    def start(self):
        """Start the Unix socket server"""
        # Remove socket if it already exists
        try:
            if os.path.exists(SOCKET_PATH):
                os.unlink(SOCKET_PATH)
        except OSError as e:
            log.error(f"Failed to remove existing socket: {e}")
            return False
            
        try:
            self.socket = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            self.socket.bind(SOCKET_PATH)
            # Ensure socket permissions allow non-root access
            os.chmod(SOCKET_PATH, 0o666)
            self.socket.listen(5)
            self.socket.settimeout(1)  # 1 second timeout for clean shutdown
            self.running = True
            
            log.info(f"Server listening on {SOCKET_PATH}")
            
            # Accept connections in a loop
            while self.running:
                try:
                    client, _ = self.socket.accept()
                    client_thread = threading.Thread(target=self.handle_client, args=(client,))
                    client_thread.daemon = True
                    client_thread.start()
                    self.clients.append((client, client_thread))
                except socket.timeout:
                    # This is expected due to the timeout
                    continue
                except Exception as e:
                    if self.running:  # Only log if not shutting down
                        log.error(f"Error accepting connection: {e}")
                        
            return True
            
        except Exception as e:
            log.error(f"Failed to start server: {e}")
            return False
            
    def stop(self):
        """Stop the server and clean up"""
        log.info("Stopping server...")
        self.running = False
        
        # Close all client connections
        for client, _ in self.clients:
            try:
                client.close()
            except:
                pass
                
        # Close server socket
        if self.socket:
            try:
                self.socket.close()
            except:
                pass
                
        # Remove socket file
        try:
            if os.path.exists(SOCKET_PATH):
                os.unlink(SOCKET_PATH)
        except:
            pass
            
    def handle_client(self, client_socket):
        """Handle communication with a client"""
        try:
            while self.running:
                # Receive data from client
                data = client_socket.recv(4096)
                if not data:
                    break
                    
                try:
                    # Parse JSON request
                    request = json.loads(data.decode('utf-8'))
                    command = request.get("command", "")
                    params = request.get("params", {})
                    
                    # Process command
                    response = self.process_command(command, params)
                    
                    # Send response
                    client_socket.sendall(json.dumps(response).encode('utf-8'))
                    
                except json.JSONDecodeError:
                    log.error("Invalid JSON received")
                    client_socket.sendall(json.dumps({
                        "success": False,
                        "error": "Invalid JSON format"
                    }).encode('utf-8'))
                except Exception as e:
                    log.error(f"Error processing request: {e}")
                    log.error(traceback.format_exc())
                    client_socket.sendall(json.dumps({
                        "success": False,
                        "error": str(e)
                    }).encode('utf-8'))
                    
        except Exception as e:
            if self.running:  # Only log if not shutting down
                log.error(f"Client connection error: {e}")
        finally:
            try:
                client_socket.close()
            except:
                pass
                
    def process_command(self, command: str, params: Dict) -> Dict:
        """Process a command from the client"""
        log.info(f"Processing command: {command} with params: {params}")
        
        try:
            if command == "get_all_settings":
                settings = self.manager.get_all_settings()
                return {
                    "success": True,
                    "data": settings
                }
                
            elif command == "get_thermal_profile":
                profile = self.manager.get_thermal_profile()
                choices = self.manager.get_thermal_profile_choices()
                return {
                    "success": True,
                    "data": {
                        "current": profile,
                        "available": choices
                    }
                }
                
            elif command == "set_thermal_profile":
                profile = params.get("profile", "")
                success = self.manager.set_thermal_profile(profile)
                return {
                    "success": success,
                    "data": {"profile": profile} if success else None,
                    "error": "Failed to set thermal profile" if not success else None
                }
                
            elif command == "set_backlight_timeout":
                enabled = params.get("enabled", False)
                success = self.manager.set_backlight_timeout(enabled)
                return {
                    "success": success,
                    "data": {"enabled": enabled} if success else None,
                    "error": "Failed to set backlight timeout" if not success else None
                }
                
            elif command == "set_battery_calibration":
                enabled = params.get("enabled", False)
                success = self.manager.set_battery_calibration(enabled)
                return {
                    "success": success,
                    "data": {"enabled": enabled} if success else None,
                    "error": "Failed to set battery calibration" if not success else None
                }
                
            elif command == "set_battery_limiter":
                enabled = params.get("enabled", False)
                success = self.manager.set_battery_limiter(enabled)
                return {
                    "success": success,
                    "data": {"enabled": enabled} if success else None,
                    "error": "Failed to set battery limiter" if not success else None
                }
                
            elif command == "set_boot_animation_sound":
                enabled = params.get("enabled", False)
                success = self.manager.set_boot_animation_sound(enabled)
                return {
                    "success": success,
                    "data": {"enabled": enabled} if success else None,
                    "error": "Failed to set boot animation sound" if not success else None
                }
                
            elif command == "set_fan_speed":
                cpu = params.get("cpu", 0)
                gpu = params.get("gpu", 0)
                success = self.manager.set_fan_speed(cpu, gpu)
                return {
                    "success": success,
                    "data": {"cpu": cpu, "gpu": gpu} if success else None,
                    "error": "Failed to set fan speed" if not success else None
                }
                
            elif command == "set_lcd_override":
                enabled = params.get("enabled", False)
                success = self.manager.set_lcd_override(enabled)
                return {
                    "success": success,
                    "data": {"enabled": enabled} if success else None,
                    "error": "Failed to set LCD override" if not success else None
                }
                
            elif command == "set_usb_charging":
                level = params.get("level", 0)
                success = self.manager.set_usb_charging(level)
                return {
                    "success": success,
                    "data": {"level": level} if success else None,
                    "error": "Failed to set USB charging" if not success else None
                }
                
            elif command == "set_per_zone_mode":
                zone1 = params.get("zone1", "000000")
                zone2 = params.get("zone2", "000000")
                zone3 = params.get("zone3", "000000")
                zone4 = params.get("zone4", "000000")
                brightness = params.get("brightness", 100)
                success = self.manager.set_per_zone_mode(zone1, zone2, zone3, zone4, brightness)
                return {
                    "success": success,
                    "data": {
                        "zone1": zone1,
                        "zone2": zone2,
                        "zone3": zone3,
                        "zone4": zone4,
                        "brightness": brightness
                    } if success else None,
                    "error": "Failed to set per-zone mode" if not success else None
                }
                
            elif command == "set_four_zone_mode":
                mode = params.get("mode", 0)
                speed = params.get("speed", 0)
                brightness = params.get("brightness", 100)
                direction = params.get("direction", 1)
                red = params.get("red", 0)
                green = params.get("green", 0)
                blue = params.get("blue", 0)
                success = self.manager.set_four_zone_mode(mode, speed, brightness, direction, red, green, blue)
                return {
                    "success": success,
                    "data": {
                        "mode": mode,
                        "speed": speed,
                        "brightness": brightness,
                        "direction": direction,
                        "red": red,
                        "green": green,
                        "blue": blue
                    } if success else None,
                    "error": "Failed to set four-zone mode" if not success else None
                }
                
            else:
                return {
                    "success": False,
                    "error": f"Unknown command: {command}"
                }
                
        except Exception as e:
            log.error(f"Error processing command {command}: {e}")
            log.error(traceback.format_exc())
            return {
                "success": False,
                "error": str(e)
            }


class DAMXDaemon:
    """Main daemon class that manages the lifecycle"""
    
    def __init__(self):
        self.running = False
        self.manager = None
        self.server = None
        
    def setup(self):
        """Set up the daemon"""
        # Create config directory if it doesn't exist
        os.makedirs(os.path.dirname(CONFIG_PATH), exist_ok=True)
        
        try:
            # Initialize DAMXManager
            self.manager = DAMXManager()
            return True
        except Exception as e:
            log.error(f"Failed to set up daemon: {e}")
            log.error(traceback.format_exc())
            return False
            
    def run(self):
        """Run the daemon"""
        log.info(f"Starting DAMX-Daemon v{VERSION}")
        
        # Write PID file
        with open(PID_FILE, 'w') as f:
            f.write(str(os.getpid()))
            
        # Set up signal handlers
        signal.signal(signal.SIGTERM, self.signal_handler)
        signal.signal(signal.SIGINT, self.signal_handler)
        
        # Set up and run the server
        try:
            self.running = True
            self.server = DaemonServer(self.manager)
            self.server.start()
        except Exception as e:
            log.error(f"Error running daemon: {e}")
            log.error(traceback.format_exc())
        finally:
            self.cleanup()
            
    def cleanup(self):
        """Clean up resources"""
        log.info("Cleaning up resources...")
        
        # Stop server
        if self.server:
            self.server.stop()
            
        # Remove PID file
        try:
            if os.path.exists(PID_FILE):
                os.unlink(PID_FILE)
        except:
            pass
            
        log.info("Daemon stopped")
        
    def signal_handler(self, sig, frame):
        """Handle termination signals"""
        log.info(f"Received signal {sig}, shutting down...")
        self.running = False
        if self.server:
            self.server.running = False


def parse_args():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(description="DAMX-Daemon")
    parser.add_argument('-v', '--verbose', action='store_true', help="Enable verbose logging")
    parser.add_argument('--version', action='version', version=f"DAMX-Daemon Daemon v{VERSION}")
    return parser.parse_args()


def main():
    """Main function"""
    args = parse_args()
    
    # Set log level based on verbosity
    if args.verbose:
        log.setLevel(logging.DEBUG)
        log.debug("Debug logging enabled")
    
    daemon = DAMXDaemon()
    if daemon.setup():
        daemon.run()
    else:
        log.error("Failed to set up daemon, exiting...")
        sys.exit(1)


if __name__ == "__main__":
    main()