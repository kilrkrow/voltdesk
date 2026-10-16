# VoltDesk

Windows tray utility for switching power plans and toggling desktop icons with keyboard shortcuts.

![VoltDesk settings window showing keyboard shortcuts and power plan configuration](docs/screenshots/hero.png)

Switching Windows power plans takes four clicks minimum, and toggling desktop icon visibility is buried even deeper. VoltDesk puts those controls on global hotkeys you define once, then lives quietly in the system tray.

## What it does

- **Power plan shortcuts** — assign a global hotkey to each Windows power plan; press it anywhere to switch instantly
- **Desktop icon toggle** — show or hide all desktop icons with a single shortcut (default: `Ctrl+Shift+D`)
- **Tray integration** — right-click the tray icon for quick access; closing the settings window keeps VoltDesk running
- **Start with Windows** — optional autostart via the registry; no installer required for basic use
- **Start Menu shortcut** — optional pin under All apps
- **No admin rights needed** — power plan switching uses the Windows API directly

## Quick start

```bash
dotnet run
```

.NET 10 or later. The app appears in the system tray immediately. Right-click the icon for settings.

To build a standalone executable:

```bash
dotnet publish -c Release -r win-x64
```

## Building

Requirements: .NET 10.0 SDK, Windows 10 or 11.

```bash
dotnet build
dotnet publish -c Release -r win-x64
```

An Inno Setup installer script (`VoltDeskInstaller.iss`) is included for packaging.

## License

[GPL-3.0](LICENSE)

## Feedback

Questions, ideas and bug reports are welcome in [Issues](https://github.com/kilrkrow/voltdesk/issues).

---

Built by Guy Schamp · [Fox & Dove](https://foxanddoveconsulting.com) · more tools at [github.com/kilrkrow](https://github.com/kilrkrow)
