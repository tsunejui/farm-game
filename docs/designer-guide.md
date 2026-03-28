# Designer Guide

This guide is for **game designers** who create and edit maps and game content. No programming knowledge is required — all content configuration is done through YAML files.

## Overview

The game world is built from a grid of colored tiles. Content is defined in three types of YAML files:

| Definition | Directory | Purpose |
|------------|-----------|---------|
| **Terrain** | `Content/Terrains/*.yaml` | Ground surface types (grass, dirt, paths, sand) — always walkable |
| **Item** | `Content/Items/*.yaml` | Objects placed on the map (water, rocks, fences, trees) — can block movement |
| **Map** | `Content/Maps/*.yaml` | Map layouts that reference terrain and item definitions |

## Terrain Definitions

Located in `game/FarmGame/Content/Terrains/`. Each file defines one terrain type:

```yaml
metadata:
  terrain_id: grass
  display_name: Grass
  category: natural

visuals:
  color: "#228B22"
```

### Available Terrain Types

| File | ID | Description | Color |
|------|----|-------------|-------|
| `grass.yaml` | grass | Default walkable ground | Green |
| `dirt.yaml` | dirt | Farmable soil | Brown |
| `path.yaml` | path | Roads and walkways | Tan |
| `sand.yaml` | sand | Sandy ground | Pale yellow |

To add a new terrain type, create a new YAML file in `Content/Terrains/` following the same format.

## Item Definitions

Located in `game/FarmGame/Content/Items/`. Each file defines one item type:

```yaml
metadata:
  item_id: rock
  display_name: Rock
  category: natural

visuals:
  color: "#808080"

physics:
  occupy_width: 1
  occupy_height: 1
  is_collidable: true
```

### Available Item Types

| File | ID | Collidable | Description |
|------|----|------------|-------------|
| `rock.yaml` | rock | Yes | Boulders and stones |
| `tree.yaml` | tree | Yes | Trees |
| `fence.yaml` | fence | Yes | Fences and barriers |
| `water_body.yaml` | water_body | Yes | Lakes and ponds |
| `portal.yaml` | portal | No | Map transition point |

To add a new item type, create a new YAML file in `Content/Items/` following the same format.

## Map Definitions

Located in `game/FarmGame/Content/Maps/`. Each file defines one complete map:

```yaml
metadata:
  map_id: farm_home
  display_name: Home Farm

config:
  width: 40
  height: 30
  tile_size: 32
  default_terrain: grass
  player_start: [10, 10]

terrains:
  - terrain_id: path
    regions:
      - { x: 0, y: 14, w: 40, h: 2 }
      - { x: 20, y: 0, w: 2, h: 30 }
  - terrain_id: dirt
    regions:
      - { x: 8, y: 8, w: 6, h: 4 }

entities:
  - item: water_body
    tile_x: 30
    tile_y: 5
    properties:
      fill_width: 5
      fill_height: 4
  - item: rock
    tile_x: 8
    tile_y: 3
```

### Map Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `metadata.map_id` | string | yes | Unique map identifier |
| `metadata.display_name` | string | yes | Display name of the map |
| `config.width` | int | yes | Map width in tiles |
| `config.height` | int | yes | Map height in tiles |
| `config.tile_size` | int | yes | Tile size in pixels (e.g., 32) |
| `config.default_terrain` | string | yes | Terrain ID used to fill the entire map |
| `config.player_start` | [x, y] | yes | Tile coordinates where the player spawns |
| `terrains` | list | no | Terrain region placements |
| `entities` | list | no | Item placements at specific positions |

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

### Placing Terrain Regions

Terrain regions are rectangular areas that override the default terrain:

```yaml
terrains:
  - terrain_id: path
    regions:
      - { x: 0, y: 14, w: 40, h: 2 }   # horizontal road
      - { x: 20, y: 0, w: 2, h: 30 }   # vertical road
```

| Parameter | Description |
|-----------|-------------|
| `terrain_id` | References a terrain definition file |
| `x` | Left edge (tile column) |
| `y` | Top edge (tile row) |
| `w` | Width in tiles |
| `h` | Height in tiles |

### Placing Entities

Entities are individual items placed at specific tile positions:

```yaml
entities:
  - item: rock
    tile_x: 8
    tile_y: 3
  - item: water_body
    tile_x: 30
    tile_y: 5
    properties:
      fill_width: 5
      fill_height: 4
```

| Parameter | Description |
|-----------|-------------|
| `item` | References an item definition file |
| `tile_x` | X position (tile column) |
| `tile_y` | Y position (tile row) |
| `properties` | Optional per-instance overrides |

## Creating a New Map

1. Copy an existing map file (e.g., `farm_home.yaml`) in `game/FarmGame/Content/Maps/`.
2. Rename it (e.g., `village.yaml`).
3. Edit the YAML fields to define your new map.
4. Set `start_map` in `Content/config.yaml` to your map's `map_id` to make it the default.
5. Run `just start` to preview.

### Step-by-Step Example

Create a small 20x15 village map:

```yaml
metadata:
  map_id: village
  display_name: Village

config:
  width: 20
  height: 15
  tile_size: 32
  default_terrain: grass
  player_start: [10, 7]

terrains:
  - terrain_id: path
    regions:
      - { x: 0, y: 7, w: 20, h: 1 }
  - terrain_id: dirt
    regions:
      - { x: 14, y: 2, w: 4, h: 3 }

entities:
  - item: fence
    tile_x: 13
    tile_y: 1
  - item: fence
    tile_x: 18
    tile_y: 1
  - item: water_body
    tile_x: 2
    tile_y: 10
    properties:
      fill_width: 3
      fill_height: 3
  - item: rock
    tile_x: 8
    tile_y: 3
```

## Tips

- **Start small**: Use a small map (e.g., 20x15) to quickly iterate on layout ideas.
- **Layer order matters**: The entire map is filled with `default_terrain` first, then terrain regions are applied, then entities are placed.
- **Check collision**: Items with `is_collidable: true` block player movement. Make sure `player_start` is not inside a collidable entity.
- **Colors are per-definition**: Each terrain and item has its own color defined in its YAML file, ensuring visual consistency across all maps.
- **Custom properties**: Use `properties` on entity placements for per-instance configuration (e.g., `fill_width` for water bodies).
