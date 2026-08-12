#!/bin/bash
set -euo pipefail

script_directory="$(cd "$(dirname "$0")" && pwd)"
handler="$(cd "$script_directory/../.." && pwd)/Editor/Platform/macOS/unity-object-link-protocol.sh"
temporary_base="${TMPDIR:-/tmp}"
temporary_root="$(mktemp -d "$temporary_base/unity-object-link-macos-installer.XXXXXX")"
original_home="$HOME"
calls="$temporary_root/calls.log"

cleanup() {
  local exit_code=$?
  trap - EXIT
  export HOME="$original_home"
  case "$temporary_root" in
    "$temporary_base"/unity-object-link-macos-installer.*)
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

export HOME="$temporary_root/home"
source "$handler"

run_osacompile() {
  [[ "${1:-}" == '-o' && -n "${2:-}" && -n "${3:-}" ]] || fail 'Unexpected osacompile arguments.'
  mkdir -p "$2/Contents"
  printf '<plist/>\n' > "$2/Contents/Info.plist"
  printf 'osacompile=%s\n' "$2" >> "$calls"
}

run_plist_buddy() {
  local operation="${2:-}"
  local plist="${3:-}"
  printf 'plist=%s\n' "$operation" >> "$calls"
  if [[ "$operation" == 'Print :CFBundleURLTypes:0:CFBundleURLSchemes:0' && -f "$plist" ]]; then
    printf '%s\n' "$scheme"
  fi
}

run_codesign() {
  printf 'codesign=%s\n' "$1" >> "$calls"
}

register_with_launch_services() {
  printf 'register=%s\n' "$1" >> "$calls"
}

unregister_from_launch_services() {
  printf 'unregister=%s\n' "$1" >> "$calls"
}

scheme='uol.test+installer'
install_output="$(install_protocol)"
[[ "$install_output" == *'STATUS=registered'* ]] || { printf 'Install failed: %s\n' "$install_output" >&2; exit 1; }

app="$(helper_path "$scheme")"
source_file="$(dirname "$app")/handler.applescript"
[[ -f "$stable_script" && -f "$source_file" && -f "$app/Contents/Info.plist" ]] || fail 'Generated helper files are incomplete.'
cmp -s "$handler" "$stable_script" || fail 'The stable handler is not a copy of the package handler.'
grep -Fq " dispatch '' " "$source_file" || fail 'AppleScript does not pass the URI to dispatch safely.'
grep -Fq "Set :CFBundleIdentifier com.eorfeorf.UnityObjectLink.uol-dot-test-plus-installer" "$calls" || fail 'Bundle identifier encoding was not applied.'
grep -Fq "Add :CFBundleURLTypes:0:CFBundleURLSchemes:0 string $scheme" "$calls" || fail 'URL scheme was not written to the plist.'
grep -Fq "register=$app" "$calls" || fail 'Launch Services registration was not requested.'

dot_suffix="$(bundle_identifier_suffix 'uol.test')"
dash_suffix="$(bundle_identifier_suffix 'uol-test')"
[[ "$dot_suffix" != "$dash_suffix" ]] || fail 'Distinct schemes produced the same bundle identifier suffix.'
[[ "$(escape_applescript_string 'a"b\c')" == 'a\"b\\c' ]] || fail 'AppleScript string escaping failed.'

if [[ "$(uname -s)" == 'Darwin' ]]; then
  permissions="$(stat -f %Lp "$stable_script")"
else
  permissions="$(stat -c %a "$stable_script")"
fi
[[ "$permissions" == '700' ]] || fail 'The stable handler does not have mode 700.'

status_output="$(protocol_status)"
[[ "$status_output" == *'STATUS=registered'* ]] || { printf 'Status failed: %s\n' "$status_output" >&2; exit 1; }

uninstall_output="$(uninstall_protocol)"
[[ "$uninstall_output" == *'STATUS=unregistered'* ]] || { printf 'Uninstall failed: %s\n' "$uninstall_output" >&2; exit 1; }
[[ ! -e "$(dirname "$app")" && ! -e "$stable_script" ]] || fail 'Generated helper files remained after uninstall.'
grep -Fq "unregister=$app" "$calls" || fail 'Launch Services unregister was not requested.'

printf 'INSTALLER_LOGIC_PASS=True;BUNDLE_ID_UNIQUE=True;APPLESCRIPT_ESCAPE=True;CLEANUP=True\n'
