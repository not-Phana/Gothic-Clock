# Gothic Clock

A lightweight, zero-dependency Windows desktop clock widget with authentic gothic blackletter typography, pinned permanently to the Windows desktop wallpaper layer (immune to Win+D), paired with a dedicated dark-themed Control Panel application.

## Key Features

- **Custom Dark Control Panel:** Sleek borderless dark window with custom title bar, minimalist minimize/close buttons, smooth dark slider, and transparent scrollbars.
- **Main Desktop Layer:** Pinned directly to the Windows desktop background (`HWND_BOTTOM` with `WS_EX_NOACTIVATE`). Never hovers over active applications.
- **Win+D / Show Desktop Immune:** Survives minimizing all windows or pressing Win+D without disappearing.
- **Size & Scaling Controls:** Quick preset sizes (Tiny 50%, Small 70%, Normal 85%, Large 120%) and a continuous scale slider.
- **Desktop Positioning:** Instant snap presets (Top-Right, Top-Left, Bottom-Right, Bottom-Left, Center) plus an interactive Move Mode for mouse dragging.
- **Silent Windows Startup:** Optional one-click toggle to launch silently on Windows boot via registry.
- **Complete Uninstaller:** One-click uninstallation from the Control Panel or via `uninstall.bat` to cleanly wipe shortcuts, registry keys, and files.

## Project Structure

- `GothicClock.cs` - Pure C# WPF source code.
- `app.ico` - High-resolution embedded clock icon.
- `build.bat` - One-click compilation script using native Windows `csc.exe`.
- `uninstall.bat` - Standalone uninstallation cleanup script.

## Building from Source

No external tools or SDKs are required. Simply run:

```cmd
build.bat
```

This compiles `GothicClock.exe` using the .NET Framework C# compiler (`csc.exe`) built directly into Windows.
