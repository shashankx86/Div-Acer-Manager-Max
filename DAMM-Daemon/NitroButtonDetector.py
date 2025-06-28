#!/usr/bin/env python3
"""
KeyboardMonitor - Efficient keyboard shortcut detection for DAMX-Daemon
Monitors for specific key events (like code 425 - gaming key) with minimal resource usage
"""

import os
import threading
import time
import logging
from typing import Optional, Callable, List
from pathlib import Path

try:
    import evdev
    from evdev import InputDevice, categorize, ecodes
    EVDEV_AVAILABLE = True
except ImportError:
    EVDEV_AVAILABLE = False

log = logging.getLogger("KeyboardMonitor")

class KeyboardMonitor:
    """Efficient keyboard event monitor for DAMX daemon"""
    
    def __init__(self, callback: Optional[Callable] = None):
        self.callback = callback
        self.monitoring = False
        self.monitor_thread = None
        self.device = None
        self.target_keycode = 425  # Gaming key code
        
        if not EVDEV_AVAILABLE:
            log.warning("evdev not available. Keyboard monitoring disabled.")
            log.info("Install python3-evdev package to enable keyboard shortcuts")
    
    def _find_keyboard_device(self) -> Optional[InputDevice]:
        """Find the primary keyboard device efficiently"""
        if not EVDEV_AVAILABLE:
            return None
            
        try:
            devices = [evdev.InputDevice(path) for path in evdev.list_devices()]
            
            # Look for devices with keyboard capabilities
            for device in devices:
                capabilities = device.capabilities()
                
                # Check if device has key events and specifically our target key
                if ecodes.EV_KEY in capabilities:
                    key_codes = capabilities[ecodes.EV_KEY]
                    
                    # Look for common keyboard indicators or our specific key
                    has_letters = any(code in key_codes for code in range(16, 26))  # Q-P keys
                    has_target_key = self.target_keycode in key_codes
                    
                    # Prefer devices with our target key, fallback to keyboards with letters
                    if has_target_key or (has_letters and 'keyboard' in device.name.lower()):
                        log.info(f"Using keyboard device: {device.name} ({device.path})")
                        return device
            
            # Fallback: use first device with key capabilities
            for device in devices:
                if ecodes.EV_KEY in device.capabilities():
                    log.info(f"Fallback keyboard device: {device.name} ({device.path})")
                    return device
                    
        except Exception as e:
            log.error(f"Error finding keyboard device: {e}")
        
        return None
    
    def _monitor_events(self):
        """Monitor keyboard events in a separate thread"""
        if not EVDEV_AVAILABLE:
            return
            
        self.device = self._find_keyboard_device()
        if not self.device:
            log.error("No suitable keyboard device found")
            return
        
        log.info(f"Starting keyboard monitoring on {self.device.path}")
        
        try:
            # Grab device to prevent other applications from receiving events
            # Comment out the next line if you want other apps to still receive the key
            # self.device.grab()
            
            while self.monitoring:
                try:
                    # Use select with timeout for clean shutdown
                    if self.device.path in evdev.util.select([self.device], [], [], 1.0)[0]:
                        event = self.device.read_one()
                        if event:
                            self._process_event(event)
                except OSError:
                    # Device disconnected or other IO error
                    log.warning("Keyboard device disconnected, attempting to reconnect...")
                    time.sleep(2)
                    self.device = self._find_keyboard_device()
                    if not self.device:
                        log.error("Could not reconnect to keyboard device")
                        break
                        
        except Exception as e:
            log.error(f"Error in keyboard monitoring: {e}")
        finally:
            if self.device:
                try:
                    # self.device.ungrab()  # Uncomment if using grab()
                    self.device.close()
                except:
                    pass
    
    def _process_event(self, event):
        """Process individual keyboard events"""
        # Only process key events
        if event.type == ecodes.EV_KEY:
            # Check for our target keycode with key press (value 1)
            if event.code == self.target_keycode and event.value == 1:
                log.debug(f"Gaming key pressed (code {self.target_keycode})")
                if self.callback:
                    try:
                        self.callback()
                    except Exception as e:
                        log.error(f"Error in keyboard callback: {e}")
    
    def start_monitoring(self, callback: Optional[Callable] = None):
        """Start keyboard monitoring"""
        if not EVDEV_AVAILABLE:
            log.info("Keyboard monitoring not available - evdev not installed")
            return False
            
        if self.monitoring:
            log.warning("Keyboard monitoring already running")
            return True
        
        if callback:
            self.callback = callback
        
        if not self.callback:
            log.warning("No callback provided for keyboard events")
            return False
        
        self.monitoring = True
        self.monitor_thread = threading.Thread(target=self._monitor_events, daemon=True)
        self.monitor_thread.start()
        
        log.info("Keyboard monitoring started")
        return True
    
    def stop_monitoring(self):
        """Stop keyboard monitoring"""
        if not self.monitoring:
            return
        
        log.info("Stopping keyboard monitoring...")
        self.monitoring = False
        
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.monitor_thread.join(timeout=2)
        
        log.info("Keyboard monitoring stopped")
    
    def set_target_keycode(self, keycode: int):
        """Change the target keycode to monitor"""
        self.target_keycode = keycode
        log.info(f"Target keycode changed to {keycode}")
    
    def is_available(self) -> bool:
        """Check if keyboard monitoring is available"""
        return EVDEV_AVAILABLE
    
    def get_device_info(self) -> dict:
        """Get information about the current keyboard device"""
        if not self.device:
            return {}
        
        return {
            "name": self.device.name,
            "path": self.device.path,
            "vendor": getattr(self.device.info, 'vendor', 'Unknown'),
            "product": getattr(self.device.info, 'product', 'Unknown'),
        }


def test_keyboard_monitor():
    """Test function for keyboard monitoring"""
    def on_gaming_key():
        print("Gaming key pressed! This would launch DAMX GUI.")
    
    monitor = KeyboardMonitor()
    
    if not monitor.is_available():
        print("evdev not available. Install with: sudo apt install python3-evdev")
        return
    
    print("Starting keyboard monitoring test...")
    print("Press the gaming key (code 425) to test...")
    print("Press Ctrl+C to stop")
    
    monitor.start_monitoring(on_gaming_key)
    
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nStopping...")
        monitor.stop_monitoring()


if __name__ == "__main__":
    # Enable debug logging for testing
    logging.basicConfig(level=logging.DEBUG)
    test_keyboard_monitor()