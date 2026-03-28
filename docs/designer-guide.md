# Designer Guide

This guide is for **game designers** who create and edit maps and game content. No programming knowledge is required — all map configuration is done through YAML files.

## Overview

The game world is built from a grid of colored tiles, organized into two layers:

| Layer | Role | Passable |
|-------|------|----------|
| **Terrain** | Ground surface (grass, dirt, paths, sand) | Always |
| **Object** | Items placed on top of terrain (water, rocks, fences, trees) | Never |

The player can walk freely on terrain tiles but cannot pass through object tiles.

## Map Config Files

Map definitions are YAML files located in:

```
FarmGame/Content/Maps/
```

Each `.yaml` file represents one complete map. After editing a YAML file, rebuild and run the game to see changes:

```bash
just start
```

## YAML Format Reference

Below is a complete example (`farm.yaml`):

```yaml
name: Farm
description: A peaceful farmland with cross-shaped paths, a small dirt plot for crops, and two water ponds.
width: 40
height: 30
tile_size: 32
player_start: [10, 10]

tileset:
  name: farm_tiles
  tile_width: 32
  tile_height: 32

terrain_colors:
  grass: [34, 139, 34]
  dirt: [139, 119, 101]
  path: [210, 180, 140]
  sand: [238, 214, 175]

object_colors:
  water: [30, 144, 255]
  rock: [128, 128, 128]
  fence: [139, 90, 43]

default_terrain: grass

terrain:
  - type: path
    regions:
      - { x: 0, y: 14, w: 40, h: 2 }
      - { x: 20, y: 0, w: 2, h: 30 }
  - type: dirt
    properties: { is_plantable: true }
    regions:
      - { x: 8, y: 8, w: 6, h: 4 }

objects:
  - type: water
    properties: { is_water: true }
    regions:
      - { x: 30, y: 5, w: 5, h: 4 }
      - { x: 2, y: 22, w: 4, h: 4 }
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | yes | Display name of the map |
| `description` | string | yes | Brief description of the map's theme and layout |
| `width` | int | yes | Map width in tiles |
| `height` | int | yes | Map height in tiles |
| `tile_size` | int | yes | Tile size in pixels (e.g., 32) |
| `player_start` | [x, y] | yes | Tile coordinates where the player spawns |
| `tileset` | map | no | Tileset reference (name, tile_width, tile_height) |
| `default_terrain` | string | yes | Terrain type used to fill the entire map before regions are applied |
| `terrain_colors` | map | yes | RGB color definitions for terrain types |
| `object_colors` | map | yes | RGB color definitions for object types |
| `terrain` | list | no | Terrain region placements (overrides the default terrain) |
| `objects` | list | no | Object region placements |

### Coordinate System

```
(0,0) ────────────→ x (width)
  │
  │   Each unit = 1 tile (32x32 pixels)
  │
  ↓
  y (height)
```

- `x: 0, y: 0` is the **top-left** corner of the map.
- `x` increases to the right, `y` increases downward.

### Defining Colors

Colors are defined as RGB arrays `[R, G, B]`, where each value ranges from 0 to 255.

```yaml
terrain_colors:
  grass: [34, 139, 34]       # forest green
  dirt: [139, 119, 101]      # brown
  path: [210, 180, 140]      # light tan
  sand: [238, 214, 175]      # pale sand
```

Only terrain/object types that appear in the map need a color definition. You can customize colors freely per map.

### Placing Regions

Terrain and object regions are rectangular areas defined by position and size:

```yaml
- type: <type name>
  properties: { is_plantable: true }   # optional custom properties
  regions:
    - { x: <left>, y: <top>, w: <width>, h: <height> }
```

| Parameter | Description |
|-----------|-------------|
| `x` | Left edge (tile column) |
| `y` | Top edge (tile row) |
| `w` | Width in tiles |
| `h` | Height in tiles |

A single type can have multiple regions:

```yaml
- type: water
  regions:
    - { x: 30, y: 5, w: 5, h: 4 }     # pond
    - { x: 2, y: 22, w: 4, h: 4 }     # small lake
```

Regions are applied in order from top to bottom. Later entries overwrite earlier ones when they overlap.

## Available Types

### Terrain Types

| Type | Description | Typical Color |
|------|-------------|---------------|
| `grass` | Default walkable ground | Green |
| `dirt` | Farmable soil | Brown |
| `path` | Roads and walkways | Tan |
| `sand` | Sandy ground | Pale yellow |

### Object Types

| Type | Description | Typical Color |
|------|-------------|---------------|
| `water` | Lakes and ponds | Blue |
| `rock` | Boulders and stones | Gray |
| `fence` | Fences and barriers | Dark brown |
| `tree` | Trees | Dark green |

> Need a new type? Ask a developer to add it to the enum — then you can use it immediately in YAML.

## Creating a New Map

1. Copy an existing map file (e.g., `farm.yaml`) in `FarmGame/Content/Maps/`.
2. Rename it (e.g., `village.yaml`).
3. Edit the YAML fields to define your new map.
4. Ask a developer to point `Game1.cs` to your new map file.
5. Run `just start` to preview.

### Step-by-Step Example

Create a small 20x15 village map:

```yaml
name: Village
description: A small village with a main road, a fenced garden, a pond, and scattered rocks.
width: 20
height: 15
player_start: [10, 7]

terrain_colors:
  grass: [34, 139, 34]
  path: [210, 180, 140]
  dirt: [139, 119, 101]

object_colors:
  rock: [128, 128, 128]
  fence: [139, 90, 43]
  water: [30, 144, 255]

default_terrain: grass

terrain:
  # Main road running horizontally
  - type: path
    regions:
      - { x: 0, y: 7, w: 20, h: 1 }

  # Small garden
  - type: dirt
    regions:
      - { x: 14, y: 2, w: 4, h: 3 }

objects:
  # Fence around the garden
  - type: fence
    regions:
      - { x: 13, y: 1, w: 6, h: 1 }    # top
      - { x: 13, y: 5, w: 6, h: 1 }    # bottom
      - { x: 13, y: 1, w: 1, h: 5 }    # left
      - { x: 18, y: 1, w: 1, h: 5 }    # right

  # Small pond
  - type: water
    regions:
      - { x: 2, y: 10, w: 3, h: 3 }

  # Scattered rocks
  - type: rock
    regions:
      - { x: 8, y: 3, w: 1, h: 1 }
      - { x: 5, y: 12, w: 1, h: 1 }
```

## Tips

- **Start small**: Use a small map (e.g., 20x15) to quickly iterate on layout ideas.
- **Layer order matters**: The entire map is filled with `default_terrain` first, then terrain regions are painted on top, then objects are placed last.
- **Objects block movement**: Make sure the player spawn (`player_start`) is not inside an object region.
- **Colors are per-map**: Each map can define its own color palette, allowing different visual themes.
- **Single-tile objects**: Use `w: 1, h: 1` to place individual tiles (e.g., a single rock or tree).
