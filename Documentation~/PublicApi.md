# Public Editor API

日本語版: [PublicApi-ja.md](PublicApi-ja.md)

All public types are in the `UnityObjectLink` namespace and the `UnityObjectLink.Editor` assembly.

## `UnityObjectLinkApi`

- `TryCreateLink(Object target, out string uri, out string error)` creates a link for one persistent object. It returns `false` with a user-facing reason for null, temporary, unsaved Scene, or unidentifiable targets.
- `HandleLink(string uri)` validates, resolves, selects, focuses, and pings a target. Most callers should let the receiver call this method.
- `LinkHandled` is raised after an explicit or inbox-delivered link has produced a result.

## `UnityObjectLinkUri`

- `TryCreate` validates individual fields and builds an immutable version 1 model.
- `TryParse` strictly parses a URI and can enforce the expected scheme and Project ID.
- `ToString` produces the canonical encoded representation.

## `UnityObjectLinkSettings`

`UnityObjectLinkSettings.instance` exposes `Scheme` and `ProjectId`. `TryUpdate` performs the same validation as URI creation and persists to `ProjectSettings/UnityObjectLinkSettings.asset`.

## `UnityObjectLinkResult`

The result exposes `Status`, `Succeeded`, `Message`, `Uri`, and `Target`. Status values distinguish invalid input, wrong project, missing object, and internal failures.

All APIs are Editor-only. Consumer code must be placed in an Editor assembly or wrapped in `#if UNITY_EDITOR`.
