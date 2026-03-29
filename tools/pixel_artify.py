#!/usr/bin/env python3
"""
pixel_artify.py — Generate PNG images from YAML pixel art definitions.

Usage:
    python3 tools/pixel_artify.py <image.yaml> --palette <palette.yaml> --output <output.png>

Each character in the `data` field maps to a color in the merged palette.
The merged palette = global base_palette + image-local palette (local wins).
"""

import argparse
import sys
from pathlib import Path

import yaml
from PIL import Image


def load_yaml(path: str) -> dict:
    with open(path, "r") as f:
        return yaml.safe_load(f)


def parse_hex_color(hex_str: str) -> tuple[int, int, int, int]:
    """Parse #RRGGBB or #RRGGBBAA to (R, G, B, A) tuple."""
    h = hex_str.lstrip("#")
    if len(h) == 6:
        return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
    elif len(h) == 8:
        return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16))
    else:
        raise ValueError(f"Invalid hex color: {hex_str}")


def build_palette(global_palettes: dict, base_palette_name: str, local_palette: dict) -> dict:
    """Merge global base palette with image-local palette overrides."""
    merged = {}

    if base_palette_name and base_palette_name in global_palettes:
        for char, color in global_palettes[base_palette_name].items():
            merged[char] = parse_hex_color(color)

    if local_palette:
        for char, color in local_palette.items():
            merged[char] = parse_hex_color(color)

    return merged


def generate_image(data: str, palette: dict) -> Image.Image:
    """Generate a PIL Image from pixel data string and color palette."""
    lines = [line for line in data.splitlines() if line.strip()]

    if not lines:
        raise ValueError("Empty pixel data")

    height = len(lines)
    width = max(len(line) for line in lines)

    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))

    for y, line in enumerate(lines):
        for x, char in enumerate(line):
            if char in palette:
                img.putpixel((x, y), palette[char])
            # Unknown chars remain transparent

    return img


def main():
    parser = argparse.ArgumentParser(description="Generate PNG from YAML pixel art definition")
    parser.add_argument("image_yaml", help="Path to the image YAML definition")
    parser.add_argument("--palette", required=True, help="Path to the global palette YAML")
    parser.add_argument("--output", required=True, help="Output PNG path")
    parser.add_argument("--scale", type=int, default=1, help="Scale factor (1 = 1 char = 1 pixel)")
    args = parser.parse_args()

    # Load global palettes
    palette_data = load_yaml(args.palette)
    global_palettes = palette_data.get("global_palettes", {})

    # Load image definition
    image_def = load_yaml(args.image_yaml)

    base_palette_name = image_def.get("base_palette", "")
    local_palette = image_def.get("palette", {})
    pixel_data = image_def.get("data", "")

    if not pixel_data:
        print(f"Error: No pixel data in {args.image_yaml}", file=sys.stderr)
        sys.exit(1)

    # Build merged palette
    palette = build_palette(global_palettes, base_palette_name, local_palette)

    # Generate image
    img = generate_image(pixel_data, palette)

    # Scale if requested
    if args.scale > 1:
        img = img.resize(
            (img.width * args.scale, img.height * args.scale),
            Image.NEAREST,
        )

    # Save
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(str(output_path))

    name = image_def.get("metadata", {}).get("name", output_path.stem)
    print(f"Generated: {output_path} ({img.width}x{img.height}) — {name}")


if __name__ == "__main__":
    main()
