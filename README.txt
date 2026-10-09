CURSORCLICK NATIVE

CursorClick.exe is the main application. It is a lightweight native Windows
application; PowerShell is retained only as a legacy fallback and reference.

START
  Run 00_START_HERE.bat or CursorClick.exe.

CONTROL
  Double-click the blue tray icon to open settings.
  Right-click it to enable/disable effects, select a preset, configure
  Windows startup, see CPU/RAM usage, or exit.
  Ctrl+Alt+C temporarily enables/disables effects.

EFFECTS
  Left click: blue rings.
  Right click: red rings.
  Middle click: green rings.
  Double click: an extra gold wave.
  Holding a mouse button produces periodic waves.

SETTINGS
  Colors, size, opacity, duration, wave count, click rate limit, optional
  glow, fullscreen pause, high-CPU pause and threshold, hold/double-click
  effects, Windows startup, and Minimal/Normal/Bright presets.
  Click a color sample to use the visual Windows color picker.
  Available shapes: Circle, Dot, Cross, Wave, and Sparks. Each mouse button
  has its own color and shape. Size, opacity, and duration have sliders.
  Preview shows the current effect directly above the settings window.
  Optional permanent cursor glow has its own color, size, and brightness.
  It follows mouse events without a permanent polling loop and hides over the
  settings window, excluded applications, fullscreen apps, and high CPU load.
  Settings can be reset, exported to XML, or imported from XML.
  Exclusions accept comma-separated process names, for example:
  game.exe, photoshop.exe

OTHER
  Only one instance can run. Starting CursorClick.exe again opens settings.
  Per-monitor screen coordinates and DPI awareness are enabled.
  Effects use a per-pixel-alpha layered window for clean fading and glow.
  Settings are saved in %APPDATA%\CursorClick\settings.xml.
  Crash details are saved in %APPDATA%\CursorClick\crash.log and the app
  attempts to restart after a UI failure.

STOP
  Choose Exit from the tray menu or run 01_STOP.bat.

REBUILD
  Stop CursorClick, then run BUILD.ps1. No downloaded SDK is required.

PACKAGE / INSTALL
  PACK.ps1 creates dist\CursorClick-Portable.zip.
  INSTALL.ps1 installs into %LOCALAPPDATA%\CursorClick and creates a Start
  Menu shortcut. UNINSTALL.ps1 removes the application but keeps settings.
