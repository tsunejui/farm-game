#!/usr/bin/env python3
"""
pixel_artify.py — Generate PNG or GIF pixel art from YAML definitions.

Usage:
    python3 tools/pixel_artify.py <image.yaml> --palette <palette.yaml> --output <output_path>
    python3 tools/pixel_artify.py --all <defs_dir> --palette <palette.yaml> --output-dir <dir>

YAML image definition format:
    metadata:
        name: "campfire_normal"
    type: "gif"                  # "png" (default) or "gif"
    frame_delay: 150             # ms between frames (gif only)
    base_palette: "nature"       # optional reference to global palette
    palette:                     # local palette overrides
        "R": "#FF4500"
        "Y": "#FFD700"
    data:                        # array of frames (png uses [0] only)
        - |
            ................
            ......RR........
        - |
            ................
            .....RRRR.......
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


def render_frame(data: str, palette: dict) -> Image.Image:
    """Generate a PIL Image from a single frame's pixel data string."""
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

    return img


def generate(image_def: dict, palette: dict, output_path: Path, scale: int = 1):
    """Generate PNG or GIF from image definition."""
    img_type = image_def.get("type", "png").lower()
    frame_delay = image_def.get("frame_delay", 150)
    data = image_def.get("data", [])

    # Backward compat: if data is a string, wrap in list
    if isinstance(data, str):
        data = [data]

    if not data:
        print(f"  WARNING: No frame data", file=sys.stderr)
        return

    frames = [render_frame(d, palette) for d in data]

    # Scale
    if scale > 1:
        frames = [f.resize((f.width * scale, f.height * scale), Image.NEAREST) for f in frames]

    output_path.parent.mkdir(parents=True, exist_ok=True)

    if img_type == "gif" and len(frames) > 1:
        # Save GIF
        out = output_path.with_suffix(".gif")
        frames[0].save(
            str(out),
            save_all=True,
            append_images=frames[1:],
            duration=frame_delay,
            loop=0,
            disposal=2,
        )
        print(f"  GIF: {out} ({len(frames)} frames, {frame_delay}ms, {frames[0].width}x{frames[0].height})")

        # Also save individual frame PNGs for game engine loading
        for i, frame in enumerate(frames):
            frame_path = output_path.parent / f"{output_path.stem}_frame{i}.png"
            frame.save(str(frame_path))
        print(f"  Frames: {len(frames)} PNGs exported ({output_path.stem}_frame*.png)")
    else:
        out = output_path.with_suffix(".png")
        frames[0].save(str(out))
        print(f"  PNG: {out} ({frames[0].width}x{frames[0].height})")


def process_file(yaml_path: str, palette_path: str, output_path: str, scale: int = 1):
    """Process a single YAML file."""
    palette_data = load_yaml(palette_path)
    global_palettes = palette_data.get("global_palettes", {})

    image_def = load_yaml(yaml_path)
    base_name = image_def.get("base_palette", "")
    local_palette = image_def.get("palette", {})
    palette = build_palette(global_palettes, base_name, local_palette)

    name = image_def.get("metadata", {}).get("name", Path(yaml_path).stem)
    print(f"Processing: {yaml_path} ({name})")
    generate(image_def, palette, Path(output_path), scale)


def process_all(defs_dir: str, palette_path: str, output_dir: str, scale: int = 1):
    """Process all YAML files in a directory."""
    palette_data = load_yaml(palette_path)
    global_palettes = palette_data.get("global_palettes", {})

    defs_path = Path(defs_dir)
    for yaml_file in sorted(defs_path.glob("*.yaml")):
        image_def = load_yaml(str(yaml_file))
        base_name = image_def.get("base_palette", "")
        local_palette = image_def.get("palette", {})
        palette = build_palette(global_palettes, base_name, local_palette)

        name = image_def.get("metadata", {}).get("name", yaml_file.stem)
        img_type = image_def.get("type", "png").lower()
        sub_dir = image_def.get("output_dir", "")
        out_path = Path(output_dir) / sub_dir / name

        print(f"Processing: {yaml_file} ({name})")
        generate(image_def, palette, out_path, scale)


def main():
    parser = argparse.ArgumentParser(description="Generate PNG/GIF from YAML pixel art")
    parser.add_argument("image_yaml", nargs="?", help="Path to image YAML (single file mode)")
    parser.add_argument("--all", metavar="DEFS_DIR", help="Process all YAMLs in directory")
    parser.add_argument("--palette", required=True, help="Path to global palette YAML")
    parser.add_argument("--output", help="Output path (single) or --output-dir (batch)")
    parser.add_argument("--output-dir", help="Output base directory (batch mode)")
    parser.add_argument("--scale", type=int, default=1, help="Scale factor")
    args = parser.parse_args()

    if args.all:
        if not args.output_dir:
            print("--output-dir required with --all", file=sys.stderr)
            sys.exit(1)
        process_all(args.all, args.palette, args.output_dir, args.scale)
    elif args.image_yaml:
        if not args.output:
            print("--output required for single file", file=sys.stderr)
            sys.exit(1)
        process_file(args.image_yaml, args.palette, args.output, args.scale)
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    main()
