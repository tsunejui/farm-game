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


def convert(input_path, output_path, resolution=256, remove_bg=True, device=None):
    import numpy as np
    from PIL import Image

    if not os.path.exists(input_path):
        print(f"Error: Input file not found: {input_path}")
        sys.exit(1)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    print(f"Loading TripoSR model (device={device or 'auto'})...")
    import torch
    from tsr.system import TSR
    from tsr.utils import remove_background, resize_foreground

    if device is None:
        # torchmcubes (used by extract_mesh) only supports CUDA or CPU.
        # MPS is not supported, so fall back to CPU on non-CUDA systems.
        device = "cuda:0" if torch.cuda.is_available() else "cpu"

    model = TSR.from_pretrained(
        "stabilityai/TripoSR",
        config_name="config.yaml",
        weight_name="model.ckpt",
    )
    model.renderer.set_chunk_size(8192)
    model.to(device)

    print("Processing input image...")
    if remove_bg:
        import rembg
        rembg_session = rembg.new_session()
        image = remove_background(Image.open(input_path), rembg_session)
        image = resize_foreground(image, 0.85)
        image = np.array(image).astype(np.float32) / 255.0
        image = image[:, :, :3] * image[:, :, 3:4] + (1 - image[:, :, 3:4]) * 0.5
        image = Image.fromarray((image * 255.0).astype(np.uint8))
    else:
        image = Image.open(input_path).convert("RGB")

    print("Running inference...")
    with torch.no_grad():
        scene_codes = model([image], device=device)

    print(f"Extracting mesh (resolution={resolution}) with vertex colors...")
    meshes = model.extract_mesh(scene_codes, True, resolution=resolution)
    mesh = meshes[0]

    # PLY preserves per-vertex colors natively and is handled reliably by Blender.
    ext = os.path.splitext(output_path)[1].lower()
    if ext == ".ply":
        mesh.export(output_path, file_type="ply")
    elif ext in (".glb", ".gltf"):
        mesh.export(output_path, file_type="glb")
    else:
        mesh.export(output_path)
    print(f"Saved 3D model: {output_path}")


def main():
    parser = argparse.ArgumentParser(description="Convert 2D image to 3D model via TripoSR")
    parser.add_argument("--input", required=True, help="Input image path (PNG/JPG)")
    parser.add_argument("--output", required=True, help="Output mesh path (.obj)")
    parser.add_argument("--resolution", type=int, default=256,
                        help="Marching cubes resolution (default: 256)")
    parser.add_argument("--no-remove-bg", action="store_true",
                        help="Skip background removal (default: remove)")
    parser.add_argument("--device", default=None,
                        help="Device: cuda, mps, cpu (auto-detected if omitted)")
    args = parser.parse_args()

    convert(args.input, args.output, args.resolution, not args.no_remove_bg, args.device)


if __name__ == "__main__":
    main()
