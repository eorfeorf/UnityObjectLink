#!/bin/bash
set -euo pipefail

if [[ "$(uname -s)" != 'Darwin' ]]; then
  printf 'This E2E test must run on macOS.\n' >&2
  exit 2
fi

scheme="${1:-unity-object-link-e2e-$$}"
project_id="${2:-e2e-$$}"
[[ "$scheme" =~ ^[A-Za-z][A-Za-z0-9+.-]{0,31}$ ]] || { printf 'The E2E scheme is invalid.\n' >&2; exit 1; }
scheme="$(printf '%s' "$scheme" | tr '[:upper:]' '[:lower:]')"
[[ "$project_id" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ && "$project_id" != *..* ]] || { printf 'The E2E Project ID is invalid.\n' >&2; exit 1; }
script_directory="$(cd "$(dirname "$0")" && pwd)"
handler="$(cd "$script_directory/../.." && pwd)/Editor/Platform/macOS/unity-object-link-protocol.sh"
product_root="$HOME/Library/Application Support/UnityObjectLink"
instance="$product_root/instances/$scheme/$project_id"
inbox="$instance/inbox"
heartbeat="$instance/heartbeat.json"
installed=0

remove_empty_directory() {
  local path="$1"
  if [[ -d "$path" ]]; then
    rmdir "$path" >/dev/null 2>&1 || true
  fi
}

cleanup() {
  local exit_code=$?
  trap - EXIT
  if (( installed == 1 )); then
    /bin/bash "$handler" uninstall "$scheme" >/dev/null 2>&1 || true
  fi

  if [[ -d "$inbox" ]]; then
    while IFS= read -r -d '' file; do
      rm -f -- "$file"
    done < <(find "$inbox" -maxdepth 1 -type f -print0)
  fi
  rm -f -- "$heartbeat"
  remove_empty_directory "$inbox"
  remove_empty_directory "$instance"
  remove_empty_directory "$(dirname "$instance")"
  exit "$exit_code"
}
trap cleanup EXIT

[[ -f "$handler" ]] || { printf 'Protocol handler not found: %s\n' "$handler" >&2; exit 1; }

installed=1
install_output="$(/bin/bash "$handler" install "$scheme")"
[[ "$install_output" == *'STATUS=registered'* ]] || { printf 'Install failed: %s\n' "$install_output" >&2; exit 1; }

status_output="$(/bin/bash "$handler" status "$scheme")"
[[ "$status_output" == *'STATUS=registered'* ]] || { printf 'Status failed: %s\n' "$status_output" >&2; exit 1; }

invalid_uri="$scheme://select?v=1&project=..%2Fescape&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
if /bin/bash "$handler" dispatch '' "$invalid_uri" >/dev/null 2>&1; then
  printf 'Traversal URI was unexpectedly accepted.\n' >&2
  exit 1
fi

mkdir -p "$inbox"
printf '{"version":1}' > "$heartbeat"
uri="$scheme://select?v=1&project=$project_id&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
/usr/bin/open "$uri"

request=''
for _ in $(seq 1 50); do
  request="$(find "$inbox" -maxdepth 1 -type f -name '*.request' -print -quit)"
  [[ -n "$request" ]] && break
  sleep 0.2
done

[[ -n "$request" ]] || { printf 'OS URL activation did not create an inbox request within 10 seconds.\n' >&2; exit 1; }
delivered="$(cat "$request")"
canonical_without_slash="$uri"
canonical_with_slash="${scheme}://select/?${uri#*\?}"
[[ "$delivered" == "$canonical_without_slash" || "$delivered" == "$canonical_with_slash" ]] || {
  printf 'Delivered URI was not equivalent: %s\n' "$delivered" >&2
  exit 1
}

uninstall_output="$(/bin/bash "$handler" uninstall "$scheme")"
[[ "$uninstall_output" == *'STATUS=unregistered'* ]] || { printf 'Uninstall failed: %s\n' "$uninstall_output" >&2; exit 1; }
installed=0
status_output="$(/bin/bash "$handler" status "$scheme")"
[[ "$status_output" == *'STATUS=not-registered'* ]] || { printf 'Status did not confirm uninstall: %s\n' "$status_output" >&2; exit 1; }
[[ ! -e "$product_root/handlers/$scheme" ]] || { printf 'Generated helper files remained after uninstall.\n' >&2; exit 1; }

printf 'E2E_PASS=True;NEGATIVE_VALIDATION=True;SCHEME=%s;PROJECT=%s\n' "$scheme" "$project_id"
printf 'DELIVERED_URI=%s\n' "$delivered"
