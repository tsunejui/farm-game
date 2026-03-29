# Create Item (Object)

When the user asks to create a new item/object, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Item ID**: unique snake_case identifier (used as filename)
- **Display Name**: human-readable name (English)
- **Display Name (zh-TW)**: Chinese name for localization
- **Category**: natural_resource | structure | terrain_feature | transport
- **Color**: hex color code (#RRGGBB) or empty for image-only
- **Size**: occupy_width and occupy_height in tiles (default: 1x1)
- **Collidable**: whether it blocks player movement (default: true)
- **Knockbackable**: whether it can be pushed back by attacks (default: false)
- **Action Handler**: game logic handler (default: "none"). Existing handlers: rock_logic, tree_logic, portal_logic
- **Health**: max_health for destructible items (default: 0 = indestructible)
- **Defense**: damage reduction value (default: 0)
- **Faction**: neutral | friendly | enemy (default: "neutral")
- **Drops**: items dropped when destroyed (optional)
- **Properties**: custom key-value pairs (optional)
- **Images**: whether to create state-based images (normal, damaged, dead)

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
    enabled: true
    display_mode: "stretch"
    offset_x: 0
    offset_y: 0
    states:
      normal:
        image_path: "Images/<folder>/<item_id>_normal"
      damaged:
        image_path: "Images/<folder>/<item_id>_damaged"
      dead:
        image_path: "Images/<folder>/<item_id>_dead"

physics:
  occupy_width: <width>
  occupy_height: <height>
  is_collidable: <true|false>
  is_knockbackable: <true|false>

logic:
  action_handler: "<handler>"
  max_health: <health>
  defense: <defense>
  faction: "<neutral|friendly|enemy>"
  drops:
    - item: "<drop_item_id>"
      amount: <quantity>
  properties:
    <key>: <value>
```

## 4. Add Locale Entries

Add the item name to both locale files:
- `game/FarmGame/Content/Locales/en/items.json` — English name
- `game/FarmGame/Content/Locales/zh-TW/items.json` — Chinese name

## 5. Create Images (if applicable)

Place images in `game/FarmGame/Content/Images/<folder>/`:
- `<item_id>_normal.png` — default alive state
- `<item_id>_damaged.png` — while taking damage (cracked/worn look)
- `<item_id>_dead.png` — destroyed/dead state

Image naming convention: `<item_id>_<state>.png`

## 6. Key Rules

- `item_id` must match the filename (without .yaml)
- Color is a hex string like "#808080" (empty string for image-only items)
- `origin_point`: "top_left" (default) or "bottom_center"
- `background.states`: maps state names to image paths. Supported states: `normal`, `damaged`, `dead`
- Each state can optionally override `display_mode`, `offset_x`, `offset_y` (inherits from parent if omitted)
- `faction`: "neutral" (attackable), "friendly" (cannot attack), "enemy" (attackable, may attack back)
- `defense`: reduces incoming damage via the combat pipeline
- `is_knockbackable`: if true, entity is pushed back 1 tile when hit
- `drops` list is optional; omit or leave empty if item doesn't drop anything
- `properties` is optional; used for custom runtime flags (e.g., `is_water: true`)
- If creating a destructible item, set `max_health > 0` and provide appropriate `drops`
- If a new `action_handler` is needed, inform the user that C# code must be added

## 7. Existing Items for Reference

| item_id    | category         | size | collidable | knockbackable | faction  | hp | def | handler      |
|------------|------------------|------|------------|---------------|----------|----|-----|--------------|
| rock       | natural_resource | 1x1  | true       | false          | neutral  | 8  | 2   | rock_logic   |
| tree       | natural_resource | 2x2  | true       | false          | neutral  | 12 | 1   | tree_logic   |
| fence      | structure        | 1x1  | true       | false          | friendly | 5  | 0   | none         |
| water_body | terrain_feature  | 1x1  | true       | false          | neutral  | 0  | 0   | none         |
| portal     | transport        | 1x1  | false      | false          | friendly | 0  | 0   | portal_logic |
| box        | structure        | 1x1  | true       | true           | neutral  | 4  | 0   | none         |
