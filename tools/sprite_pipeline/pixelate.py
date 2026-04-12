#!/usr/bin/env python3
"""
pixelate.py — Downscale and color-quantize high-res renders into pixel art style

Reuses the LANCZOS downscale + quantize approach from compress_and_yaml.py.

Usage:
    python3 tools/sprite_pipeline/pixelate.py \
        --input-dir build/sprite_pipeline/chicken/renders/ \
        --output-dir build/sprite_pipeline/chicken/pixelated/ \
        --size 64 \
        --colors 32
"""

import argparse
import os
import sys
from PIL import Image


def pixelate(input_path, output_path, max_dim=64, color_count=32):
    """Downscale and color-quantize a single RGBA PNG to pixel art style."""
    img = Image.open(input_path).convert("RGBA")
    w, h = img.size

    # 1. Downscale (preserve aspect ratio)
    if max(w, h) > max_dim:
        scale = max_dim / max(w, h)
        new_w = max(1, int(w * scale))
        new_h = max(1, int(h * scale))
        img = img.resize((new_w, new_h), Image.Resampling.LANCZOS)

    # 2. Separate alpha channel
    alpha = img.getchannel("A")

    # 3. Color quantize RGB
    img_rgb = img.convert("RGB").quantize(colors=color_count).convert("RGB")

    # 4. Re-apply alpha (threshold at 128)
    result = img_rgb.convert("RGBA")
    result_pixels = result.load()
    alpha_pixels = alpha.load()
    width, height = result.size

    for y in range(height):
        for x in range(width):
            r, g, b, _ = result_pixels[x, y]
            a = 255 if alpha_pixels[x, y] >= 128 else 0
            result_pixels[x, y] = (r, g, b, a)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    result.save(output_path)
    return output_path


def process_directory(input_dir, output_dir, max_dim=64, color_count=32):
    """Process all PNGs in input directory."""
    if not os.path.exists(input_dir):
        print(f"Error: Input directory not found: {input_dir}")
        sys.exit(1)

    os.makedirs(output_dir, exist_ok=True)

    files = sorted(f for f in os.listdir(input_dir) if f.lower().endswith(".png"))
    if not files:
        print(f"Warning: No PNG files found in {input_dir}")
        return

    for filename in files:
        input_path = os.path.join(input_dir, filename)
        output_path = os.path.join(output_dir, filename)

        # Skip empty/fully-transparent images
        img = Image.open(input_path).convert("RGBA")
        if img.getextrema()[3][1] == 0:
            print(f"  Skipping {filename} (fully transparent)")
            continue

        pixelate(input_path, output_path, max_dim, color_count)
        print(f"  Pixelated: {filename}")

    print(f"Done! Output in {output_dir}")


def main():
    parser = argparse.ArgumentParser(description="Downscale + quantize renders to pixel art")
    parser.add_argument("--input-dir", required=True, help="Directory with high-res renders")
    parser.add_argument("--output-dir", required=True, help="Output directory for pixelated PNGs")
    parser.add_argument("--size", type=int, default=64, help="Max dimension (default: 64)")
    parser.add_argument("--colors", type=int, default=32, help="Color count (default: 32)")
    args = parser.parse_args()

    process_directory(args.input_dir, args.output_dir, args.size, args.colors)


if __name__ == "__main__":
    main()
