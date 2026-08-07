#!/bin/bash
set -euo pipefail
umask 077

command_name="${1:-status}"
scheme="${2:-}"
uri="${3:-}"
product_root="$HOME/Library/Application Support/UnityObjectLink"
stable_script="$product_root/bin/unity-object-link-protocol.sh"

fail() {
  printf 'Unity Object Link: %s\n' "$1" >&2
  exit 1
}

validate_scheme() {
  local value="$1"
  [[ "$value" =~ ^[A-Za-z][A-Za-z0-9+.-]{0,31}$ ]] || fail 'Invalid URI scheme.'
  printf '%s' "$value" | tr '[:upper:]' '[:lower:]'
}

bundle_identifier_suffix() {
  local value="$1"
  local result=""
  local index character
  for ((index=0; index<${#value}; index++)); do
    character="${value:index:1}"
    case "$character" in
      '.') result+='-dot-' ;;
      '+') result+='-plus-' ;;
      '-') result+='-dash-' ;;
      *) result+="$character" ;;
    esac
  done
  printf '%s' "$result"
}

escape_applescript_string() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '%s' "$value"
}

run_osacompile() {
  /usr/bin/osacompile "$@"
}

run_plist_buddy() {
  /usr/libexec/PlistBuddy "$@"
}

run_codesign() {
  /usr/bin/codesign --force --deep --sign - "$1" >/dev/null
}

register_with_launch_services() {
  /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$1"
}

unregister_from_launch_services() {
  /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -u "$1" >/dev/null 2>&1 || true
}

percent_decode() {
  local value="$1"
  local result=""
  local index=0
  local character hex decoded
  while (( index < ${#value} )); do
    character="${value:index:1}"
    if [[ "$character" == '%' ]]; then
      (( index + 2 < ${#value} )) || fail 'Invalid percent encoding.'
      hex="${value:index+1:2}"
      [[ "$hex" =~ ^[0-9A-Fa-f]{2}$ ]] || fail 'Invalid percent encoding.'
      local decimal=$((16#$hex))
      (( decimal >= 32 && decimal != 127 && decimal < 128 )) || fail 'Percent encoding contains a control or non-ASCII byte.'
      printf -v decoded '%b' "\\x$hex"
      result+="$decoded"
      ((index+=3))
    else
      result+="$character"
      ((index+=1))
    fi
  done
  printf '%s' "$result"
}

parse_link() {
  local raw="$1"
  (( ${#raw} > 0 && ${#raw} <= 8192 )) || fail 'URI is empty or too long.'
  [[ ! "$raw" =~ [[:cntrl:]] ]] || fail 'URI contains control characters.'
  [[ "$raw" =~ ^([A-Za-z][A-Za-z0-9+.-]{0,31})://[Ss][Ee][Ll][Ee][Cc][Tt]/?\?([^#]+)$ ]] || fail 'Unsupported URI action.'
  parsed_scheme="$(validate_scheme "${BASH_REMATCH[1]}")"
  local query="${BASH_REMATCH[2]}"
  local pair encoded_name encoded_value name value
  local version="" project="" object=""
  local count=0
  local seen_v=0 seen_project=0 seen_object=0
  IFS='&' read -r -a pairs <<< "$query"
  for pair in "${pairs[@]}"; do
    [[ "$pair" == *=* && "$pair" != *=*=* ]] || fail 'Malformed query parameter.'
    encoded_name="${pair%%=*}"
    encoded_value="${pair#*=}"
    name="$(percent_decode "$encoded_name")"
    value="$(percent_decode "$encoded_value")"
    case "$name" in
      v) (( seen_v == 0 )) || fail 'Duplicate query parameter.'; seen_v=1; version="$value" ;;
      project) (( seen_project == 0 )) || fail 'Duplicate query parameter.'; seen_project=1; project="$value" ;;
      object) (( seen_object == 0 )) || fail 'Duplicate query parameter.'; seen_object=1; object="$value" ;;
      *) fail 'Unknown query parameter.' ;;
    esac
    ((count+=1))
  done
  (( count == 3 && seen_v == 1 && seen_project == 1 && seen_object == 1 )) || fail 'Missing query parameter.'
  [[ "$version" == '1' ]] || fail 'Unsupported URI version.'
  [[ "$project" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ && "$project" != *..* ]] || fail 'Invalid Project ID.'
  (( ${#object} <= 512 )) && [[ "$object" == GlobalObjectId_V1-* ]] || fail 'Invalid GlobalObjectId.'
  parsed_project="$project"
}

helper_path() {
  printf '%s/handlers/%s/Unity Object Link Handler.app' "$product_root" "$1"
}

install_protocol() {
  local normalized helper_dir app source_file plist bundle_suffix applescript_script source_script
  normalized="$(validate_scheme "$scheme")"
  mkdir -p "$(dirname "$stable_script")"
  source_script="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")"
  if [[ "$source_script" != "$stable_script" ]]; then
    cp "$source_script" "$stable_script"
  fi
  chmod 700 "$stable_script"

  app="$(helper_path "$normalized")"
  helper_dir="$(dirname "$app")"
  mkdir -p "$helper_dir"
  source_file="$helper_dir/handler.applescript"
  applescript_script="$(escape_applescript_string "$stable_script")"
  printf 'on open location theUrl\n  do shell script quoted form of "%s" & " dispatch %s " & quoted form of theUrl\nend open location\n' "$applescript_script" "''" > "$source_file"
  rm -rf -- "$app"
  run_osacompile -o "$app" "$source_file"
  plist="$app/Contents/Info.plist"
  bundle_suffix="$(bundle_identifier_suffix "$normalized")"
  run_plist_buddy -c "Set :CFBundleIdentifier com.example.UnityObjectLink.$bundle_suffix" "$plist" >/dev/null
  run_plist_buddy -c 'Delete :CFBundleURLTypes' "$plist" >/dev/null 2>&1 || true
  run_plist_buddy -c 'Add :CFBundleURLTypes array' "$plist"
  run_plist_buddy -c 'Add :CFBundleURLTypes:0 dict' "$plist"
  run_plist_buddy -c 'Add :CFBundleURLTypes:0:CFBundleURLName string Unity Object Link' "$plist"
  run_plist_buddy -c 'Add :CFBundleURLTypes:0:CFBundleURLSchemes array' "$plist"
  run_plist_buddy -c "Add :CFBundleURLTypes:0:CFBundleURLSchemes:0 string $normalized" "$plist"
  if [[ -x /usr/bin/codesign ]]; then
    run_codesign "$app"
  fi
  register_with_launch_services "$app"
  printf 'STATUS=registered;SCHEME=%s\n' "$normalized"
}

uninstall_protocol() {
  local normalized app helper_dir source_file
  normalized="$(validate_scheme "$scheme")"
  app="$(helper_path "$normalized")"
  helper_dir="$(dirname "$app")"
  source_file="$helper_dir/handler.applescript"
  if [[ -d "$app" ]]; then
    unregister_from_launch_services "$app"
    rm -rf -- "$app"
  fi
  rm -f -- "$source_file"
  rmdir "$helper_dir" >/dev/null 2>&1 || true
  rmdir "$product_root/handlers" >/dev/null 2>&1 || true
  if [[ ! -d "$product_root/handlers" ]] || ! find "$product_root/handlers" -name '*.app' -type d -print -quit | grep -q .; then
    rm -f -- "$stable_script"
    rmdir "$(dirname "$stable_script")" >/dev/null 2>&1 || true
  fi
  printf 'STATUS=unregistered;SCHEME=%s\n' "$normalized"
}

protocol_status() {
  local normalized app plist
  normalized="$(validate_scheme "$scheme")"
  app="$(helper_path "$normalized")"
  plist="$app/Contents/Info.plist"
  if [[ -f "$plist" ]] && run_plist_buddy -c 'Print :CFBundleURLTypes:0:CFBundleURLSchemes:0' "$plist" 2>/dev/null | grep -Fxq "$normalized"; then
    printf 'STATUS=registered;SCHEME=%s\n' "$normalized"
  else
    printf 'STATUS=not-registered;SCHEME=%s\n' "$normalized"
  fi
}

dispatch_link() {
  parse_link "$uri"
  local instance heartbeat now modified age inbox request_id temporary request
  instance="$product_root/instances/$parsed_scheme/$parsed_project"
  heartbeat="$instance/heartbeat.json"
  [[ -f "$heartbeat" ]] || fail 'The target Unity project is not running.'
  now="$(date +%s)"
  modified="$(stat -f %m "$heartbeat")"
  age=$((now-modified))
  (( age >= -5 && age <= 15 )) || fail 'The target Unity project heartbeat is stale.'
  inbox="$instance/inbox"
  mkdir -p "$inbox"
  request_id="$(date -u +%Y%m%d%H%M%S)-$$-$RANDOM-$RANDOM"
  temporary="$inbox/$request_id.tmp"
  request="$inbox/$request_id.request"
  printf '%s' "$uri" > "$temporary"
  mv "$temporary" "$request"
  printf 'STATUS=dispatched;PROJECT=%s\n' "$parsed_project"
}

main() {
  case "$command_name" in
    install) install_protocol ;;
    uninstall) uninstall_protocol ;;
    status) protocol_status ;;
    dispatch) dispatch_link ;;
    *) fail 'Unknown command.' ;;
  esac
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  main
fi
