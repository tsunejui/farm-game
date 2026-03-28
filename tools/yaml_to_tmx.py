#!/usr/bin/env python3
"""
yaml_to_tmx.py — Convert YAML map source files to Tiled TMX + TSX format.

Usage:
    python3 tools/yaml_to_tmx.py <yaml_file> --output <output_dir>

Reads a YAML map definition with region-based tile placement and generates:
  - A .tsx tileset file with tile properties
  - A .tmx map file with CSV-encoded layers and an object group for spawns

The output can then be validated and exported to JSON via the Tiled CLI.
"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET

import yaml


def parse_args():
    parser = argparse.ArgumentParser(description="Convert YAML map to TMX + TSX")
    parser.add_argument("yaml_file", help="Path to the YAML map file")
    parser.add_argument("--output", "-o", required=True, help="Output directory")
    return parser.parse_args()


def load_yaml(path):
    with open(path, "r") as f:
        return yaml.safe_load(f)


def build_tile_catalog(data):
    """
    Build a catalog of unique tile types from terrain and object entries.
    Returns:
        catalog: list of tile info dicts (id, gid, layer, type, properties)
        type_to_gid: dict mapping (layer, type) -> GID (1-based)
    """
    catalog = []
    type_to_gid = {}

    # Collect all terrain types (default_terrain first)
    terrain_types = set()
    default_terrain = data.get("default_terrain", "grass")
    terrain_types.add(default_terrain)
    for entry in data.get("terrain", []):
        terrain_types.add(entry["type"])

    for t in sorted(terrain_types):
        props = {}
        # Find properties from terrain entries
        for entry in data.get("terrain", []):
            if entry["type"] == t and "properties" in entry:
                props = entry["properties"]
                break
        gid = len(catalog) + 1
        type_to_gid[("terrain", t)] = gid
        catalog.append({
            "id": len(catalog),
            "gid": gid,
            "layer": "terrain",
            "type": t,
            "properties": props,
        })

    # Collect all object types
    object_types = {}
    for entry in data.get("objects", []):
        if entry["type"] not in object_types:
            object_types[entry["type"]] = entry.get("properties", {})

    for t in sorted(object_types.keys()):
        gid = len(catalog) + 1
        type_to_gid[("object", t)] = gid
        catalog.append({
            "id": len(catalog),
            "gid": gid,
            "layer": "object",
            "type": t,
            "properties": object_types[t],
        })

    return catalog, type_to_gid


def regions_to_grid(data, type_to_gid):
    """
    Convert region-based definitions into two 2D GID grids (terrain + objects).
    """
    width = data["width"]
    height = data["height"]
    default_terrain = data.get("default_terrain", "grass")
    default_gid = type_to_gid[("terrain", default_terrain)]

    # Initialize terrain grid with default
    terrain_grid = [[default_gid] * width for _ in range(height)]

    # Apply terrain regions
    for entry in data.get("terrain", []):
        gid = type_to_gid[("terrain", entry["type"])]
        for r in entry.get("regions", []):
            for y in range(r["y"], r["y"] + r["h"]):
                for x in range(r["x"], r["x"] + r["w"]):
                    if 0 <= x < width and 0 <= y < height:
                        terrain_grid[y][x] = gid

    # Initialize object grid with 0 (empty)
    object_grid = [[0] * width for _ in range(height)]

    # Apply object regions
    for entry in data.get("objects", []):
        gid = type_to_gid[("object", entry["type"])]
        for r in entry.get("regions", []):
            for y in range(r["y"], r["y"] + r["h"]):
                for x in range(r["x"], r["x"] + r["w"]):
                    if 0 <= x < width and 0 <= y < height:
                        object_grid[y][x] = gid

    return terrain_grid, object_grid


def grid_to_csv(grid):
    """Convert a 2D GID grid to Tiled CSV format."""
    csv_lines = []
    for i, row in enumerate(grid):
        line = ",".join(str(g) for g in row)
        if i < len(grid) - 1:
            line += ","
        csv_lines.append(line)
    return "\n".join(csv_lines)


def generate_tsx(catalog, tileset_cfg, colors):
    """Generate a TSX (Tileset XML) element tree."""
    tile_count = len(catalog)
    cols = min(tile_count, 4)
    tw = tileset_cfg["tile_width"]
    th = tileset_cfg["tile_height"]
    img_w = cols * tw
    img_h = ((tile_count + cols - 1) // cols) * th

    tileset = ET.Element("tileset", {
        "name": tileset_cfg["name"],
        "tilewidth": str(tw),
        "tileheight": str(th),
        "tilecount": str(tile_count),
        "columns": str(cols),
    })

    image_source = tileset_cfg.get("image", f"{tileset_cfg['name']}.png")
    ET.SubElement(tileset, "image", {
        "source": image_source,
        "width": str(img_w),
        "height": str(img_h),
    })

    all_colors = {}
    all_colors.update(colors.get("terrain", {}))
    all_colors.update(colors.get("object", {}))

    for tile_info in catalog:
        tile_el = ET.SubElement(tileset, "tile", {"id": str(tile_info["id"])})
        props_el = ET.SubElement(tile_el, "properties")

        ET.SubElement(props_el, "property", {
            "name": "type",
            "value": tile_info["type"],
        })
        ET.SubElement(props_el, "property", {
            "name": "layer",
            "value": tile_info["layer"],
        })

        type_name = tile_info["type"]
        if type_name in all_colors:
            rgb = all_colors[type_name]
            ET.SubElement(props_el, "property", {
                "name": "color",
                "value": f"{rgb[0]},{rgb[1]},{rgb[2]}",
            })

        for key, value in tile_info["properties"].items():
            prop_attrs = {"name": key}
            if isinstance(value, bool):
                prop_attrs["type"] = "bool"
                prop_attrs["value"] = "true" if value else "false"
            elif isinstance(value, int):
                prop_attrs["type"] = "int"
                prop_attrs["value"] = str(value)
            elif isinstance(value, float):
                prop_attrs["type"] = "float"
                prop_attrs["value"] = str(value)
            else:
                prop_attrs["value"] = str(value)
            ET.SubElement(props_el, "property", prop_attrs)

    return tileset


def generate_tmx(data, terrain_grid, object_grid, tsx_filename):
    """Generate a TMX (Map XML) element tree."""
    width = data["width"]
    height = data["height"]
    tw = data["tile_size"]

    map_el = ET.Element("map", {
        "version": "1.10",
        "tiledversion": "1.12.1",
        "orientation": "orthogonal",
        "renderorder": "right-down",
        "width": str(width),
        "height": str(height),
        "tilewidth": str(tw),
        "tileheight": str(tw),
        "infinite": "0",
    })

    ET.SubElement(map_el, "tileset", {
        "firstgid": "1",
        "source": tsx_filename,
    })

    # Terrain layer
    terrain_layer = ET.SubElement(map_el, "layer", {
        "name": "terrain",
        "width": str(width),
        "height": str(height),
    })
    terrain_data = ET.SubElement(terrain_layer, "data", {"encoding": "csv"})
    terrain_data.text = "\n" + grid_to_csv(terrain_grid) + "\n"

    # Object layer
    object_layer = ET.SubElement(map_el, "layer", {
        "name": "objects",
        "width": str(width),
        "height": str(height),
    })
    object_data = ET.SubElement(object_layer, "data", {"encoding": "csv"})
    object_data.text = "\n" + grid_to_csv(object_grid) + "\n"

    # Spawn object group
    player_start = data.get("player_start", [0, 0])
    obj_group = ET.SubElement(map_el, "objectgroup", {"name": "spawns"})
    ET.SubElement(obj_group, "object", {
        "name": "player_start",
        "x": str(player_start[0] * tw),
        "y": str(player_start[1] * tw),
        "width": str(tw),
        "height": str(tw),
    })

    return map_el


def write_xml(element, path):
    """Write XML to file, preserving CSV data content without indentation corruption."""
    data_texts = {}
    for data_el in element.iter("data"):
        data_texts[data_el] = data_el.text
        data_el.text = "PLACEHOLDER"

    tree = ET.ElementTree(element)
    ET.indent(tree, space=" ")

    for data_el, text in data_texts.items():
        data_el.text = text

    tree.write(path, encoding="unicode", xml_declaration=True)


def main():
    args = parse_args()
    data = load_yaml(args.yaml_file)

    for field in ["name", "width", "height", "tile_size"]:
        if field not in data:
            print(f"Error: missing required field '{field}' in {args.yaml_file}", file=sys.stderr)
            sys.exit(1)

    os.makedirs(args.output, exist_ok=True)

    map_name = data["name"].lower().replace(" ", "_")
    tileset_cfg = data.get("tileset", {
        "name": f"{map_name}_tiles",
        "tile_width": data["tile_size"],
        "tile_height": data["tile_size"],
    })

    colors = {
        "terrain": data.get("terrain_colors", {}),
        "object": data.get("object_colors", {}),
    }

    catalog, type_to_gid = build_tile_catalog(data)
    terrain_grid, object_grid = regions_to_grid(data, type_to_gid)

    # Generate TSX
    tsx_filename = f"{tileset_cfg['name']}.tsx"
    tsx_path = os.path.join(args.output, tsx_filename)
    write_xml(generate_tsx(catalog, tileset_cfg, colors), tsx_path)
    print(f"Generated: {tsx_path}")

    # Generate TMX
    tmx_path = os.path.join(args.output, f"{map_name}.tmx")
    write_xml(generate_tmx(data, terrain_grid, object_grid, tsx_filename), tmx_path)
    print(f"Generated: {tmx_path}")


if __name__ == "__main__":
    main()
