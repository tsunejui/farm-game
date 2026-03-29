#!/usr/bin/env python3
"""
Merge sprite YAML data (palette + pixel data) into item YAML files.

For each item YAML in game/FarmGame/Content/Items/ that has background states
with image_path, find the corresponding sprite YAML and embed the palette/data
directly into the item YAML.
"""

import os
import sys
import glob
import re

import yaml


# ---------------------------------------------------------------------------
# Custom YAML handling
# ---------------------------------------------------------------------------

class LiteralStr(str):
    """String subclass that will be dumped in literal block style."""
    pass


def literal_str_representer(dumper, data):
    return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|")


yaml.add_representer(LiteralStr, literal_str_representer)


# When loading sprite YAMLs the data values have the tag `tag:yaml.org,002:str`
# (non-standard). We register a constructor so PyYAML doesn't choke on it.
def _str_constructor(loader, node):
    return loader.construct_scalar(node)


yaml.SafeLoader.add_constructor("tag:yaml.org,002:str", _str_constructor)


# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
ITEMS_DIR = os.path.join(PROJECT_ROOT, "game", "FarmGame", "Content", "Items")
SPRITES_DIR = os.path.join(ITEMS_DIR, "sprites")


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def load_yaml(path):
    with open(path, "r") as f:
        return yaml.safe_load(f)


def load_sprite(path):
    """Load a sprite YAML and return (palette, data, type)."""
    sprite = load_yaml(path)
    palette = sprite.get("palette", {})
    data = sprite.get("data", [])
    stype = sprite.get("type", "png")
    # Convert data strings to LiteralStr so they dump with | style
    data = [LiteralStr(d) if isinstance(d, str) else d for d in data]
    return palette, data, stype


def image_path_to_sprite_path(image_path):
    """
    Convert an image_path like "Images/apple_trees/apple_tree_normal"
    to a sprite YAML path like sprites/apple_trees/apple_tree_normal.yaml
    """
    # image_path: "Images/apple_trees/apple_tree_normal"
    # sprite:     sprites/apple_trees/apple_tree_normal.yaml
    parts = image_path.split("/")
    if len(parts) >= 3 and parts[0] == "Images":
        # e.g. ["Images", "apple_trees", "apple_tree_normal"]
        subdir = parts[1]
        name = parts[2]
        return os.path.join(SPRITES_DIR, subdir, name + ".yaml")
    return None


def find_frame_sprites(image_path):
    """
    For animated states, find frame sprite YAMLs.
    e.g. image_path "Images/apple_trees/apple_tree_a_normal"
    -> sprites/apple_trees/apple_tree_a_normal_frame0.yaml, frame1, frame2, ...
    Returns sorted list of paths, or empty list if none found.
    """
    parts = image_path.split("/")
    if len(parts) < 3 or parts[0] != "Images":
        return []
    subdir = parts[1]
    name = parts[2]
    pattern = os.path.join(SPRITES_DIR, subdir, name + "_frame*.yaml")
    frames = sorted(glob.glob(pattern))
    return frames


def dump_yaml(data, path):
    """Write YAML with literal block style for pixel data."""
    with open(path, "w") as f:
        yaml.dump(data, f, default_flow_style=False, allow_unicode=True,
                  sort_keys=False, width=10000)


# ---------------------------------------------------------------------------
# Main merge logic
# ---------------------------------------------------------------------------

def merge_item(item_path):
    """Merge sprite data into a single item YAML. Returns True if modified."""
    item = load_yaml(item_path)
    if not item:
        return False

    visuals = item.get("visuals")
    if not visuals:
        return False

    background = visuals.get("background")
    if not background:
        return False

    states = background.get("states")
    if not states:
        return False

    modified = False
    item_name = os.path.basename(item_path)

    for state_name, state_data in states.items():
        if not isinstance(state_data, dict):
            continue
        image_path = state_data.get("image_path")
        if not image_path:
            continue

        # Already merged?
        if "palette" in state_data or "frames" in state_data:
            continue

        is_animated = state_data.get("file_type") == "gif"

        if is_animated:
            # Look for frame sprites
            frame_paths = find_frame_sprites(image_path)
            if not frame_paths:
                # Try the single sprite as fallback
                sprite_path = image_path_to_sprite_path(image_path)
                if sprite_path and os.path.exists(sprite_path):
                    frame_paths = [sprite_path]

            if not frame_paths:
                print(f"  SKIP {item_name} state={state_name}: no frame sprites found for {image_path}")
                continue

            frames = []
            for fp in frame_paths:
                palette, data, stype = load_sprite(fp)
                frames.append({"palette": palette, "data": data})
                print(f"  Merged frame: {os.path.basename(fp)}")

            state_data["type"] = stype
            state_data["frames"] = frames
            modified = True

        else:
            # Static sprite
            sprite_path = image_path_to_sprite_path(image_path)
            if not sprite_path or not os.path.exists(sprite_path):
                print(f"  SKIP {item_name} state={state_name}: sprite not found at {sprite_path}")
                continue

            palette, data, stype = load_sprite(sprite_path)
            state_data["type"] = stype
            state_data["palette"] = palette
            state_data["data"] = data
            print(f"  Merged: {os.path.basename(sprite_path)} -> {state_name}")
            modified = True

    if modified:
        dump_yaml(item, item_path)

    return modified


def main():
    item_files = sorted(glob.glob(os.path.join(ITEMS_DIR, "*.yaml")))
    print(f"Found {len(item_files)} item YAML files\n")

    merged_count = 0
    for item_path in item_files:
        name = os.path.basename(item_path)
        # Skip example
        if name == "example.yaml":
            continue

        print(f"Processing: {name}")
        if merge_item(item_path):
            merged_count += 1
            print(f"  -> Written\n")
        else:
            print(f"  -> No changes\n")

    print(f"\nDone. Merged sprites into {merged_count} item files.")


if __name__ == "__main__":
    main()
