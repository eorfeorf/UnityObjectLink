# Client and platform compatibility

日本語版: [Compatibility-ja.md](Compatibility-ja.md)

Custom URI handling depends on both OS registration and the application containing the link. This table records the current verification state; “manual confirmation” means the client may ask before launching an external handler.

| Environment | Link recognition | Handler launch | Status |
| --- | --- | --- | --- |
| Windows 11 Run dialog / shell | Native custom protocol | Yes | Verified install/status, rejection, foreign-registration preservation, OS activation, inbox delivery, Unity selection, uninstall, and cleanup on 2026-08-01 |
| macOS handler dispatch | Local Bash/file transport | Yes | URI rejection, heartbeat, and atomic delivery verified with the portable test on 2026-08-01 |
| macOS helper installer logic | Stubbed macOS system commands | Not applicable | App layout, collision-free bundle IDs, AppleScript escaping, plist operations, registration calls, permissions, status, uninstall, and cleanup verified portably on 2026-08-01 |
| macOS `open` command | Native URL handler | Expected | Helper generation and Launch Services still require `macOSProtocolE2E.sh` on macOS |
| Chromium-based browser | Usually clickable when rendered as an anchor | Client confirmation varies | Manual verification required |
| Codex in-app Browser | Private `data:` validation page blocked before navigation by Browser URL policy | Not reached | Environment limitation confirmed on 2026-08-01; not a product result |
| Slack Desktop | Security policy varies by version/workspace | Unknown | Manual verification required |
| Slack Web | Browser policy applies | Unknown | Manual verification required |
| Jira | Renderer/security policy varies | Unknown | Manual verification required |
| Confluence | Renderer/security policy varies | Unknown | Manual verification required |

Plain-text fields may not auto-link a non-HTTP scheme. When a client refuses custom schemes, copy the URI into an OS launcher or another trusted link-capable surface. An optional HTTPS redirect service may be evaluated in a future version; it is not part of the local-only version 1 security model.

Use the [client compatibility checklist](ClientCompatibilityChecklist.md) to complete and record the remaining manual checks without posting test links outside an authorized private location.
