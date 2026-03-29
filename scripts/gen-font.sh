#!/bin/sh
# =============================================================================
# gen-font.sh — Copy downloaded fonts to game Content directory
#
# Copies fonts from fonts/ (managed by vendir) to game/FarmGame/Content/Fonts/
# Also creates Inter-Regular.ttf alias for Myra UI compatibility.
#
# Usage:
#   ./scripts/gen-font.sh
# =============================================================================

set -e

SRC_DIR="fonts/noto-sans-tc"
DEST_DIR="game/FarmGame/Content/Fonts"
FONT_FILE="NotoSansCJKtc-Regular.otf"

if [ ! -f "$SRC_DIR/$FONT_FILE" ]; then
    echo "Error: Font not found at $SRC_DIR/$FONT_FILE"
    echo "Run 'just vendor-sync' first to download fonts."
    exit 1
fi

mkdir -p "$DEST_DIR"

cp "$SRC_DIR/$FONT_FILE" "$DEST_DIR/$FONT_FILE"
# Myra's built-in stylesheet references Inter-Regular.ttf by name.
# Alias our Chinese font so Myra can render CJK characters.
cp "$SRC_DIR/$FONT_FILE" "$DEST_DIR/Inter-Regular.ttf"

echo "Fonts copied to $DEST_DIR:"
ls -lh "$DEST_DIR"
