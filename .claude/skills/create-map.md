# Create Map

When the user asks to create a new map, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Map ID**: unique snake_case identifier (used as filename)
- **Display Name**: human-readable name
- **Dimensions**: width and height in tiles (default: 30x20)
- **Default Terrain**: base terrain_id that fills the entire map (default: "grass"). Must match an existing file in `game/FarmGame/Content/Terrains/`
- **Player Start**: [x, y] tile coordinates (default: center of map)
- **Terrain regions**: list of terrain placements (terrain_id + rectangles)
- **Entities**: list of items to place (item_id + tile coordinates + optional properties)

## 2. Validate References

Before writing the file:
- Check that all referenced `terrain` IDs exist in `game/FarmGame/Content/Terrains/<id>.yaml`
- Check that all referenced `item` IDs exist in `game/FarmGame/Content/Items/<id>.yaml`
- Ensure `player_start` is within the map bounds (0 to width-1, 0 to height-1)
- Ensure all entity positions and terrain regions are within map bounds

## 3. Create the YAML File

Write the map file to `game/FarmGame/Content/Maps/<map_id>.yaml` using this format:

```yaml
metadata:
  map_id: "<map_id>"
  display_name: "<Display Name>"

config:
  width: <width>
  height: <height>
  tile_size: 32
  default_terrain: <terrain_id>
  player_start: [<x>, <y>]

terrains:
  - terrain: "<terrain_id>"
    regions:
      - { x: <x>, y: <y>, w: <w>, h: <h> }

entities:
  - item: "<item_id>"
    tile_x: <x>
    tile_y: <y>
    properties:          # optional, only if needed
      <key>: <value>
```

## 4. Key Rules

- `tile_size` is always 32
- Coordinate system: (0,0) = top-left, x goes right, y goes down
- Terrain regions are rectangles `{x, y, w, h}` in tile units; later entries overwrite earlier ones on overlap
- Entities reference item_ids from `Content/Items/`; terrains reference terrain_ids from `Content/Terrains/`
- `properties` on entities are optional instance-specific overrides (e.g., `fill_width`/`fill_height` for water_body, `target_map`/`spawn_tile_x`/`spawn_tile_y` for portal)
- Available terrain categories: natural, farmable, constructed
- Available item categories: natural_resource, structure, terrain_feature, transport

## 5. Existing References

Current terrains: grass, dirt, path, sand
Current items: water_body, rock, tree, fence, portal
