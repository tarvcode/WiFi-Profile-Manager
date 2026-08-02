# WiFi Profile Manager

WinForms GUI for viewing/deleting saved Windows WiFi profiles. No tray, no background process — closes on Exit.

## Requirements

- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

## Build (single-file exe, self-contained, no .NET install needed on target machine)

From the `WifiProfileManager` folder:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output exe:

```
bin\Release\net8.0-windows\win-x64\publish\WifiProfileManager.exe
```

## Build (smaller exe, requires .NET 8 runtime on target machine)

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## Run without building (dev/test)

```
dotnet run
```

## Notes

- No admin rights required for `netsh wlan show profiles` / `delete profile`.
- Checkboxes support multi-select delete.
- Refresh button re-reads profiles after changes.
