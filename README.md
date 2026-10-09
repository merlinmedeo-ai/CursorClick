# CursorClick

A lightweight Windows app that displays visual effects where you click and a persistent glow around the pointer. It runs in the system tray and does not poll the mouse position in a loop: button and movement events are received through a low-level system mouse hook.

## Download and run

Open the [latest release](../../releases/latest) and download `CursorClick-Portable.zip`. Extract the archive and run `CursorClick.exe`. No installation is required for normal use.

The archive also includes `INSTALL.ps1`, `UNINSTALL.ps1`, and this guide. Installation copies the app to `%LOCALAPPDATA%\CursorClick` and creates a Start Menu shortcut. Uninstallation removes the app and shortcut but keeps your settings.

## Features

- Blue, red, and green effects for the left, right, and middle mouse buttons.
- Five shapes: circle, dot, cross, wave, and sparkles. Shape and color can be configured separately for each button.
- An additional golden wave on double-click.
- Repeating effects while a mouse button is held.
- Persistent pointer glow with configurable color, size, and brightness.
- Controls for effect size, opacity, and animation duration.
- Minimal, Normal, and Bright presets, plus preview and reset options.
- Import and export settings as XML.
- Effect rate limiting for rapid clicks.
- Automatic pause in fullscreen apps and under high CPU load. The Windows desktop and taskbar are not treated as fullscreen apps.
- Per-process exclusions, such as `game.exe, photoshop.exe`.
- Global `Ctrl+Alt+C` shortcut to toggle effects on and off.
- Single-instance behavior: launching the app again opens its settings window.
- CPU load and memory usage indicators in the tray menu.
- Optional launch at Windows startup.
- English, Ukrainian, and Russian interface. On first launch, the app follows the Windows language; you can change it in settings.
- DPI awareness and support for coordinates across multiple monitors.
- Smooth per-pixel alpha graphics that fade without a black afterimage.

## Controls

- Double-click the CursorClick tray icon to open settings.
- Right-click the icon for the menu: toggle effects, choose a preset, configure startup, view resource usage, or exit.
- Press `Ctrl+Alt+C` to quickly toggle effects.
- Click a color swatch in settings to choose a color from the Windows palette.

## Settings and data

Settings are stored in `%APPDATA%\CursorClick\settings.xml`, in a separate folder for each Windows user. The portable archive does not include user settings, so the app creates a new settings file the first time it runs on another PC. If needed, a crash recovery log is stored alongside it as `crash.log`.

Startup configuration adds an entry only to the current user's Windows registry and copies the executable to `%LOCALAPPDATA%\CursorClick\CursorClick.exe`. Administrator privileges are not required. If you move the app to another PC, enable startup again from the tray menu or settings.

## Requirements and portability

- Windows 10 or Windows 11.
- .NET Framework 4.x, usually already installed as a Windows component on current Windows 10/11 systems.
- No internet connection or external libraries are required at runtime.

The app is built as AnyCPU and uses built-in WinForms, GDI+, XML, and Win32 APIs. It is not intended for macOS or Linux. The executable is not digitally signed, so Windows SmartScreen may show a warning the first time you run a file downloaded from the internet.

## Build from source

To build on Windows, you need Windows PowerShell and the .NET Framework `csc.exe` compiler (usually included with the Windows/.NET Framework Developer Pack). The .NET SDK, NuGet, and downloaded dependencies are not required.

```powershell
.\BUILD.ps1
```

This creates `CursorClick.exe` and the `CursorClick.ico` icon. To package a ZIP with the app and installation scripts, run:

```powershell
.\PACK.ps1
```

The archive will be created at `dist\CursorClick-Portable.zip`.

## Project structure

- `CursorClickApp.cs` — app, tray icon, settings, system mouse hook, effects, and window management.
- `BUILD.ps1` — generates the icon and compiles the `.exe`.
- `PACK.ps1` — builds the portable ZIP.
- `INSTALL.ps1`, `UNINSTALL.ps1` — per-user installation and removal scripts.
- `CursorClick.exe` — ready-to-run application.
- `dist/CursorClick-Portable.zip` — portable distribution package.
- `CursorClick.ps1`, `CursorClick_ORIGINAL_V7.ps1` — earlier PowerShell versions retained for compatibility and reference.

## Troubleshooting

If effects stop, check their state in the tray menu and try the `Ctrl+Alt+C` shortcut. Automatic pause may be expected while a fullscreen app is active or CPU load is high; both options can be disabled in settings. To close the app, choose Exit from the tray icon menu.

## License

No license has been specified for this project yet. Until one is added, copyright remains with the author; do not assume the source can be freely reused or redistributed beyond what applicable law permits.
