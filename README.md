<p align="center">
  <img src="https://github.com/user-attachments/assets/6d383e82-8221-438b-9d6d-a19e998fcc59" alt="icon" width="80" style="vertical-align: middle;">
</p>

<h1 align="center">
  Div Acer Manager Max
</h1>

**Div Acer Manager Max** is a feature-rich Linux GUI utility for Acer laptops powered by the incredible [Linuwu Sense](https://github.com/0x7375646F/Linuwu-Sense) drivers. It replicates and expands on Acer’s NitroSense capabilities on Linux with full fan control, performance modes, battery optimization, backlight settings, and more — all wrapped in a modern Avalonia-based UI.

> ⚠️ **Project is under active development.**

![image](https://github.com/user-attachments/assets/f8c26491-8995-40b5-9d5d-8711bb574d10)

<h4 align="center">
Please star this repository to show support. It motivates me to make the project better for everyone
</h4>  


## ✨ Features

### ✅ Fully Implemented

* 🔋 **Performance / Thermal Profiles**
  Eco, Silent, Balanced, Performance, Turbo — automatically adjusted based on AC/battery status
  (e.g., Turbo hidden when on battery or unsupported)

* 🌡 **Fan Control**
  Manual and Auto fan speed modes
  Manual disabled automatically when in Quiet profile

* 💡 **LCD Override Setting**
  Direct control over LCD power behavior

* 🎨 **Keyboard Backlight Timeout Control**
  Customize the keyboard backlight timeout
  *(Color picker coming soon)*

* 🔊 **Boot Animation and Sound Toggle**
  Enable/disable Acer's startup animations and sounds

* 💻 **Live System Info Display**
  Shows real-time performance profile, fan settings, calibration state, and more

* 🧠 **Smart Daemon (Low Resource Use)**

  * Auto-detects feature support per device
  * Communicates with GUI in real-time
  * Lightweight: uses \~10MB RAM
  * Can run **independently** of GUI

* 🖥️ **Modern GUI**

  * Avalonia-based, clean and responsive
  * Dynamic UI hides unsupported features
  * Real-time feedback from daemon


## 🔧 Upcoming Features

* 🎨 RGB Keyboard Lighting Control
* 🔗 Update button linking directly to GitHub Releases


## 🖥️ Installation

> ✅ Simple CLI-based setup provided in the release.

1. **Download** the latest release from the [Releases](https://github.com/PXDiv/Div-Acer-Manager-Max/releases/) page.
2. **Unpack** the archive.
3. Open a terminal in the unpacked directory.
4. Run:

```bash
./Setup.sh
```

5. Choose an option from the menu:

   * `1` → Install
   * `2` → Uninstall
   * `3` → Reinstall/Update

6. And a Reboot

That’s it!


## 🖥️ Troubleshooting
You can check the logs at /var/log/DAMX_Daemon_Log.log

If you get UNKNOWN as Laptop type, try restarting (it happens somethings)
But if it still happenes that might mean the Drivers Installation failed, Make sure you have the approprite kernel headers to compile the drivers.

Please file a Issue in the issue corner and include the logs to get support and help the project grow


## ❤️ Powered by Linuwu

This project is built entirely on top of the [Linuwu Sense](https://github.com/0x7375646F/Linuwu-Sense) drivers — huge thanks to their developers for enabling hardware-level access on Acer laptops.



## Screenshots
![image](https://github.com/user-attachments/assets/069b6a74-ded7-4d0e-8bfe-0f556f3e70fe)
![image](https://github.com/user-attachments/assets/c76c6aa9-8ac2-4047-a8ba-2d943ac7d5f5)
![image](https://github.com/user-attachments/assets/42b7a328-eb05-42fb-847c-f7a34e751446)


## 🛠 Tech Stack

* **UI**: Avalonia (.NET Core)
* **Daemon**: Python
* **OS**: Linux (Acer laptops only)
* **Communication**: Unix Sockets



## 🤝 Contributing

* Report bugs or request features via GitHub Issues
* Submit pull requests to improve code or UI
* Help test on different Acer laptop models



## 📄 License

This project is licensed under the **GNU General Public License v3.0**.  
See the [LICENSE](LICENSE) file for details.
