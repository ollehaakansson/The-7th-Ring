#!/usr/bin/env bash
#
# Opens the project with the Godot in tools/, not whatever is on PATH.
#
#   ./scripts/run.sh          open the editor
#   ./scripts/run.sh --play   run the game directly
#   ./scripts/run.sh --play --sandbox   run only the test room
#   ./scripts/run.sh --play --design    run only the rooms being designed

set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

while IFS='=' read -r key value; do
	case "$key" in
		GODOT_VERSION|GODOT_RELEASE) printf -v "$key" '%s' "$value" ;;
	esac
done < <(sed -e 's/[[:space:]]*#.*$//' -e '/^[[:space:]]*$/d' "$REPO_ROOT/versions.env")

GODOT_TAG="${GODOT_VERSION}-${GODOT_RELEASE}"
GODOT_BIN="$REPO_ROOT/tools/godot/$GODOT_TAG/Godot_mono.app/Contents/MacOS/Godot"

if [ ! -x "$GODOT_BIN" ]; then
	echo "Godot $GODOT_TAG is not installed in tools/." >&2
	echo "Run ./scripts/install.sh first." >&2
	exit 1
fi

# Godot looks for .NET in the system location and fails with "Failed to load
# hostfxr" on Homebrew installs, so point it at the dotnet on PATH.
if [ -z "${DOTNET_ROOT:-}" ] && command -v dotnet >/dev/null 2>&1; then
	sdk_dir="$(dotnet --list-sdks 2>/dev/null | tail -n 1 | sed -n 's/.*\[\(.*\)\]/\1/p')"
	[ -n "$sdk_dir" ] && export DOTNET_ROOT="$(dirname "$sdk_dir")"
fi

PLAY=0
SANDBOX=0
DESIGN=0
args=()
for a in "$@"; do
	case "$a" in
		--play) PLAY=1 ;;
		--sandbox) SANDBOX=1 ;;
		--design) DESIGN=1 ;;
		*) args+=("$a") ;;
	esac
done

# Godot only rebuilds C# when you press Play in the editor, so build here or
# you'd be running old code.
if [ "$PLAY" -eq 1 ]; then
	dotnet build "$REPO_ROOT" -v quiet --nologo
fi

godot_args=(--path "$REPO_ROOT")
[ "$PLAY" -eq 0 ] && godot_args+=(--editor)
godot_args+=(${args[@]+"${args[@]}"})
[ "$SANDBOX" -eq 1 ] && godot_args+=(-- --sandbox)
[ "$DESIGN" -eq 1 ] && godot_args+=(-- --design)
exec "$GODOT_BIN" "${godot_args[@]}"
