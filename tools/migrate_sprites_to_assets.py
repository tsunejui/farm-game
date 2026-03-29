#!/usr/bin/env python3
"""
Migrate sprite YAMLs from game/FarmGame/Content/Items/sprites/{type}/{name}.yaml
to assets/images/{type}/{name}.yaml format.

Static sprites -> single data entry with type: "png"
Animated sprites (frame files) -> merged into single file with type: "gif"
"""

import os
import re
import yaml
from collections import OrderedDict
from pathlib import Path

# Project root
ROOT = Path(__file__).resolve().parent.parent
SPRITES_DIR = ROOT / "game" / "FarmGame" / "Content" / "Items" / "sprites"
ASSETS_DIR = ROOT / "assets" / "images"

# Frame delay overrides by type directory
FRAME_DELAY_MAP = {
    "campfires": 200,
    "firewoods": 200,
    "trees": 800,
    "oak_trees": 800,
    "pine_trees": 800,
    "birch_trees": 800,
    "apple_trees": 800,
    "portals": 300,
}
DEFAULT_FRAME_DELAY = 800


# Custom YAML loader that handles tag:yaml.org,002:str
class SafeLoaderWithTag(yaml.SafeLoader):
    pass

def _str_constructor(loader, node):
    return loader.construct_scalar(node)

SafeLoaderWithTag.add_constructor('tag:yaml.org,002:str', _str_constructor)


# Custom YAML dumper for our output format
class QuotedDumper(yaml.Dumper):
    pass

def _str_representer(dumper, data):
    if '\n' in data:
        return dumper.represent_scalar('tag:yaml.org,2002:str', data, style='|')
    if data.startswith('#') or data == '':
        return dumper.represent_scalar('tag:yaml.org,2002:str', data, style='"')
    return dumper.represent_scalar('tag:yaml.org,2002:str', data)

QuotedDumper.add_representer(str, _str_representer)


def load_sprite_yaml(path):
    """Load a sprite YAML file, handling the custom tag."""
    with open(path, 'r') as f:
        return yaml.load(f, Loader=SafeLoaderWithTag)


def build_palette_with_transparency(palette):
    """Ensure '.' maps to transparent and is listed first."""
    new_palette = OrderedDict()
    new_palette['.'] = '#00000000'
    for k, v in palette.items():
        if k != '.':
            new_palette[k] = v
    return new_palette


def write_asset_yaml(path, data_dict):
    """Write asset YAML in the expected format."""
    path.parent.mkdir(parents=True, exist_ok=True)

    lines = []

    # metadata
    lines.append('metadata:')
    lines.append(f'  name: "{data_dict["metadata"]["name"]}"')

    # type
    lines.append(f'type: "{data_dict["type"]}"')

    # frame_delay (only for gif)
    if data_dict["type"] == "gif" and "frame_delay" in data_dict:
        lines.append(f'frame_delay: {data_dict["frame_delay"]}')

    # output_dir
    lines.append(f'output_dir: "{data_dict["output_dir"]}"')

    # palette
    lines.append('palette:')
    for k, v in data_dict["palette"].items():
        lines.append(f'  "{k}": "{v}"')

    # data
    lines.append('data:')
    for block in data_dict["data"]:
        lines.append('  - |')
        for row in block.rstrip('\n').split('\n'):
            lines.append(f'    {row}')

    lines.append('')  # trailing newline
    with open(path, 'w') as f:
        f.write('\n'.join(lines))


def extract_pixel_data(sprite_data):
    """Extract pixel data string from the sprite's data field."""
    if isinstance(sprite_data, list):
        # Take the first (and usually only) data block
        text = sprite_data[0]
    else:
        text = sprite_data
    # Strip leading/trailing whitespace but preserve internal structure
    return text.strip()


def remap_pixel_data(pixel_text, old_palette, new_palette):
    """
    Re-encode pixel data from old_palette keys to new_palette keys.
    The new_palette maps color->key (inverted).
    old_palette maps key->color.
    """
    # Build mapping: old_key -> color -> new_key
    color_to_new_key = {}
    for k, v in new_palette.items():
        color_to_new_key[v.lower()] = k

    old_key_to_new_key = {}
    for k, v in old_palette.items():
        color = v.lower()
        if color in color_to_new_key:
            old_key_to_new_key[k] = color_to_new_key[color]
        else:
            old_key_to_new_key[k] = k  # fallback

    # Remap each character
    result = []
    for char in pixel_text:
        if char in old_key_to_new_key:
            result.append(old_key_to_new_key[char])
        else:
            result.append(char)  # keep as-is (e.g., newlines, dots if not in palette)
    return ''.join(result)


def merge_palettes(frame_palettes):
    """
    Merge palettes from multiple frames into a single unified palette.
    Returns: OrderedDict of key->color (with '.' first for transparency).
    """
    # Collect all unique colors (excluding transparency dot)
    color_to_key = OrderedDict()
    color_to_key['#00000000'] = '.'

    # Track all colors across frames
    all_colors = OrderedDict()
    for pal in frame_palettes:
        for k, v in pal.items():
            color = v.lower()
            if color != '#00000000' and color not in all_colors:
                all_colors[color] = k  # prefer first key seen

    # Build merged palette: assign single-char keys
    # Available keys: uppercase, lowercase, digits, and safe extra symbols (excluding '.')
    # Avoid YAML-special chars like : # > | - { } [ ] ' " % @ ` ! & * ?
    available_keys = []
    for c in 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+^~=;<>$/()_,':
        available_keys.append(c)

    merged = OrderedDict()
    merged['.'] = '#00000000'

    key_idx = 0
    color_to_merged_key = {}
    color_to_merged_key['#00000000'] = '.'

    for color, orig_key in all_colors.items():
        if key_idx < len(available_keys):
            new_key = available_keys[key_idx]
            merged[new_key] = color
            color_to_merged_key[color] = new_key
            key_idx += 1
        else:
            print(f"  WARNING: Ran out of palette keys! Skipping color {color}")

    return merged, color_to_merged_key


def process_static_sprite(sprite_path, type_dir):
    """Process a single static sprite file."""
    name = sprite_path.stem
    data = load_sprite_yaml(sprite_path)

    palette = build_palette_with_transparency(data.get('palette', {}))
    pixel_data = extract_pixel_data(data.get('data', ['']))
    output_dir = data.get('output_dir', f'Images/{type_dir}')

    asset_data = {
        'metadata': {'name': name},
        'type': 'png',
        'output_dir': output_dir,
        'palette': palette,
        'data': [pixel_data],
    }

    output_path = ASSETS_DIR / type_dir / f'{name}.yaml'
    write_asset_yaml(output_path, asset_data)
    return output_path


def process_animated_sprite(base_name, frame_files, type_dir):
    """Process animated sprite frames into a single gif asset."""
    # Sort frames by frame number
    frame_files.sort(key=lambda f: int(re.search(r'_frame(\d+)', f.stem).group(1)))

    # Load all frames
    frames = []
    frame_palettes = []
    for fp in frame_files:
        data = load_sprite_yaml(fp)
        frames.append(data)
        frame_palettes.append(data.get('palette', {}))

    # Get frame_delay
    frame_delay = FRAME_DELAY_MAP.get(type_dir, DEFAULT_FRAME_DELAY)

    # Try to read existing asset for frame_delay override
    existing_asset = ASSETS_DIR / type_dir / f'{base_name}.yaml'
    if existing_asset.exists():
        try:
            with open(existing_asset, 'r') as f:
                existing = yaml.safe_load(f)
            if existing and 'frame_delay' in existing:
                # Use the configured override, not the old file
                pass
        except Exception:
            pass

    # Merge palettes
    merged_palette, color_to_key = merge_palettes(frame_palettes)

    # Re-encode each frame's pixel data with the merged palette
    merged_frames = []
    for frame_data in frames:
        pixel_text = extract_pixel_data(frame_data.get('data', ['']))
        old_pal = frame_data.get('palette', {})
        remapped = remap_pixel_data(pixel_text, old_pal, merged_palette)
        merged_frames.append(remapped)

    output_dir = frames[0].get('output_dir', f'Images/{type_dir}')

    asset_data = {
        'metadata': {'name': base_name},
        'type': 'gif',
        'frame_delay': frame_delay,
        'output_dir': output_dir,
        'palette': merged_palette,
        'data': merged_frames,
    }

    output_path = ASSETS_DIR / type_dir / f'{base_name}.yaml'
    write_asset_yaml(output_path, asset_data)
    return output_path


def count_colors_in_file(path):
    """Count palette colors in a YAML file."""
    try:
        with open(path, 'r') as f:
            data = yaml.load(f, Loader=SafeLoaderWithTag)
        if data and 'palette' in data:
            return len(data['palette'])
    except Exception:
        pass
    return 0


def main():
    print("=" * 70)
    print("Migrating sprites from Content/Items/sprites/ to assets/images/")
    print("=" * 70)

    updated_files = set()
    summary = []

    for type_dir in sorted(os.listdir(SPRITES_DIR)):
        type_path = SPRITES_DIR / type_dir
        if not type_path.is_dir():
            continue

        print(f"\n--- {type_dir} ---")

        # Categorize files
        static_files = []
        frame_files = {}  # base_name -> [frame_paths]

        for yaml_file in sorted(type_path.glob('*.yaml')):
            match = re.search(r'_frame(\d+)$', yaml_file.stem)
            if match:
                # Extract base name (remove _frameN)
                base_name = yaml_file.stem[:match.start()]
                if base_name not in frame_files:
                    frame_files[base_name] = []
                frame_files[base_name].append(yaml_file)
            else:
                static_files.append(yaml_file)

        # Process static sprites
        for sf in static_files:
            name = sf.stem
            # Skip if this name has frame files (the static one is the non-animated fallback)
            if name in frame_files:
                # Still process it as static (it's the non-animated variant)
                pass

            old_path = ASSETS_DIR / type_dir / f'{name}.yaml'
            old_size = old_path.stat().st_size if old_path.exists() else 0
            old_colors = count_colors_in_file(old_path) if old_path.exists() else 0

            output_path = process_static_sprite(sf, type_dir)
            new_size = output_path.stat().st_size
            new_colors = count_colors_in_file(output_path)

            updated_files.add(str(output_path.relative_to(ASSETS_DIR)))
            summary.append({
                'file': f'{type_dir}/{name}.yaml',
                'old_size': old_size,
                'new_size': new_size,
                'old_colors': old_colors,
                'new_colors': new_colors,
                'type': 'static',
            })
            print(f"  [static] {name}.yaml: {old_size}B -> {new_size}B, "
                  f"colors: {old_colors} -> {new_colors}")

        # Process animated sprites
        for base_name, frames in sorted(frame_files.items()):
            old_path = ASSETS_DIR / type_dir / f'{base_name}.yaml'
            old_size = old_path.stat().st_size if old_path.exists() else 0
            old_colors = count_colors_in_file(old_path) if old_path.exists() else 0

            output_path = process_animated_sprite(base_name, frames, type_dir)
            new_size = output_path.stat().st_size
            new_colors = count_colors_in_file(output_path)

            updated_files.add(str(output_path.relative_to(ASSETS_DIR)))
            summary.append({
                'file': f'{type_dir}/{base_name}.yaml',
                'old_size': old_size,
                'new_size': new_size,
                'old_colors': old_colors,
                'new_colors': new_colors,
                'type': 'animated',
                'frames': len(frames),
            })
            print(f"  [animated:{len(frames)}f] {base_name}.yaml: {old_size}B -> {new_size}B, "
                  f"colors: {old_colors} -> {new_colors}")

    # Summary
    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)

    total_old = sum(s['old_size'] for s in summary)
    total_new = sum(s['new_size'] for s in summary)
    static_count = sum(1 for s in summary if s['type'] == 'static')
    animated_count = sum(1 for s in summary if s['type'] == 'animated')

    print(f"Files updated:   {len(summary)}")
    print(f"  Static:        {static_count}")
    print(f"  Animated:      {animated_count}")
    print(f"Total old size:  {total_old:,} bytes")
    print(f"Total new size:  {total_new:,} bytes")
    print(f"Size change:     {total_new - total_old:+,} bytes")

    # Color count comparison
    print("\nColor count changes:")
    for s in summary:
        if s['old_colors'] != s['new_colors']:
            print(f"  {s['file']}: {s['old_colors']} -> {s['new_colors']} colors")

    # Find files in assets/images that were NOT updated
    print("\n" + "=" * 70)
    print("FILES IN assets/images/ NOT UPDATED (still old quality):")
    print("=" * 70)
    not_updated = []
    for type_dir in sorted(os.listdir(ASSETS_DIR)):
        type_path = ASSETS_DIR / type_dir
        if not type_path.is_dir():
            continue
        for yaml_file in sorted(type_path.glob('*.yaml')):
            rel = str(yaml_file.relative_to(ASSETS_DIR))
            if rel not in updated_files:
                not_updated.append(rel)

    if not_updated:
        for f in not_updated:
            print(f"  {f}")
        print(f"\nTotal not updated: {len(not_updated)}")
    else:
        print("  (all files were updated)")

    print("\nDone!")


if __name__ == '__main__':
    main()
