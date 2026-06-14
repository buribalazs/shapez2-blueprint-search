#!/usr/bin/env bash
set -euo pipefail

# Usage: ./SteamPublish.sh <steam_username>
# Requires: steamcmd (brew install steamcmd)
# Place a preview.png (512x512 recommended) next to this script before running.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STEAM_USERNAME="${1:?Usage: $0 <steam_username>}"

# Load env vars if not already set (they're defined in ~/.zprofile)
[[ -z "${SPZ2_PERSISTENT:-}" ]] && source "$HOME/.zprofile" 2>/dev/null || true
[[ -z "${SPZ2_PERSISTENT:-}" ]] && { echo "SPZ2_PERSISTENT is not set. Source ~/.zprofile first."; exit 1; }

CONTENT_PATH="${SPZ2_PERSISTENT}/mods/BlueprintSearch"
PREVIEW_IMG="${SCRIPT_DIR}/preview.png"
VDF_SRC="${SCRIPT_DIR}/base.vdf"
VDF_TMP="${SCRIPT_DIR}/base.tmp.vdf"

# Validate
[[ -d "$CONTENT_PATH" ]] || { echo "Content folder not found: $CONTENT_PATH — run 'dotnet build -c Release' first."; exit 1; }
[[ -f "$PREVIEW_IMG" ]]  || { echo "Missing preview image: $PREVIEW_IMG (512x512 PNG recommended)."; exit 1; }
command -v steamcmd      || { echo "steamcmd not found — install it with: brew install steamcmd"; exit 1; }

# Substitute placeholders and expand \n escape sequences to real newlines
sed \
    -e "s|CONTENT_PATH_PLACEHOLDER|$CONTENT_PATH|g" \
    -e "s|PREVIEW_IMG_PLACEHOLDER|$PREVIEW_IMG|g" \
    "$VDF_SRC" | perl -pe 's/\\n/\n/g' > "$VDF_TMP"

echo "=== Publishing with ==="
cat "$VDF_TMP"
echo

steamcmd +login "$STEAM_USERNAME" +workshop_build_item "$VDF_TMP" +quit

# steamcmd writes the assigned file ID back into the VDF it was given.
# Read it and persist it into base.vdf so future runs update rather than re-create.
FILE_ID=$(grep '"publishedfileid"' "$VDF_TMP" | grep -oE '[0-9]+' | head -1)

if [[ -n "$FILE_ID" && "$FILE_ID" != "0" ]]; then
    # BSD sed (macOS) needs -i ''
    sed -i '' 's/\("publishedfileid"[[:space:]]*"\)[0-9]*/\1'"$FILE_ID"'"/' "$VDF_SRC"
    echo
    echo "=== Done ==="
    echo "Workshop item ID : $FILE_ID"
    echo "URL              : https://steamcommunity.com/sharedfiles/filedetails/?id=$FILE_ID"
    echo
    echo "Visibility is currently set to Private (2). Change it on the Workshop page when ready to release."
fi

rm -f "$VDF_TMP"
