#!/usr/bin/env python3
"""
yaml_export.py — Convert pixelated PNGs to YAML palette-encoded format

Reuses palette construction and character matrix logic from compress_and_yaml.py / png_to_yaml.py.
Uses a safe character set (a-z, 0-9) to avoid GitHub secret scanning false positives.

Usage:
    python3 tools/sprite_pipeline/yaml_export.py \
        --input-dir build/sprite_pipeline/chicken/pixelated/ \
        --output-dir assets/images/creatures_chicken/ \
        --name chicken \
        --category creatures_chicken
"""

import argparse
import os
import sys
from PIL import Image
import yaml


# Safe character set (same as compress_and_yaml.py)
SAFE_CHARS = "abcdefghijklmnopqrstuvwxyz0123456789"


class LiteralDumper(yaml.SafeDumper):
    """YAML dumper that uses literal block style for multiline strings."""
    def represent_data(self, data):
        if isinstance(data, str) and "\n" in data:
            return self.represent_scalar("tag:yaml.org,2002:str", data, style="|")
        return super().represent_data(data)


def png_to_yaml(png_path, output_path, name, output_dir_value):
    """Convert a single pixelated PNG to YAML palette-encoded format."""
    img = Image.open(png_path).convert("RGBA")
    width, height = img.size

    color_to_char = {}
    char_to_hex = {}
    char_index = 0
    pixel_matrix = []

    for y in range(height):
        row = ""
        for x in range(width):
            r, g, b, a = img.getpixel((x, y))

            if a < 128:
                row += "."
            else:
                hex_color = f"#{r:02x}{g:02x}{b:02x}"
                if hex_color not in color_to_char:
                    if char_index < len(SAFE_CHARS):
                        c = SAFE_CHARS[char_index]
                        color_to_char[hex_color] = c
                        char_to_hex[c] = hex_color
                        char_index += 1
                    else:
                        c = "?"
                row += color_to_char.get(hex_color, "?")
        pixel_matrix.append(row)

    # Add transparent pixel to palette
    palette = {".": "#00000000"}
    palette.update(char_to_hex)

    yaml_data = {
        "metadata": {"name": name},
        "type": "png",
        "output_dir": f"Images/{output_dir_value}",
        "palette": palette,
        "data": ["\n" + "\n".join(pixel_matrix) + "\n"],
    }

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        yaml.dump(yaml_data, f, Dumper=LiteralDumper, sort_keys=False, allow_unicode=True)

    return name


def process_directory(input_dir, output_dir, name, category):
    """Convert all pixelated PNGs in a directory to YAML files."""
    if not os.path.exists(input_dir):
        print(f"Error: Input directory not found: {input_dir}")
        sys.exit(1)

    os.makedirs(output_dir, exist_ok=True)

    files = sorted(f for f in os.listdir(input_dir) if f.lower().endswith(".png"))
    if not files:
        print(f"Warning: No PNG files found in {input_dir}")
        return

    for filename in files:
        base = os.path.splitext(filename)[0]
        input_path = os.path.join(input_dir, filename)
        output_path = os.path.join(output_dir, f"{base}.yaml")

        png_to_yaml(input_path, output_path, base, category)
        print(f"  Exported YAML: {base}.yaml")

    print(f"Done! {len(files)} YAML files in {output_dir}")


def main():
    parser = argparse.ArgumentParser(description="Convert pixelated PNGs to YAML palette format")
    parser.add_argument("--input-dir", required=True, help="Directory with pixelated PNGs")
    parser.add_argument("--output-dir", required=True, help="Output directory for YAML files")
    parser.add_argument("--name", required=True, help="Base name for the sprite set")
    parser.add_argument("--category", required=True,
                        help="Category for output_dir in YAML (e.g., creatures_chicken)")
    args = parser.parse_args()

    process_directory(args.input_dir, args.output_dir, args.name, args.category)


if __name__ == "__main__":
    main()
