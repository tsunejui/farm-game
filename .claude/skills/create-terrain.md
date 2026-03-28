# Create Terrain

When the user asks to create a new terrain type, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Terrain ID**: unique snake_case identifier (used as filename)
- **Display Name**: human-readable name
- **Category**: natural | farmable | constructed
- **Color**: hex color code (#RRGGBB)
- **Properties**: optional key-value pairs (e.g., `is_plantable: true`)

## 2. Validate

- Ensure terrain_id is unique: check no file exists at `game/FarmGame/Content/Terrains/<terrain_id>.yaml`
- Terrain is always walkable (no collision settings needed)

## 3. Create the YAML File

Write the terrain file to `game/FarmGame/Content/Terrains/<terrain_id>.yaml` using this format:

### Minimal (no custom properties):
```yaml
metadata:
  terrain_id: "<terrain_id>"
  display_name: "<Display Name>"
  category: "<category>"

visuals:
  color: "<#RRGGBB>"
```

### With properties:
```yaml
metadata:
  terrain_id: "<terrain_id>"
  display_name: "<Display Name>"
  category: "<category>"

visuals:
  color: "<#RRGGBB>"

properties:
  <key>: <value>
```

## 4. Key Rules

- `terrain_id` must match the filename (without .yaml)
- Terrain is always passable (walkable) - no collision settings
- Color is a hex string like "#228B22"
- Properties are optional runtime flags queryable via `GameMap.HasProperty(x, y, name)`
- Categories: `natural` (grass, sand), `farmable` (dirt with is_plantable), `constructed` (paths, floors)
- No C# code changes needed to add new terrains - just create the YAML file and reference it in maps

## 5. Existing Terrains for Reference

| terrain_id | category | color   | properties        |
|------------|----------|---------|-------------------|
| grass      | natural  | #228B22 | (none)            |
| dirt       | farmable | #8B7765 | is_plantable: true|
| path       | constructed | #D2B48C | (none)         |
| sand       | natural  | #EED6AF | (none)            |
