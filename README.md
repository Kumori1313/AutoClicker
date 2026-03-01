# AutoClicker

> A lightweight Windows desktop auto-clicker with a clean WPF interface, configurable hotkeys, multi-location support, and INI-based configuration persistence.

## Overview

AutoClicker is a Windows GUI application that automates mouse clicks at a user-defined interval. It supports left, right, and middle mouse buttons, can click at the current cursor position or cycle through a saved list of screen coordinates, and can run indefinitely or stop after a fixed number of clicks. All settings are saved to an INI file and restored automatically on next launch. The toggle hotkey works globally — the application does not need to be in focus to start or stop clicking.

The tool is aimed at developers, testers, and power users who need reliable, scriptable click automation without external dependencies.

**Key features:**

- Configurable click interval (hours, minutes, seconds, milliseconds)
- Left, right, and middle mouse button support
- Click at current cursor position or at one or more predetermined screen coordinates
- Indefinite or fixed-count iteration modes
- Rebindable global toggle hotkey (default: `Insert`)
- INI file configuration with save/load and a Browse dialog to choose the file path
- Multi-monitor support via Windows virtual screen coordinate mapping
- Self-contained single-file executable — no installer or runtime required

## Prerequisites

| Requirement | Details |
|---|---|
| Operating System | Windows 10 (build 19041 / 2004) or later |
| Architecture | x64 |
| .NET SDK (build only) | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| .NET Runtime (run only) | Not required — the published binary is self-contained |

> If you only want to run the application, download the pre-built executable from the Releases page. No .NET installation is needed on the target machine.

## Building from Source

### 1. Clone the repository

```bash
git clone <repository-url>
cd AutoClicker
```

### 2. Build (debug)

```bash
dotnet build
```

The debug output is placed in `bin\Debug\net10.0-windows\`.

### 3. Run directly

```bash
dotnet run
```

### 4. Publish a self-contained single-file executable

```bash
dotnet publish -c Release -r win-x64
```

The output executable is placed in:

```
bin\Release\net10.0-windows\win-x64\publish\AutoClicker.exe
```

This binary bundles the .NET runtime and all native libraries. It can be copied to any Windows 10+ x64 machine and run without any installation.

## Usage

Launch `AutoClicker.exe`. The main window provides all configuration options in a single scrollable panel.

### Click Interval

Enter values in the **Hours**, **Minutes**, **Seconds**, and **Milliseconds** fields. The minimum effective interval is 1 ms.

```
Hours: 0   Minutes: 0   Seconds: 1   Milliseconds: 0
```

This example clicks once every second.

### Mouse Button

Select **Left Click**, **Right Click**, or **Middle Click** from the drop-down.

### Click Location

| Mode | Behavior |
|---|---|
| Current Mouse Position | Clicks wherever the cursor is at the time of each click |
| Predetermined Locations | Cycles through a saved list of screen coordinates |

To add a predetermined location:

1. Select **Predetermined Locations**.
2. Click **Choose Location** — the button toggles on and a hint appears.
3. Click anywhere on the screen (including outside the application window). The coordinate is recorded and added to the list.
4. Repeat for additional locations. Click **Choose Location** again to stop.
5. To remove a location, select it in the list and click **Remove Selected**.

When multiple locations are configured the clicker cycles through them in order, wrapping back to the first after the last.

### Iterations

| Mode | Behavior |
|---|---|
| Repeat Indefinitely | Runs until manually stopped |
| Set Number of Clicks | Stops automatically after the specified click count |

### Toggle Hotkey

The default hotkey is `Insert`. Press the hotkey at any time — even when another application is focused — to start or stop clicking.

**To rebind the hotkey:**

1. Click **Rebind**.
2. Press the new key. The key name updates immediately.

Supported keys include `Insert`, `F1`–`F12`, letter keys (`A`–`Z`), and digit keys (`0`–`9`).

### Starting and Stopping

- Click the **Start** button in the application, or
- Press the configured toggle hotkey from anywhere on the system.

The button turns red and the status bar shows **Running...** while active. When running in fixed-count mode the status bar shows the current progress (`Clicks: N / Total`).

## Configuration Files

Settings are saved to and loaded from `.ini` files.

### Default file path

```
%APPDATA%\AutoClicker\autoclicker_save.ini
```

The path is remembered across sessions via the Windows registry key `HKCU\Software\AutoClicker\ConfigFilePath`. Use the **Browse...** button to point to a different file.

### Saving and loading

| Button | Action |
|---|---|
| **Save Config** | Opens a Save dialog and writes current settings to the chosen `.ini` file |
| **Load Config** | Opens an Open dialog and applies settings from the chosen `.ini` file |

Settings are also saved automatically whenever the toggle hotkey is rebound.

### INI file format

```ini
[Interval]
Hours=0
Minutes=0
Seconds=1
Milliseconds=0

[Locations]
UseMousePosition=True
Count=2
Location0=640,480
Location1=1280,720

[Settings]
IsIndefinite=True
IterationCount=10
ToggleKey=45
MouseButton=Left
```

| Key | Type | Description |
|---|---|---|
| `Hours` | int | Hours component of the interval |
| `Minutes` | int (0–59) | Minutes component of the interval |
| `Seconds` | int (0–59) | Seconds component of the interval |
| `Milliseconds` | int (0–999) | Milliseconds component of the interval |
| `UseMousePosition` | bool | `True` = click at cursor; `False` = use location list |
| `Count` | int | Number of saved locations |
| `Location{N}` | `x,y` | Screen coordinate of the Nth location (0-indexed) |
| `IsIndefinite` | bool | `True` = run forever; `False` = stop after `IterationCount` clicks |
| `IterationCount` | int (≥1) | Number of clicks before auto-stopping |
| `ToggleKey` | uint | Virtual-key code of the toggle hotkey (e.g. `45` = Insert) |
| `MouseButton` | enum | `Left`, `Right`, or `Middle` |

Lines beginning with `;` or `#` are treated as comments and ignored.

## Project Structure

```
AutoClicker/
├── App.xaml                  Application entry point and resource dictionary
├── App.xaml.cs               Minimal Application subclass
├── MainWindow.xaml           WPF layout — all UI controls
├── MainWindow.xaml.cs        UI event handlers and service wiring
├── AutoClicker.csproj        MSBuild project file (targets net10.0-windows, WPF, x64)
├── Helpers/
│   └── NativeMethods.cs      P/Invoke declarations (SendInput, hooks, screen metrics)
│                             MouseButton enum and coordinate conversion helpers
├── Models/
│   └── ClickerConfiguration.cs  All settings as an INotifyPropertyChanged model
│                                ClickLocation (X, Y) record type
└── Services/
    ├── ClickerService.cs     Background thread click loop using Stopwatch for precision
    ├── ConfigurationService.cs  INI file serialiser / deserialiser
    └── HotkeyService.cs      Low-level global keyboard hook (WH_KEYBOARD_LL)
```

## Development

### Requirements

- .NET 10 SDK
- Visual Studio 2022 (17.12+) or JetBrains Rider, or any editor with the C# Dev Kit extension

### Building in Visual Studio

Open `AutoClicker.csproj` directly or open the folder in Visual Studio. Press `F5` to build and run in debug mode.

### Building on the command line

```bash
# Debug build and run
dotnet run

# Release build
dotnet build -c Release

# Self-contained single-file publish for distribution
dotnet publish -c Release -r win-x64 --self-contained true
```

### Notes for contributors

- The project uses `AllowUnsafeBlocks` because of the `StructLayout` P/Invoke interop types in `NativeMethods.cs`. No unsafe pointer arithmetic is used in application logic.
- `ClickerService` runs its click loop on a high-priority background thread and uses `Stopwatch`-based spin-waiting for sub-15 ms timing accuracy.
- `HotkeyService` installs a system-wide `WH_KEYBOARD_LL` hook rather than `RegisterHotKey`, so the hotkey functions even in games and full-screen applications without requiring a modifier key.
- All UI updates from background threads are marshalled back through the WPF `Dispatcher`.
