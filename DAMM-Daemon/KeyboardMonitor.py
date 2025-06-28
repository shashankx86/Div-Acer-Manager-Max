#!/usr/bin/env python3
"""
KeyboardMonitor - Monitor keyboard events for specific keycode and trigger actions
Simple and efficient implementation using native Python libraries
"""

import os
import struct
import select
import subprocess
import threading
import logging
from pathlib import Path

# Event structure format (from linux/input.h)
# struct input_event {
#     struct timeval time;  // 8 bytes on 32-bit, 16 bytes on 64-bit
#     __u16 type;           // 2 bytes
#     __u16 code;           // 2 bytes  
#     __s32 value;          // 4 bytes
# }

# Determine if we're on 64-bit system
import platform
IS_64BIT = platform.machine().endswith('64')
EVENT_SIZE = 24 if IS_64BIT else 16

# Event types and codes
EV_KEY = 1
KEY_PRESS = 1
TARGET_KEYCODE = 425  # The keycode we're monitoring for

class KeyboardMonitor:
    def __init__(self, target_keycode=TARGET_KEYCODE, command_to_run="DAMX"):
        self.target_keycode = target_keycode
        self.command_to_run = command_to_run
        self.running = False
        self.device_path = None
        self.monitor_thread = None
        self.log = logging.getLogger("KeyboardMonitor")
        
    def find_keyboard_device(self):
        """Find the keyboard input device"""
        devices_path = Path("/proc/bus/input/devices")
        
        if not devices_path.exists():
            self.log.error("Cannot access /proc/bus/input/devices")
            return None
            
        try:
            with open(devices_path, 'r') as f:
                content = f.read()
                
            # Parse the devices file
            devices = content.split('\n\n')
            
            for device in devices:
                lines = device.strip().split('\n')
                if not lines:
                    continue
                    
                # Look for keyboard devices
                is_keyboard = False
                event_num = None
                
                for line in lines:
                    line = line.strip()
                    # Check if it's a keyboard
                    if 'keyboard' in line.lower() or 'Keyboard' in line:
                        is_keyboard = True
                    # Extract event number
                    elif line.startswith('H:') and 'event' in line:
                        # Extract event number from handler line
                        import re
                        match = re.search(r'event(\d+)', line)
                        if match:
                            event_num = match.group(1)
                
                if is_keyboard and event_num:
                    device_path = f"/dev/input/event{event_num}"
                    if os.path.exists(device_path):
                        self.log.info(f"Found keyboard device: {device_path}")
                        return device_path
                        
        except Exception as e:
            self.log.error(f"Error finding keyboard device: {e}")
            
        return None
    
    def execute_command(self):
        """Execute the target command"""
        try:
            subprocess.Popen(self.command_to_run, shell=True)
            self.log.info(f"Executed command: {self.command_to_run}")
        except Exception as e:
            self.log.error(f"Failed to execute command '{self.command_to_run}': {e}")
    
    def monitor_events(self):
        """Monitor keyboard events"""
        if not self.device_path:
            self.log.error("No device path set")
            return
            
        try:
            with open(self.device_path, 'rb') as device:
                self.log.info(f"Monitoring {self.device_path} for keycode {self.target_keycode}")
                
                while self.running:
                    # Use select to avoid blocking reads
                    ready, _, _ = select.select([device], [], [], 1.0)
                    
                    if not ready:
                        continue
                        
                    # Read event data
                    data = device.read(EVENT_SIZE)
                    if len(data) != EVENT_SIZE:
                        continue
                    
                    # Unpack event structure
                    if IS_64BIT:
                        # 64-bit: sec(8) + usec(8) + type(2) + code(2) + value(4) + padding(0)
                        _, _, event_type, code, value = struct.unpack('QQHHi', data)
                    else:
                        # 32-bit: sec(4) + usec(4) + type(2) + code(2) + value(4)
                        _, _, event_type, code, value = struct.unpack('IIHHi', data)
                    
                    # Check if this is our target key press
                    if (event_type == EV_KEY and 
                        code == self.target_keycode and 
                        value == KEY_PRESS):
                        
                        self.log.info(f"Target keycode {self.target_keycode} pressed!")
                        self.execute_command()
                        
        except PermissionError:
            self.log.error(f"Permission denied accessing {self.device_path}. Run as root or add user to input group.")
        except FileNotFoundError:
            self.log.error(f"Device {self.device_path} not found")
        except Exception as e:
            self.log.error(f"Error monitoring events: {e}")
    
    def start_monitoring(self):
        """Start monitoring in a background thread"""
        if self.running:
            self.log.warning("Monitor is already running")
            return False
            
        # Find keyboard device
        self.device_path = self.find_keyboard_device()
        if not self.device_path:
            self.log.error("Could not find keyboard device")
            return False
            
        self.running = True
        self.monitor_thread = threading.Thread(target=self.monitor_events, daemon=True)
        self.monitor_thread.start()
        
        self.log.info("Keyboard monitoring started")
        return True
    
    def stop_monitoring(self):
        """Stop monitoring"""
        if not self.running:
            return
            
        self.running = False
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.monitor_thread.join(timeout=2.0)
            
        self.log.info("Keyboard monitoring stopped")


# Example usage
if __name__ == "__main__":
    import time
    
    # Set up logging
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    
    # Create monitor instance
    monitor = KeyboardMonitor(target_keycode=425, command_to_run="echo 'Key 425 pressed!'")
    
    # Start monitoring
    if monitor.start_monitoring():
        try:
            # Keep the main thread alive
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("\nStopping monitor...")
            monitor.stop_monitoring()
    else:
        print("Failed to start monitoring")