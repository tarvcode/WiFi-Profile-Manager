# WiFi Profile Manager

WinForms GUI for managing saved Windows WiFi profiles. No tray, no background process — closes on Exit.

## Features

- Lists saved WiFi profiles, sorted by connection priority (highest first)
- Columns: Profile Name, AutoConnect (Yes/No), Priority
- Checkbox multi-select to Delete profiles
- Checkbox multi-select to Enable/Disable Auto-Connect (`netsh wlan set profileparameter ... ConnectionMode=auto|manual`)
- Move Up / Move Down buttons to reorder a profile's connection priority (`netsh wlan set profileorder`) — enabled only when exactly one profile is selected
- Generate WLAN Report button — runs `netsh wlan show wlanreport` and opens the resulting `wlan-report-latest.html` in your default browser
- Auto-detects the WiFi interface name (`netsh wlan show interfaces`) instead of hardcoding "Wi-Fi"

## Requirements

- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- Some actions (Enable/Disable Auto-Connect, Move Up/Down, Generate WLAN Report) may require running as Administrator, especially for all-user profiles. If an action fails, the app shows the netsh error and suggests re-running elevated.

## Build (single exe, self-contained, no .NET install needed on target machine)

From the `WifiProfileManager` folder:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none --output ./dist
```

Output: `./dist/WifiProfileManager.exe` — single file.

## Build (smaller exe, requires .NET 8 runtime on target machine)

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none --output ./dist
```

## Run without building (dev/test)

```
dotnet run
```

## Notes

- No admin rights required for `netsh wlan show profiles` / `delete profile`.
- Admin rights typically required for `set profileparameter`, `set profileorder`, and `show wlanreport`.
- WLAN report is always written to `C:\ProgramData\Microsoft\Windows\WlanReport\wlan-report-latest.html` — the app opens that fixed path after generation.
