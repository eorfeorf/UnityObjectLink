# Security model

日本語版: [Security-ja.md](Security-ja.md)

Custom URI activation is untrusted input. Unity Object Link treats both the OS handler and Unity receiver as validation boundaries.

## Guarantees

- Only the `select` action and URI version 1 are accepted.
- Exactly `v`, `project`, and `object` are allowed; missing, duplicate, and unknown parameters fail closed.
- Input lengths, percent encoding, scheme syntax, and Project ID syntax are bounded and validated twice.
- Project IDs cannot contain separators or `..`, so they cannot escape the fixed instance directory.
- A URI cannot select an executable, command, script, method, or local filesystem path.
- The OS handler writes only under `%LOCALAPPDATA%\UnityObjectLink` on Windows or `~/Library/Application Support/UnityObjectLink` on macOS.
- A request is accepted only when the exact scheme/Project ID heartbeat is at most 15 seconds old.
- Temporary-to-request rename prevents Unity from reading a partially written request.
- Unity rejects empty, stale (over 60 seconds), oversized, duplicate, malformed UTF-8, and malformed URI requests, then attempts to delete them.
- The package never opens or saves a Scene in response to a link.

## Registration ownership

Windows uninstall removes `HKCU\Software\Classes\<scheme>` only when its command names the Unity Object Link handler. It refuses to remove a scheme owned by another application. macOS stores one generated helper under the fixed product directory for each validated scheme and unregisters that exact bundle.

## Privacy

Generated links contain Project ID and Unity `GlobalObjectId`; they do not contain machine paths, user names, asset names, Scene names, or source content. Heartbeats contain scheme, Project ID, process ID, version, and timestamp. Requests remain local to the current user profile and are deleted after handling.

## Residual platform behavior

Operating systems and client applications may display a confirmation prompt or refuse to activate custom schemes. OS foreground restrictions can also prevent Unity from becoming the frontmost application; the package still focuses the relevant Unity window and selects the object when delivery succeeds.
