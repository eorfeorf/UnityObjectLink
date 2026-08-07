# Client compatibility checklist

日本語版: [ClientCompatibilityChecklist-ja.md](ClientCompatibilityChecklist-ja.md)

Use an authorized private test channel, draft, page, or project. Do not post a test link to another person or a shared production location merely to verify link handling.

## Preparation

1. Open the package's `DevelopmentProject~` or another disposable Unity project.
2. In **Project Settings > Unity Object Link**, set a recognizable test Project ID and register the scheme.
3. Select a saved asset and copy its link with **Tools > Unity Object Link > Copy Link for Active Selection**.
4. Confirm that the receiver heartbeat is **Active**.

## Per-client check

1. Paste the raw URI into a private draft or test surface.
2. Record whether the client turns it into a clickable link.
3. If the client supports explicit link markup, repeat with the URI as the link destination.
4. Activate the link and record whether the client or OS displays a confirmation prompt.
5. Confirm that the expected Unity project becomes active when permitted, selects the exact object, and pings it.
6. Repeat with a saved loaded Scene object and confirm that an unloaded Scene is not opened automatically.

Record one row per client/version:

| Date | OS | Client and version | Raw URI clickable | Explicit anchor clickable | Confirmation | Asset selected | Scene behavior | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | | |

Target clients for version 1 are a Chromium-based browser, Slack Desktop/Web, Jira, and Confluence. Client security policies can change independently of this package, so retain the date and version with every result.

## Cleanup

Unregister any temporary scheme from **Project Settings > Unity Object Link**. If a disposable project was used, close it so its heartbeat is removed. Do not publish compatibility as supported when only link rendering—but not handler activation and Unity selection—was observed.
