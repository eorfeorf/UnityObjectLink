# Architecture

日本語版: [Architecture-ja.md](Architecture-ja.md)

Unity Object Link is an Editor-only UPM package. It deliberately has no Runtime assembly and no dependency outside Unity's public Editor API and the .NET profile provided by Unity 2022.3.

## Flow

1. `UnityObjectLinkApi.TryCreateLink` asks Unity for a `GlobalObjectId` and combines it with version, scheme, and Project ID.
2. The OS opens the registered protocol handler when a user activates the URI.
3. The handler strictly validates the routing envelope and checks the target project's heartbeat.
4. It writes the URI to a unique temporary file and atomically renames it to `*.request` in the target inbox.
5. `UnityObjectLinkReceiverService` polls the inbox on the Editor update loop.
6. `UnityObjectLinkInboxProcessor` rejects stale, empty, oversized, duplicate, or unreadable requests and deletes every processed file.
7. `UnityObjectLinkResolver` parses the full URI, resolves the `GlobalObjectId`, selects the target, focuses the Project or Hierarchy view, and pings it.

## Boundaries

- `Editor/Public` contains settings, URI, result, and link APIs available to other Editor assemblies.
- `Editor/Internal` contains validation, local storage, receiver, selection, UI, and menu implementation.
- `Editor/Platform` contains the OS registration bridge and scripts.
- `Tests/Editor` compiles only when the package is testable.

Filesystem and timing inputs of the inbox processor are constructor boundaries so transport behavior can be tested without a live protocol registration. Selection behavior remains behind `UnityObjectLinkApi.HandleLink`, whose public result and event form the notification boundary for integrations.

## Object identity

The package stores the exact string returned by `GlobalObjectId.GetGlobalObjectIdSlow`. It does not invent fallback paths or names because those are ambiguous and machine-dependent. `GlobalObjectId.GlobalObjectIdentifierToObjectSlow` only resolves a Scene object while its saved Scene is loaded; the package never loads a Scene as a side effect.
