# Unity Object Link

Unity Object Link creates stable links to Unity assets, sub-assets, Prefab objects, and objects in saved, loaded Scenes. Opening a link selects and pings the object in the matching running Unity Editor project.

The package is Editor-only and supports Unity 2022.3 LTS or newer. Windows and macOS protocol handlers run entirely in the current user's account and require no administrator privileges or additional runtime.

> [!IMPORTANT]
> `com.example.unity-object-link`, the repository URL, and the license are publication placeholders. Replace them before publishing this repository. The author is `eorfeorf`.

## Install

This repository is a UPM package at its root. During local development, choose **Window > Package Management > Package Manager > + > Add package from disk** and select `package.json`. After publication, use **Add package from git URL** with the repository URL.

Then:

1. Open **Edit > Project Settings > Unity Object Link**.
2. Choose a stable, shared Project ID. The generated value is safe for local use but should be replaced with a recognizable identifier before links are shared.
3. Keep the default `unity-object-link` scheme or select an organization-specific scheme.
4. Click **Apply Settings**, then **Register**.

The registration is per OS user. Register the same scheme once on every workstation that should open links. Only a running Unity Editor is eligible to receive a link.

## Copy a link

Select one object and use one of these commands:

- **Assets > Copy Unity Object Link**
- **GameObject > Copy Unity Object Link**
- **Tools > Unity Object Link > Copy Link for Active Selection**

The public API is also available:

```csharp
if (UnityObjectLink.UnityObjectLinkApi.TryCreateLink(target, out string uri, out string error))
{
    GUIUtility.systemCopyBuffer = uri;
}
```

A version 1 link has this form:

```text
unity-object-link://select?v=1&project=sample-project&object=<URL-encoded GlobalObjectId>
```

## Supported targets and limits

| Target | Supported | Requirement |
| --- | --- | --- |
| Asset | Yes | The asset is saved in the Asset Database. |
| Sub-asset | Yes | The sub-asset has a persistent `GlobalObjectId`. |
| Object inside a Prefab asset | Yes | The Prefab is saved. |
| Scene object | Yes | The Scene has no unsaved changes and is currently loaded when the link opens. |
| Object in an unloaded Scene | No automatic load | The package reports that the object was not found and leaves Scenes unchanged. |
| Unsaved Scene or temporary object | No | Unity cannot provide a shareable persistent identity. |
| Play Mode-only object | No | The identity does not survive outside that session. |

Links contain no absolute file paths. A Project ID routes the request, and Unity's `GlobalObjectId` identifies the target.

## Uninstall

Open **Project Settings > Unity Object Link** and click **Unregister** before removing the package. If Unity is unavailable, run the packaged script manually.

Windows PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Editor\Platform\Windows\UnityObjectLinkProtocol.ps1 -Command uninstall -Scheme unity-object-link
```

macOS:

```bash
/bin/bash Editor/Platform/macOS/unity-object-link-protocol.sh uninstall unity-object-link
```

## Security and privacy

The OS handler accepts only the `select` action and exactly three bounded parameters. Scheme and Project ID values are restricted before they are used in local paths. Requests cannot name an executable, command, or filesystem path. The handler only writes a uniquely named request into a fixed per-user inbox when a fresh heartbeat proves that the target project is running.

See [Security model](Documentation~/Security.md) and [URI specification](Documentation~/UriSpecification.md) for the complete validation contract.

## Client compatibility

Custom URI link behavior depends on the application and its security policy. Some clients display a custom scheme as plain text or require a confirmation prompt. See [Compatibility](Documentation~/Compatibility.md) and the [manual checklist](Documentation~/ClientCompatibilityChecklist.md). An HTTPS redirect service is intentionally outside version 1 and may be considered later.

## Development

`DevelopmentProject~` is the interactive Unity 6 development project. `TestProject~` and `TestProject2022~` are minimal Unity 6 and Unity 2022.3 command-line validation projects. The package's EditMode tests cover URI validation, storage and inbox behavior, and `GlobalObjectId` round trips. See [Testing](Documentation~/Testing.md), [Architecture](Documentation~/Architecture.md), and [Platform integration](Documentation~/PlatformIntegration.md).

Japanese documentation: [README-ja.md](README-ja.md)  
HTML version: [Documentation~/README.html](Documentation~/README.html)
