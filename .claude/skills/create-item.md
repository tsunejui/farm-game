# Create Item (Object)

When the user asks to create a new item/object, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Item ID**: unique snake_case identifier (used as filename)
- **Display Name**: human-readable name
- **Category**: natural_resource | structure | terrain_feature | transport
- **Color**: hex color code (#RRGGBB)
- **Size**: occupy_width and occupy_height in tiles (default: 1x1)
- **Collidable**: whether it blocks player movement (default: true)
- **Action Handler**: game logic handler (default: "none"). Existing handlers: rock_logic, tree_logic, portal_logic
- **Health**: max_health for destructible items (default: 0 = indestructible)
- **Drops**: items dropped when destroyed (optional)
- **Properties**: custom key-value pairs (optional)

## 2. Validate

- Ensure item_id is unique: check no file exists at `game/FarmGame/Content/Items/<item_id>.yaml`
- If action_handler is not "none", confirm it matches an existing handler or note that new C# code is needed

## 3. Create the YAML File

Write the item file to `game/FarmGame/Content/Items/<item_id>.yaml` using this format:

```yaml
metadata:
  item_id: "<item_id>"
  display_name: "<Display Name>"
  category: "<category>"

visuals:
  color: "<#RRGGBB>"
  origin_point: "top_left"
  background:
    enabled: false

physics:
  occupy_width: <width>
  occupy_height: <height>
  is_collidable: <true|false>

logic:
  action_handler: "<handler>"
  max_health: <health>
  drops:
    - item: "<drop_item_id>"
      amount: <quantity>
  properties:
    <key>: <value>
```

## 4. Key Rules

- `item_id` must match the filename (without .yaml)
- Color is a hex string like "#808080"
- `origin_point`: "top_left" (default) or "bottom_center"
- `background` section: set `enabled: true` only if a background texture is needed; includes `image_path`, `display_mode` (stretch|tile|center), `offset_x`, `offset_y`
- `drops` list is optional; omit or leave empty if item doesn't drop anything
- `properties` is optional; used for custom runtime flags (e.g., `is_water: true`)
- If creating a destructible item, set `max_health > 0` and provide appropriate `drops`
- If a new `action_handler` is needed, inform the user that C# code must be added

## 5. Existing Items for Reference

| item_id    | category         | size | collidable | handler      |
|------------|------------------|------|------------|--------------|
| water_body | terrain_feature  | 1x1  | true       | none         |
| rock       | natural_resource | 1x1  | true       | rock_logic   |
| tree       | natural_resource | 2x2  | true       | tree_logic   |
| fence      | structure        | 1x1  | true       | none         |
| portal     | transport        | 1x1  | false      | portal_logic |
