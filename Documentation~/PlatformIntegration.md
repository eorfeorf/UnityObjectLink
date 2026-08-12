# Platform integration

日本語版: [PlatformIntegration-ja.md](PlatformIntegration-ja.md)

## Common layout

```text
UnityObjectLink/
  bin/
  instances/<scheme>/<project-id>/
    heartbeat.json
    inbox/
      <unique-id>.request
```

The root is `%LOCALAPPDATA%` on Windows and `~/Library/Application Support` on macOS. Handler scripts derive every path from validated URI values. The heartbeat payload is informational; scripts do not trust it to supply an inbox path.

## Windows

`UnityObjectLinkProtocol.ps1 install` copies itself to the stable `bin` path, records a small ownership marker, and writes `HKCU\Software\Classes\<scheme>`. The open command uses Windows PowerShell with `-NoProfile` and passes `%1` only to the script's `dispatch` command. `status` reports whether the registration is missing, owned by Unity Object Link, or owned by another application. `uninstall` refuses to remove another application's registration and removes the stable script only when no tracked schemes remain.

Manual commands:

```powershell
./UnityObjectLinkProtocol.ps1 -Command install -Scheme unity-object-link
./UnityObjectLinkProtocol.ps1 -Command status -Scheme unity-object-link
./UnityObjectLinkProtocol.ps1 -Command dispatch -Uri 'unity-object-link://select?...'
./UnityObjectLinkProtocol.ps1 -Command uninstall -Scheme unity-object-link
```

## macOS

`unity-object-link-protocol.sh install` copies itself to the stable `bin` path, compiles a minimal AppleScript URL handler with the system `osacompile`, adds `CFBundleURLTypes`, and registers the helper with Launch Services. Xcode and third-party runtimes are not required.

Manual commands:

```bash
./unity-object-link-protocol.sh install unity-object-link
./unity-object-link-protocol.sh status unity-object-link
./unity-object-link-protocol.sh dispatch '' 'unity-object-link://select?...'
./unity-object-link-protocol.sh uninstall unity-object-link
```

## Foreground behavior

After resolution, assets focus the Project window. Scene objects attempt to focus the Hierarchy and fall back to the last Scene view. `PingObject` and selection are deterministic, but the OS may deny a request to bring another application in front; foreground activation is therefore best-effort.
