# VoltDesk

A Windows desktop application providing quick access to system power controls, settings, and utilities via a tray icon.

## Features

- System tray integration
- Quick access to common settings
- Configurable hotkeys
- Power management controls
- Lightweight and fast

## Building

### Requirements
- .NET 10.0 or later
- Windows 10/11

### Build
```bash
dotnet build
dotnet publish -c Release -r win-x64
```

### Run
```bash
dotnet run
```

## Project Structure

- `Form1.cs` — Main UI form
- `TrayApplicationContext.cs` — Tray icon integration
- `PowerManager.cs` — Power management functionality
- `HotkeyManager.cs` — Keyboard hotkey handling
- `Configuration.cs` — Settings management
- `VoltDeskInstaller.iss` — Inno Setup installer configuration

## License

This project is licensed under the **GNU General Public License v3.0 (GPLv3)**. See the LICENSE file for full details.

In summary: You are free to use, modify, and distribute this software, provided that any derivative works are also licensed under GPLv3.

For the full license text, visit: https://www.gnu.org/licenses/gpl-3.0.txt

## Author

Created by Guy Schamp
