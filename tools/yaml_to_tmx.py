#!/usr/bin/env python3
"""
yaml_to_tmx.py — Convert YAML map source files to Tiled TMX + TSX format.

Usage:
    python3 tools/yaml_to_tmx.py <yaml_file> --output <output_dir>

This script reads a YAML map definition with a symbol legend and layer matrices,
then generates:
  - A .tsx tileset file with tile properties (is_plantable, is_water, etc.)
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
    parser.add_argument("--output", "-o", required=True, help="Output directory for TMX/TSX files")
    return parser.parse_args()


def load_yaml(path):
    with open(path, "r") as f:
        return yaml.safe_load(f)


def build_tile_catalog(legend):
    """
    Build a catalog of unique tiles from the legend.
    Returns:
        catalog: list of dicts with keys: symbol, layer, type, properties
        symbol_to_gid: dict mapping symbol -> GID (1-based for TMX)
    """
    catalog = []
    symbol_to_gid = {}

    # Sort legend entries for deterministic GID assignment
    for symbol, definition in sorted(legend.items()):
        gid = len(catalog) + 1  # TMX GIDs are 1-based
        symbol_to_gid[symbol] = gid
        catalog.append({
            "id": len(catalog),  # TSX tile IDs are 0-based
            "gid": gid,
            "symbol": symbol,
            "layer": definition["layer"],
            "type": definition["type"],
            "properties": definition.get("properties", {}),
        })

    return catalog, symbol_to_gid


def parse_layer_matrix(matrix_text, width, height):
    """Parse a text block into a list of row strings, validating dimensions."""
    rows = [r for r in matrix_text.strip().split("\n") if r]
    if len(rows) != height:
        raise ValueError(f"Layer has {len(rows)} rows, expected {height}")
    for i, row in enumerate(rows):
        if len(row) != width:
            raise ValueError(f"Row {i} has length {len(row)}, expected {width}")
    return rows


def matrix_to_csv(rows, symbol_to_gid, layer_type):
    """
    Convert symbol matrix rows to CSV GID data.
    For the terrain layer, every cell must have a valid terrain symbol.
    For the object layer, "." means empty (GID 0).
    """
    all_gids = []
    for row in rows:
        for ch in row:
            if ch == "." and layer_type == "object":
                all_gids.append(0)
            elif ch in symbol_to_gid:
                all_gids.append(symbol_to_gid[ch])
            else:
                all_gids.append(0)
    # Tiled CSV: all values comma-separated, rows split by newline for readability
    width = len(rows[0])
    csv_lines = []
    for i in range(0, len(all_gids), width):
        row_gids = all_gids[i:i + width]
        line = ",".join(str(g) for g in row_gids)
        if i + width < len(all_gids):
            line += ","
        csv_lines.append(line)
    return "\n".join(csv_lines)


def generate_tsx(catalog, tileset_cfg, colors):
    """Generate a TSX (Tileset XML) element tree."""
    tile_count = len(catalog)
    cols = min(tile_count, 4)  # arbitrary column count for layout
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

    # Placeholder image (required by Tiled for valid TSX)
    image_source = tileset_cfg.get("image", f"{tileset_cfg['name']}.png")
    ET.SubElement(tileset, "image", {
        "source": image_source,
        "width": str(img_w),
        "height": str(img_h),
    })

    # Merge terrain_colors and object_colors for lookup
    all_colors = {}
    all_colors.update(colors.get("terrain", {}))
    all_colors.update(colors.get("object", {}))

    for tile_info in catalog:
        tile_el = ET.SubElement(tileset, "tile", {"id": str(tile_info["id"])})
        props_el = ET.SubElement(tile_el, "properties")

        # type property
        ET.SubElement(props_el, "property", {
            "name": "type",
            "value": tile_info["type"],
        })

        # layer property
        ET.SubElement(props_el, "property", {
            "name": "layer",
            "value": tile_info["layer"],
        })

        # color property (for runtime rendering)
        type_name = tile_info["type"]
        if type_name in all_colors:
            rgb = all_colors[type_name]
            ET.SubElement(props_el, "property", {
                "name": "color",
                "value": f"{rgb[0]},{rgb[1]},{rgb[2]}",
            })

        # custom properties (is_plantable, is_water, etc.)
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


def generate_tmx(data, catalog, symbol_to_gid, tsx_filename):
    """Generate a TMX (Map XML) element tree."""
    width = data["width"]
    height = data["height"]
    tw = data["tile_size"]
    th = data["tile_size"]

    map_el = ET.Element("map", {
        "version": "1.10",
        "tiledversion": "1.12.1",
        "orientation": "orthogonal",
        "renderorder": "right-down",
        "width": str(width),
        "height": str(height),
        "tilewidth": str(tw),
        "tileheight": str(th),
        "infinite": "0",
    })

    # Tileset reference
    ET.SubElement(map_el, "tileset", {
        "firstgid": "1",
        "source": tsx_filename,
    })

    # Terrain layer
    terrain_rows = parse_layer_matrix(data["layers"]["terrain"], width, height)
    terrain_csv = matrix_to_csv(terrain_rows, symbol_to_gid, "terrain")
    terrain_layer = ET.SubElement(map_el, "layer", {
        "name": "terrain",
        "width": str(width),
        "height": str(height),
    })
    terrain_data = ET.SubElement(terrain_layer, "data", {"encoding": "csv"})
    terrain_data.text = "\n" + terrain_csv + "\n"

    # Object tile layer
    object_rows = parse_layer_matrix(data["layers"]["objects"], width, height)
    object_csv = matrix_to_csv(object_rows, symbol_to_gid, "object")
    object_layer = ET.SubElement(map_el, "layer", {
        "name": "objects",
        "width": str(width),
        "height": str(height),
    })
    object_data = ET.SubElement(object_layer, "data", {"encoding": "csv"})
    object_data.text = "\n" + object_csv + "\n"

    # Spawn object group
    player_start = data.get("player_start", [0, 0])
    obj_group = ET.SubElement(map_el, "objectgroup", {"name": "spawns"})
    ET.SubElement(obj_group, "object", {
        "name": "player_start",
        "x": str(player_start[0] * tw),
        "y": str(player_start[1] * th),
        "width": str(tw),
        "height": str(th),
    })

    return map_el


def write_xml(element, path):
    """Write XML to file, preserving CSV data content without indentation corruption."""
    # Save and clear text of <data> elements before indenting
    data_texts = {}
    for data_el in element.iter("data"):
        data_texts[data_el] = data_el.text
        data_el.text = "PLACEHOLDER"

    tree = ET.ElementTree(element)
    ET.indent(tree, space=" ")

    # Restore data text
    for data_el, text in data_texts.items():
        data_el.text = text

    tree.write(path, encoding="unicode", xml_declaration=True)


def main():
    args = parse_args()
    data = load_yaml(args.yaml_file)

    # Validate required fields
    for field in ["name", "width", "height", "tile_size", "legend", "layers"]:
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

    # Collect colors for embedding in TSX
    colors = {
        "terrain": data.get("terrain_colors", {}),
        "object": data.get("object_colors", {}),
    }

    # Build tile catalog from legend
    catalog, symbol_to_gid = build_tile_catalog(data["legend"])

    # Generate TSX
    tsx_filename = f"{tileset_cfg['name']}.tsx"
    tsx_el = generate_tsx(catalog, tileset_cfg, colors)
    tsx_path = os.path.join(args.output, tsx_filename)
    write_xml(tsx_el, tsx_path)
    print(f"Generated: {tsx_path}")

    # Generate TMX
    tmx_el = generate_tmx(data, catalog, symbol_to_gid, tsx_filename)
    tmx_path = os.path.join(args.output, f"{map_name}.tmx")
    write_xml(tmx_el, tmx_path)
    print(f"Generated: {tmx_path}")


if __name__ == "__main__":
    main()
