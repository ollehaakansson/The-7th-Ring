#!/usr/bin/env bash
#
# Downloads the Godot version pinned in versions.env into tools/. Nothing is
# installed system-wide.
#
#   ./scripts/install.sh                     normal setup
#   ./scripts/install.sh --export-templates  also fetch export templates (~1 GB)
#   ./scripts/install.sh --force             re-download even if present

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

EXPORT_TEMPLATES=0
FORCE=0
for arg in "$@"; do
	case "$arg" in
		--export-templates) EXPORT_TEMPLATES=1 ;;
		--force)            FORCE=1 ;;
		-h|--help)          sed -n '3,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
		*)                  echo "unknown option: $arg" >&2; exit 2 ;;
	esac
done

if [ -t 1 ]; then
	C_CYAN=$'\033[36m'; C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'
	C_RED=$'\033[31m';  C_MAGENTA=$'\033[35m'; C_OFF=$'\033[0m'
else
	C_CYAN=; C_GREEN=; C_YELLOW=; C_RED=; C_MAGENTA=; C_OFF=
fi
info() { echo "  $*"; }
step() { echo ""; echo "${C_CYAN}==> $*${C_OFF}"; }
ok()   { echo "  ${C_GREEN}OK${C_OFF}  $*"; }
warn() { echo "  ${C_YELLOW}!${C_OFF}   $*"; }
fail() { echo ""; echo "${C_RED}FAILED: $*${C_OFF}" >&2; exit 1; }

[ "$(uname -s)" = "Darwin" ] || fail "This script is for macOS. On Windows use scripts\install.ps1"

ENV_FILE="$REPO_ROOT/versions.env"
[ -f "$ENV_FILE" ] || fail "versions.env not found at $ENV_FILE"
while IFS='=' read -r key value; do
	case "$key" in
		GODOT_VERSION|GODOT_RELEASE|DOTNET_SDK_VERSION|GODOT_EXPORT_TEMPLATES)
			printf -v "$key" '%s' "$value" ;;
	esac
done < <(sed -e 's/[[:space:]]*#.*$//' -e '/^[[:space:]]*$/d' "$ENV_FILE")

: "${GODOT_VERSION:?versions.env is missing GODOT_VERSION}"
: "${GODOT_RELEASE:?versions.env is missing GODOT_RELEASE}"
: "${DOTNET_SDK_VERSION:?versions.env is missing DOTNET_SDK_VERSION}"
[ "${GODOT_EXPORT_TEMPLATES:-0}" = "1" ] && EXPORT_TEMPLATES=1

GODOT_TAG="${GODOT_VERSION}-${GODOT_RELEASE}"
BASE_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_TAG}"

echo "${C_MAGENTA}The 7th Ring - development environment setup${C_OFF}"
info "Godot      ${GODOT_TAG} (.NET build)"
info ".NET SDK   ${DOTNET_SDK_VERSION} or later"
info "Repo       ${REPO_ROOT}"

step ".NET SDK"
DOTNET_OK=0
required_major="${DOTNET_SDK_VERSION%%.*}"
if command -v dotnet >/dev/null 2>&1; then
	found_major=$(dotnet --list-sdks 2>/dev/null | sed -n 's/^\([0-9]\{1,\}\)\..*/\1/p' |
	              sort -rn | awk -v r="$required_major" '$1 >= r {print; exit}')
	if [ -n "$found_major" ]; then
		ok ".NET SDK ${found_major}.x found (need ${required_major} or later)"
		DOTNET_OK=1
	else
		warn "dotnet is installed but no SDK >= ${required_major} was found"
	fi
else
	warn ".NET SDK not found"
fi

if [ "$DOTNET_OK" -eq 0 ]; then
	if command -v brew >/dev/null 2>&1; then
		info "Install it with:"
		info "    brew install --cask dotnet-sdk"
	else
		info "Download it from https://dotnet.microsoft.com/download"
	fi
	warn "C# scripts will not compile until the SDK is installed."
	warn "The Godot editor itself will still open, so continuing."
fi

step "Git LFS"
if command -v git-lfs >/dev/null 2>&1; then
	git -C "$REPO_ROOT" lfs install --local >/dev/null
	ok "Git LFS active (sprites, audio and other binaries are stored properly)"
else
	warn "git-lfs not found - binary assets will bloat the repo history."
	info "Install with: brew install git-lfs"
fi

step "Godot engine"
GODOT_DIR="$REPO_ROOT/tools/godot/$GODOT_TAG"
GODOT_APP="$GODOT_DIR/Godot_mono.app"
GODOT_BIN="$GODOT_APP/Contents/MacOS/Godot"

if [ -x "$GODOT_BIN" ] && [ "$FORCE" -eq 0 ]; then
	ok "Already present at tools/godot/$GODOT_TAG"
else
	[ "$FORCE" -eq 1 ] && rm -rf "$GODOT_DIR"
	mkdir -p "$GODOT_DIR"

	zip_name="Godot_v${GODOT_TAG}_mono_macos.universal.zip"
	tmp_zip="$(mktemp -t godot-editor).zip"
	info "Downloading ${zip_name} (~150 MB)"
	info "${BASE_URL}/${zip_name}"
	curl -fL --progress-bar -o "$tmp_zip" "${BASE_URL}/${zip_name}" ||
		fail "Download failed. Check that ${GODOT_TAG} exists at https://github.com/godotengine/godot-builds/releases"

	info "Extracting"
	ditto -x -k "$tmp_zip" "$GODOT_DIR"
	rm -f "$tmp_zip"
	[ -x "$GODOT_BIN" ] || fail "Expected editor at $GODOT_BIN but it is not there"

	# Otherwise Gatekeeper won't open the downloaded .app.
	xattr -dr com.apple.quarantine "$GODOT_APP" 2>/dev/null || true
	ok "Installed to tools/godot/$GODOT_TAG"
fi

reported="$("$GODOT_BIN" --version 2>/dev/null | head -n 1 || true)"
case "$reported" in
	"$GODOT_VERSION"*) ok "Version check: $reported" ;;
	"")                : ;;
	*)                 warn "Binary reports '$reported' but versions.env pins $GODOT_VERSION" ;;
esac

step "Export templates"
TPL_ROOT="$HOME/Library/Application Support/Godot/export_templates/${GODOT_VERSION}.${GODOT_RELEASE}.mono"
if [ "$EXPORT_TEMPLATES" -eq 1 ]; then
	if [ -d "$TPL_ROOT" ] && [ "$FORCE" -eq 0 ]; then
		ok "Already present"
	else
		tpz_name="Godot_v${GODOT_TAG}_mono_export_templates.tpz"
		tmp_tpz="$(mktemp -t godot-tpl).tpz"
		info "Downloading ${tpz_name} (~1 GB, this takes a while)"
		curl -fL --progress-bar -o "$tmp_tpz" "${BASE_URL}/${tpz_name}" || fail "Download failed"

		staging="$(mktemp -d)"
		ditto -x -k "$tmp_tpz" "$staging"
		mkdir -p "$TPL_ROOT"
		cp -R "$staging/templates/." "$TPL_ROOT/"
		rm -rf "$staging" "$tmp_tpz"
		ok "Installed to $TPL_ROOT"
	fi
else
	info "Skipped. Run with --export-templates when you need to build a release."
fi

echo ""
echo "${C_GREEN}Setup complete.${C_OFF}"
echo "Open the editor with:  ${C_CYAN}./scripts/run.sh${C_OFF}"
[ "$DOTNET_OK" -eq 0 ] &&
	echo "${C_YELLOW}Install the .NET SDK first if you intend to write C# today.${C_OFF}"
exit 0
