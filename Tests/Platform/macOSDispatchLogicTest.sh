#!/bin/bash
set -euo pipefail

script_directory="$(cd "$(dirname "$0")" && pwd)"
handler="$(cd "$script_directory/../.." && pwd)/Editor/Platform/macOS/unity-object-link-protocol.sh"
temporary_base="${TMPDIR:-/tmp}"
temporary_root="$(mktemp -d "$temporary_base/unity-object-link-macos-dispatch.XXXXXX")"
original_home="$HOME"
scheme='unity-object-link-macos-logic'
project_id='portable-e2e'

cleanup() {
  local exit_code=$?
  trap - EXIT
  export HOME="$original_home"
  case "$temporary_root" in
    "$temporary_base"/unity-object-link-macos-dispatch.*)
      rm -rf -- "$temporary_root"
      ;;
    *)
      printf 'Refusing to remove unexpected temporary path: %s\n' "$temporary_root" >&2
      exit 1
      ;;
  esac
  exit "$exit_code"
}
trap cleanup EXIT

if [[ "$(uname -s)" != 'Darwin' ]]; then
  stat() {
    if [[ "${1:-}" == '-f' && "${2:-}" == '%m' && -n "${3:-}" ]]; then
      command stat -c %Y "$3"
    else
      command stat "$@"
    fi
  }
  export -f stat
fi

export HOME="$temporary_root/home"
product_root="$HOME/Library/Application Support/UnityObjectLink"
instance="$product_root/instances/$scheme/$project_id"
inbox="$instance/inbox"
heartbeat="$instance/heartbeat.json"
mkdir -p "$inbox"
printf '{"version":1}' > "$heartbeat"

invalid_uri="$scheme://select?v=1&project=..%2Fescape&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
if /bin/bash "$handler" dispatch '' "$invalid_uri" >/dev/null 2>&1; then
  printf 'Traversal URI was unexpectedly accepted.\n' >&2
  exit 1
fi

uri="$scheme://select?v=1&project=$project_id&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
touch -t 200001010000 "$heartbeat"
if /bin/bash "$handler" dispatch '' "$uri" >/dev/null 2>&1; then
  printf 'A stale heartbeat was unexpectedly accepted.\n' >&2
  exit 1
fi
touch "$heartbeat"
dispatch_output="$(/bin/bash "$handler" dispatch '' "$uri")"
[[ "$dispatch_output" == *'STATUS=dispatched'* ]] || { printf 'Dispatch failed: %s\n' "$dispatch_output" >&2; exit 1; }

request="$(find "$inbox" -maxdepth 1 -type f -name '*.request' -print -quit)"
[[ -n "$request" ]] || { printf 'Dispatch did not create a request.\n' >&2; exit 1; }
[[ "$(cat "$request")" == "$uri" ]] || { printf 'Request content did not match the URI.\n' >&2; exit 1; }
[[ -z "$(find "$inbox" -maxdepth 1 -type f -name '*.tmp' -print -quit)" ]] || { printf 'A temporary request file remained.\n' >&2; exit 1; }
rm -f -- "$request"

slash_uri="$scheme://SeLeCt/?v=1&project=$project_id&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
slash_output="$(/bin/bash "$handler" dispatch '' "$slash_uri")"
[[ "$slash_output" == *'STATUS=dispatched'* ]] || { printf 'Dispatch with a mixed-case action and slash failed: %s\n' "$slash_output" >&2; exit 1; }
request="$(find "$inbox" -maxdepth 1 -type f -name '*.request' -print -quit)"
[[ -n "$request" && "$(cat "$request")" == "$slash_uri" ]] || { printf 'Slash-form URI was not delivered intact.\n' >&2; exit 1; }

printf 'DISPATCH_LOGIC_PASS=True;NEGATIVE_VALIDATION=True;STALE_HEARTBEAT=True;SLASH_AND_ACTION_CASE=True\n'
