# Gothic Clock · Bleach & Cyberpunk 2077 Edition

A lightweight, zero-dependency Windows desktop clock HUD combining authentic Gothic Old English blackletter typography with high-tech Cyberpunk 2077 optics and Bleach spiritual pressure (Reiatsu) aura shaders. 

Features an authentic **MAINFRAME** control interface with dual-channel navigation tabs (`[ Settings ]` and `[ Mods ]`), hardware-accelerated vector reticle brackets, live telemetry capsule HUD, and anti-hide immunity (`Win+D` / `Ctrl+D` proof).

---

## Key Features

### 1. Vector Cyberpunk HUD Reticles & Telemetry Capsule
- **Chamfered 45° Vector Brackets:** Replaces plain font brackets with hardware-accelerated WPF vector paths featuring 45° corner cuts, rangefinder ticks, and neon glow that dynamically mirrors the active theme.
- **Cyber Telemetry Capsule Bar:** Status capsule displaying real-time telemetry (`SYS.2077 // NIGHT_CITY // SYNC_OK`) with glowing pulse beacon.
- **Ornate Gothic Filigree Divider:** Center diamond and dual-gradient spirit lines between time and calendar telemetry.
- **Smoked Cyber-Glass Plate:** Optional translucent acrylic backplate with rounded corners and border outline.

### 2. Dual-Channel MAINFRAME Interface
- **Crimson Chamfered Frame:** Precision 45° armor notches with glowing drop-shadow border.
- **Pill Navigation Tabs:**
  - `[ Settings ]`: Chrono Core scale, line spacing, time circuits (hours, rolling seconds, 12h AM/PM), calendar telemetry (day of week, date formatting), display layer depth, coordinates snap presets, and Move Mode.
  - `[ Mods ]`: Vector HUD reticle brackets, telemetry capsule bar, gothic diamond filigree, smoked acrylic plate, rolling motion physics, colon pulse modes, 10 Reiatsu & Neon aura themes, and system daemon controls.
- **Cyber Diamond Toggles:** Crisp diamond indicators (`◆` active neon cyan, `◇` inactive muted slate).
- **Holographic CRT De-Rez Shutdown Animation:** Horizontal squeeze and vertical collapse into a central electron beam flare on window close.

### 3. Display Layer & Anti-Hide Immunity
- **Always-On-Top HUD Mode (Default):** Floats above full-screen windows with `WS_EX_TRANSPARENT` click-through pass-through, completely immune to `Win+D` and `Ctrl+D` minimize gestures.
- **Desktop Layer Mode:** Alternative mode pinned directly to the wallpaper layer (`HWND_BOTTOM`) above wallpaper and icons.
- **Move Mode:** Dedicated drag-and-drop mode allowing free positioning anywhere on multi-monitor setups without interfering with normal window clicks.

### 4. 10 Reiatsu & Neon Aura Themes
| Theme Key | Name | Visual Archetype |
|:---|:---|:---|
| `NightCity` | **Night City** | High-contrast neon cyan optical HUD (`#00F0FF`) |
| `Cyberpunk` | **Cyberpunk 2077** | High-voltage radiant amber (`#FCEE0A`) |
| `Getsuga` | **Getsuga Tensho** | Pure bone white with crimson blood Reiatsu (`#E11D48`) |
| `Bankai` | **Bankai Hollow** | Stark pure white with void obsidian shadow (`#FAFAFF`) |
| `Hollow` | **Hollow Mask** | Bone white with dark blood crimson (`#991B1B`) |
| `Ichigo` | **Ichigo Soul** | Radiant light with flame Reiatsu orange (`#EA580C`) |
| `Quincy` | **Quincy Blut** | Spirit silver with sacred Quincy blue (`#0284C7`) |
| `Cero` | **Cero Blast** | Spectral white with Ulquiorra emerald (`#10B981`) |
| `Senbonzakura` | **Senbonzakura** | Byakuya cherry blossom Reiatsu pink (`#F43F5E`) |
| `Moonlight` | **Moonlight Silver** | Classic gothic moonlight silver (`#94A3B8`) |

---

## Architecture & Source Code

- **Zero External Dependencies:** Built with pure C# and WPF on .NET Framework 4.0+, which is native to all modern Windows installations.
- **No Heavy Runtimes:** No Electron, no Node.js, and no WebView2 needed. Uses less than 40MB memory.
- **Direct Windows API Integration:** Custom `WndProc` hook with `WM_WINDOWPOSCHANGING` enforcement for topmost persistence and click-through transparency.

---

## Building from Source

No installation of Visual Studio or third-party SDK is required. Simply run:

```cmd
build.bat
```

This invokes Windows native `csc.exe` to compile `GothicClock.exe` with embedded icon and full WPF references.

---

## Installation & Uninstallation

- **Auto-Start:** Enable "Start Daemon Silently on Windows Boot" in the MAINFRAME to auto-launch on startup.
- **Clean Uninstall:** Click `[ UNINSTALL ]` inside the MAINFRAME or execute `uninstall.bat` to completely remove all application files, shortcuts, and registry entries.
