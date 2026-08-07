# Testing

日本語版: [Testing-ja.md](Testing-ja.md)

The package uses Unity Test Framework EditMode tests only. `TestProject~` targets Unity 6 and `TestProject2022~` targets the supported lower bound, Unity 2022.3 LTS. Both reference the package at the repository root and mark it testable.

Example commands on Windows:

```powershell
& '<Unity 6 path>\Editor\Unity.exe' -batchmode -nographics `
  -projectPath '<repository>\TestProject~' `
  -runTests -testPlatform EditMode -testResults '<output>\unity6.xml' -logFile '<output>\unity6.log'

& '<Unity 2022.3 path>\Editor\Unity.exe' -batchmode -nographics `
  -projectPath '<repository>\TestProject2022~' `
  -runTests -testPlatform EditMode -testResults '<output>\unity2022.xml' -logFile '<output>\unity2022.log'
```

Coverage includes:

- canonical URI creation, percent decoding, version, scheme, Project ID, parameter cardinality, and length limits;
- traversal rejection and atomic local writes;
- heartbeat age and clock-skew limits;
- inbox TTL, size, duplicate, malformed UTF-8, deletion, and injectable clock/filesystem boundaries;
- assets, sub-assets, Prefab children, saved loaded Scene objects, unsaved Scenes, unloaded Scenes, and deleted targets;
- strict `GlobalObjectId` parsing and an injectable selection boundary.
- a Windows-only full round trip from native OS URI activation through the running receiver to the exact Unity selection.

## OS protocol E2E

The platform scripts use a unique temporary scheme and Project ID, confirm that a traversal request is rejected, exercise the real OS URL dispatch path, verify the delivered request, and remove their registration and files in a `finally`/trap cleanup path.

Windows:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\Platform\WindowsProtocolE2E.ps1
```

macOS:

```bash
/bin/bash Tests/Platform/macOSProtocolE2E.sh
```

Run these from the package root. A passing run prints `E2E_PASS=True`.

The URI validation, heartbeat, and atomic inbox portion of the macOS handler can also be checked without Launch Services. This portable test adapts only the BSD `stat` call when it runs under Linux or Git Bash:

```bash
/bin/bash Tests/Platform/macOSDispatchLogicTest.sh
```

The macOS helper layout, injective bundle identifier encoding, AppleScript escaping, plist operations, registration calls, permissions, status, uninstall, and cleanup can be checked with stubbed system commands on any Bash environment:

```bash
/bin/bash Tests/Platform/macOSInstallerLogicTest.sh
```

This installer-logic test does not substitute for compiling the application with the real `osacompile`, registering it with Launch Services, and opening its URI on macOS.

On 2026-08-01, 52/52 EditMode tests passed with Unity 2022.3.62f1 and Unity 6000.3.20f1 on Windows 11. The Windows tests passed per-user install/status, preservation of a foreign registration, real OS URI activation, atomic inbox delivery, selection in the running Unity Editor, uninstall, and cleanup with temporary isolated schemes. macOS and third-party link clients still require their platform-specific manual E2E checks recorded in [Compatibility](Compatibility.md).
