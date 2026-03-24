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
\`\`\`bash
dotnet build
dotnet publish -c Release -r win-x64
\`\`\`

### Run
\`\`\`bash
dotnet run
\`\`\`

## Project Structure

- `Form1.cs` — Main UI form
- `TrayApplicationContext.cs` — Tray icon integration
- `PowerManager.cs` — Power management functionality
- `HotkeyManager.cs` — Keyboard hotkey handling
- `Configuration.cs` — Settings management
- `VoltDeskInstaller.iss` — Inno Setup installer configuration

## License

[Specify your license here]

## Author

Created by Guy Schamp
