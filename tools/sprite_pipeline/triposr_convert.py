#!/usr/bin/env python3
"""
triposr_convert.py — Convert a 2D image to a 3D model using TripoSR

Usage:
    python3 tools/sprite_pipeline/triposr_convert.py \
        --input photo.png \
        --output build/sprite_pipeline/chicken/model.obj \
        --resolution 256 \
        [--remove-bg] \
        [--device cuda|cpu]
"""

import argparse
import os
import sys

# TripoSR is not on PyPI; we vendor it under vendor/TripoSR via `just sprite-setup` (vendir).
# Add it to sys.path so `from tsr.system import TSRSystem` resolves.
_PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
_TRIPOSR_PATH = os.path.join(_PROJECT_ROOT, "vendor", "TripoSR")
if os.path.isdir(_TRIPOSR_PATH) and _TRIPOSR_PATH not in sys.path:
    sys.path.insert(0, _TRIPOSR_PATH)


def convert(input_path, output_path, resolution=256, remove_bg=False, device=None):
    from PIL import Image

    if not os.path.exists(input_path):
        print(f"Error: Input file not found: {input_path}")
        sys.exit(1)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    # Optionally remove background
    img = Image.open(input_path)
    if remove_bg:
        print("Removing background...")
        from rembg import remove
        img = remove(img)

    # Ensure RGBA
    img = img.convert("RGBA")

    print(f"Loading TripoSR model (device={device or 'auto'})...")
    import torch
    from tsr.system import TSRSystem

    if device is None:
        if torch.cuda.is_available():
            device = "cuda:0"
        elif torch.backends.mps.is_available():
            device = "mps"
        else:
            device = "cpu"

    model = TSRSystem.from_pretrained(
        "stabilityai/TripoSR",
        device=device,
    )
    model.renderer.set_chunk_size(8192)

    print("Running inference...")
    scene_codes = model([img], device=device)

    print(f"Extracting mesh (resolution={resolution})...")
    meshes = model.extract_mesh(scene_codes, resolution=resolution)

    meshes[0].export(output_path)
    print(f"Saved 3D model: {output_path}")


def main():
    parser = argparse.ArgumentParser(description="Convert 2D image to 3D model via TripoSR")
    parser.add_argument("--input", required=True, help="Input image path (PNG/JPG)")
    parser.add_argument("--output", required=True, help="Output mesh path (.obj)")
    parser.add_argument("--resolution", type=int, default=256,
                        help="Marching cubes resolution (default: 256)")
    parser.add_argument("--remove-bg", action="store_true",
                        help="Remove background before processing")
    parser.add_argument("--device", default=None,
                        help="Device: cuda, mps, cpu (auto-detected if omitted)")
    args = parser.parse_args()

    convert(args.input, args.output, args.resolution, args.remove_bg, args.device)


if __name__ == "__main__":
    main()
