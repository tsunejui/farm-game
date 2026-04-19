#!/usr/bin/env python3
"""
run_pipeline.py — AI Sprite Generation Pipeline entry point

Converts a reference image through TripoSR -> Blender -> pixelation -> YAML export,
producing 8-direction game-ready pixel sprite YAML definitions.

Usage:
    python3 tools/sprite_pipeline/run_pipeline.py \
        --input photo.png \
        --name chicken \
        --category creatures_chicken \
        [--size 64] \
        [--colors 32] \
        [--resolution 256] \
        [--render-resolution 512] \
        [--elevation 30] \
        [--remove-bg] \
        [--device cuda|mps|cpu] \
        [--skip-3d] \
        [--skip-render]
"""

import argparse
import os
import subprocess
import sys
import shutil


def run_step(step_name, cmd):
    """Run a pipeline step, exit on failure."""
    print(f"\n{'='*60}")
    print(f"Step: {step_name}")
    print(f"{'='*60}")
    print(f"Command: {' '.join(cmd)}\n")

    result = subprocess.run(cmd)
    if result.returncode != 0:
        print(f"\nError: Step '{step_name}' failed (exit code {result.returncode})")
        print(f"Intermediate files preserved in build directory for debugging.")
        sys.exit(result.returncode)


def find_blender():
    """Find Blender executable."""
    candidates = [
        "blender",
        "/Applications/Blender.app/Contents/MacOS/Blender",
    ]
    for path in candidates:
        if shutil.which(path):
            return path
        if os.path.isfile(path):
            return path
    return None


def main():
    parser = argparse.ArgumentParser(
        description="AI Sprite Generation Pipeline: image -> 8-direction pixel sprites"
    )
    parser.add_argument("--input", required=True, help="Input image path (PNG/JPG)")
    parser.add_argument("--name", required=True, help="Sprite name (e.g., chicken)")
    parser.add_argument("--category", default="items",
                        help="Asset category (default: items)")
    parser.add_argument("--output-name", default=None,
                        help="Override output directory name (default: <category>_<name>)")
    parser.add_argument("--size", type=int, default=128,
                        help="Pixel art max dimension (default: 128)")
    parser.add_argument("--colors", type=int, default=48,
                        help="Color count after quantization (default: 48)")
    parser.add_argument("--resolution", type=int, default=320,
                        help="TripoSR mesh resolution (default: 320)")
    parser.add_argument("--render-resolution", type=int, default=1024,
                        help="Blender render resolution (default: 1024)")
    parser.add_argument("--elevation", type=float, default=45.0,
                        help="Camera elevation in degrees (default: 45, classic RO 3/4 view)")
    parser.add_argument("--no-remove-bg", action="store_true",
                        help="Skip background removal (default: remove)")
    parser.add_argument("--device", default=None,
                        help="TripoSR device: cuda, mps, cpu (auto-detected)")
    parser.add_argument("--skip-3d", action="store_true",
                        help="Skip TripoSR step (reuse existing model)")
    parser.add_argument("--skip-render", action="store_true",
                        help="Skip Blender step (reuse existing renders)")
    args = parser.parse_args()

    # Validate input
    if not args.skip_3d and not os.path.exists(args.input):
        print(f"Error: Input file not found: {args.input}")
        sys.exit(1)

    # Paths
    project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    build_dir = os.path.join(project_root, "build", "sprite_pipeline", args.name)
    model_path = os.path.join(build_dir, "model.ply")
    renders_dir = os.path.join(build_dir, "renders")
    pixelated_dir = os.path.join(build_dir, "pixelated")
    output_category = args.output_name or f"{args.category}_{args.name}"
    yaml_output_dir = os.path.join(project_root, "assets", "images", output_category)
    tools_dir = os.path.join(project_root, "tools", "sprite_pipeline")

    os.makedirs(build_dir, exist_ok=True)

    print(f"Sprite Pipeline: {args.name}")
    print(f"  Input:    {args.input}")
    print(f"  Build:    {build_dir}")
    print(f"  Output:   {yaml_output_dir}")
    print(f"  Size:     {args.size}px, {args.colors} colors")

    # Step 1: TripoSR (image -> 3D model)
    if not args.skip_3d:
        cmd = [
            sys.executable, os.path.join(tools_dir, "triposr_convert.py"),
            "--input", os.path.abspath(args.input),
            "--output", model_path,
            "--resolution", str(args.resolution),
        ]
        if args.no_remove_bg:
            cmd.append("--no-remove-bg")
        if args.device:
            cmd.extend(["--device", args.device])
        run_step("TripoSR: Image to 3D", cmd)
    else:
        if not os.path.exists(model_path):
            print(f"Error: --skip-3d but model not found: {model_path}")
            sys.exit(1)
        print(f"\nSkipping TripoSR (using existing model: {model_path})")

    # Step 2: Blender (3D model -> 8 direction renders)
    if not args.skip_render:
        blender = find_blender()
        if not blender:
            print("Error: Blender not found. Install with: brew install --cask blender")
            sys.exit(1)

        cmd = [
            blender, "--background", "--python-exit-code", "1", "--python",
            os.path.join(tools_dir, "blender_render.py"),
            "--",
            "--model", model_path,
            "--output-dir", renders_dir,
            "--name", args.name,
            "--resolution", str(args.render_resolution),
            "--elevation", str(args.elevation),
        ]
        run_step("Blender: 8-Direction Render", cmd)
    else:
        if not os.path.exists(renders_dir):
            print(f"Error: --skip-render but renders not found: {renders_dir}")
            sys.exit(1)
        print(f"\nSkipping Blender (using existing renders: {renders_dir})")

    # Step 3: Pixelate (downscale + quantize)
    cmd = [
        sys.executable, os.path.join(tools_dir, "pixelate.py"),
        "--input-dir", renders_dir,
        "--output-dir", pixelated_dir,
        "--size", str(args.size),
        "--colors", str(args.colors),
    ]
    run_step("Pixelate: Downscale + Quantize", cmd)

    # Step 4: YAML export
    cmd = [
        sys.executable, os.path.join(tools_dir, "yaml_export.py"),
        "--input-dir", pixelated_dir,
        "--output-dir", yaml_output_dir,
        "--name", args.name,
        "--category", output_category,
    ]
    run_step("YAML Export: Palette-Encoded Format", cmd)

    # Summary
    print(f"\n{'='*60}")
    print(f"Pipeline Complete!")
    print(f"{'='*60}")
    print(f"  3D Model:    {model_path}")
    print(f"  Renders:     {renders_dir}/")
    print(f"  Pixelated:   {pixelated_dir}/")
    print(f"  YAML Output: {yaml_output_dir}/")
    print(f"\nNext: run 'just image-generate' to produce game-ready PNGs")


if __name__ == "__main__":
    main()
