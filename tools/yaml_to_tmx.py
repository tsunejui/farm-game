#!/usr/bin/env python3
"""
yaml_to_tmx.py — Convert YAML map source files to Tiled TMX + TSX format.

Usage:
    python3 tools/yaml_to_tmx.py <yaml_file> \
        --terrains-dir <terrains_dir> \
        --items-dir <items_dir> \
        --output <output_dir>
"""

import argparse
import os
import sys
import xml.etree.ElementTree as ET

import yaml


def parse_args():
    parser = argparse.ArgumentParser(description="Convert YAML map to TMX + TSX")
    parser.add_argument("yaml_file", help="Path to the YAML map file")
    parser.add_argument("--terrains-dir", "-t", required=True, help="Path to Terrains YAML directory")
    parser.add_argument("--items-dir", "-i", required=True, help="Path to Items YAML directory")
    parser.add_argument("--output", "-o", required=True, help="Output directory")
    return parser.parse_args()


def load_yaml(path):
    with open(path, "r") as f:
        return yaml.safe_load(f)


def load_all_defs(directory, id_field):
    """Load all YAML files from directory, keyed by metadata.<id_field>."""
    defs = {}
    if not os.path.isdir(directory):
        return defs
    for filename in os.listdir(directory):
        if not filename.endswith(".yaml"):
            continue
        data = load_yaml(os.path.join(directory, filename))
        def_id = data.get("metadata", {}).get(id_field, "")
        if def_id:
            defs[def_id] = data
    return defs


def build_tile_catalog(map_data, terrains, items):
    """Build tile catalog from terrain and item definitions."""
    catalog = []
    type_to_gid = {}

    # Collect terrain types used in this map
    default_terrain = map_data.get("config", {}).get("default_terrain", "grass")
    terrain_ids = {default_terrain}
    for entry in map_data.get("terrains", []):
        terrain_ids.add(entry["terrain"])

    for tid in sorted(terrain_ids):
        gid = len(catalog) + 1
        type_to_gid[("terrain", tid)] = gid
        terrain_def = terrains.get(tid, {})
        color = terrain_def.get("visuals", {}).get("color", "#FF00FF")
        catalog.append({
            "id": len(catalog), "gid": gid, "layer": "terrain",
            "type": tid, "properties": {},
            "color": color,
        })

    # Collect item types used in this map
    seen_items = set()
    for entity in map_data.get("entities", []):
        item_id = entity.get("item", "")
        if item_id in seen_items:
            continue
        seen_items.add(item_id)

        item_def = items.get(item_id, {})
        color = item_def.get("visuals", {}).get("color", "#FF00FF")
        is_collidable = item_def.get("physics", {}).get("is_collidable", False)

        gid = len(catalog) + 1
        type_to_gid[("entity", item_id)] = gid
        catalog.append({
            "id": len(catalog), "gid": gid, "layer": "object",
            "type": item_id,
            "properties": {"is_collidable": is_collidable},
            "color": color,
        })

    return catalog, type_to_gid


def build_grids(map_data, items, type_to_gid):
    """Build terrain and object grids from map data."""
    config = map_data.get("config", {})
    width = config["width"]
    height = config["height"]
    default_terrain = config.get("default_terrain", "grass")
    default_gid = type_to_gid[("terrain", default_terrain)]

    terrain_grid = [[default_gid] * width for _ in range(height)]
    object_grid = [[0] * width for _ in range(height)]

    # Terrain placements
    for entry in map_data.get("terrains", []):
        tid = entry["terrain"]
        if ("terrain", tid) not in type_to_gid:
            continue
        gid = type_to_gid[("terrain", tid)]
        for r in entry.get("regions", []):
            for y in range(r["y"], r["y"] + r["h"]):
                for x in range(r["x"], r["x"] + r["w"]):
                    if 0 <= x < width and 0 <= y < height:
                        terrain_grid[y][x] = gid

    # Entity placements
    for entity in map_data.get("entities", []):
        item_id = entity.get("item", "")
        if ("entity", item_id) not in type_to_gid:
            continue
        gid = type_to_gid[("entity", item_id)]
        item_def = items.get(item_id, {})

        ew = item_def.get("physics", {}).get("occupy_width", 1)
        eh = item_def.get("physics", {}).get("occupy_height", 1)
        props = entity.get("properties", {})
        ew = props.get("fill_width", ew)
        eh = props.get("fill_height", eh)

        tx = entity["tile_x"]
        ty = entity["tile_y"]
        for y in range(ty, ty + eh):
            for x in range(tx, tx + ew):
                if 0 <= x < width and 0 <= y < height:
                    object_grid[y][x] = gid

    return terrain_grid, object_grid


def grid_to_csv(grid):
    lines = []
    for i, row in enumerate(grid):
        line = ",".join(str(g) for g in row)
        if i < len(grid) - 1:
            line += ","
        lines.append(line)
    return "\n".join(lines)


def generate_tsx(catalog, tileset_name, tile_size):
    count = len(catalog)
    cols = min(count, 4)
    tileset = ET.Element("tileset", {
        "name": tileset_name,
        "tilewidth": str(tile_size), "tileheight": str(tile_size),
        "tilecount": str(count), "columns": str(cols),
    })
    ET.SubElement(tileset, "image", {
        "source": f"{tileset_name}.png",
        "width": str(cols * tile_size),
        "height": str(((count + cols - 1) // cols) * tile_size),
    })

    for tile in catalog:
        tile_el = ET.SubElement(tileset, "tile", {"id": str(tile["id"])})
        props_el = ET.SubElement(tile_el, "properties")
        ET.SubElement(props_el, "property", {"name": "type", "value": tile["type"]})
        ET.SubElement(props_el, "property", {"name": "layer", "value": tile["layer"]})
        ET.SubElement(props_el, "property", {
            "name": "color", "value": tile["color"]
        })
        for k, v in tile["properties"].items():
            attrs = {"name": k}
            if isinstance(v, bool):
                attrs["type"] = "bool"
                attrs["value"] = "true" if v else "false"
            else:
                attrs["value"] = str(v)
            ET.SubElement(props_el, "property", attrs)

    return tileset


def generate_tmx(map_data, terrain_grid, object_grid, tsx_filename):
    config = map_data["config"]
    w, h, ts = config["width"], config["height"], config["tile_size"]

    map_el = ET.Element("map", {
        "version": "1.10", "tiledversion": "1.12.1",
        "orientation": "orthogonal", "renderorder": "right-down",
        "width": str(w), "height": str(h),
        "tilewidth": str(ts), "tileheight": str(ts), "infinite": "0",
    })
    ET.SubElement(map_el, "tileset", {"firstgid": "1", "source": tsx_filename})

    for name, grid in [("terrain", terrain_grid), ("objects", object_grid)]:
        layer = ET.SubElement(map_el, "layer", {
            "name": name, "width": str(w), "height": str(h),
        })
        data = ET.SubElement(layer, "data", {"encoding": "csv"})
        data.text = "\n" + grid_to_csv(grid) + "\n"

    ps = config.get("player_start", [0, 0])
    grp = ET.SubElement(map_el, "objectgroup", {"name": "spawns"})
    ET.SubElement(grp, "object", {
        "name": "player_start",
        "x": str(ps[0] * ts), "y": str(ps[1] * ts),
        "width": str(ts), "height": str(ts),
    })

    return map_el


def write_xml(element, path):
    data_texts = {}
    for el in element.iter("data"):
        data_texts[el] = el.text
        el.text = "PLACEHOLDER"
    tree = ET.ElementTree(element)
    ET.indent(tree, space=" ")
    for el, text in data_texts.items():
        el.text = text
    tree.write(path, encoding="unicode", xml_declaration=True)


def main():
    args = parse_args()
    map_data = load_yaml(args.yaml_file)
    terrains = load_all_defs(args.terrains_dir, "terrain_id")
    items = load_all_defs(args.items_dir, "item_id")

    for field in ["metadata", "config"]:
        if field not in map_data:
            print(f"Error: missing '{field}' in {args.yaml_file}", file=sys.stderr)
            sys.exit(1)

    os.makedirs(args.output, exist_ok=True)

    map_id = map_data["metadata"]["map_id"]
    config = map_data["config"]
    tileset_name = f"{map_id}_tiles"

    catalog, type_to_gid = build_tile_catalog(map_data, terrains, items)
    terrain_grid, object_grid = build_grids(map_data, items, type_to_gid)

    tsx_file = f"{tileset_name}.tsx"
    write_xml(generate_tsx(catalog, tileset_name, config["tile_size"]),
              os.path.join(args.output, tsx_file))
    print(f"Generated: {os.path.join(args.output, tsx_file)}")

    tmx_file = f"{map_id}.tmx"
    write_xml(generate_tmx(map_data, terrain_grid, object_grid, tsx_file),
              os.path.join(args.output, tmx_file))
    print(f"Generated: {os.path.join(args.output, tmx_file)}")


if __name__ == "__main__":
    main()
