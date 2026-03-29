#!/bin/sh
# =============================================================================
# gen-image.sh — Batch generate PNG images from YAML pixel art definitions
#
# Reads all YAML files under assets/images/*/ (excluding palette.yaml),
# generates PNG files to game/FarmGame/Content/Images/<category>/<name>.png
#
# Usage:
#   ./scripts/gen-image.sh
# =============================================================================

set -e

ASSETS_DIR="assets/images"
PALETTE_FILE="$ASSETS_DIR/palette.yaml"
OUTPUT_BASE="game/FarmGame/Content/Images"
TOOL="tools/pixel_artify.py"

if [ ! -f "$PALETTE_FILE" ]; then
    echo "Error: Palette file not found: $PALETTE_FILE"
    exit 1
fi

pip3 install -q -r tools/requirements.txt

count=0

for dir in "$ASSETS_DIR"/*/; do
    [ -d "$dir" ] || continue
    category=$(basename "$dir")

    for yaml_file in "$dir"*.yaml; do
        [ -f "$yaml_file" ] || continue
        name=$(basename "$yaml_file" .yaml)
        output="$OUTPUT_BASE/$category/$name.png"

        python3 "$TOOL" "$yaml_file" --palette "$PALETTE_FILE" --output "$output"
        count=$((count + 1))
    done
done

echo "Done: $count image(s) generated"
