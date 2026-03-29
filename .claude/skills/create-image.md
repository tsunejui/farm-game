# Create Pixel Art Image

When the user asks to create a new pixel art image for an item, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Name**: display name for the image
- **Type**: what kind of item (Weapon, Item, Terrain, etc.)
- **Category directory**: the subdirectory under `assets/images/` (e.g., rocks, trees, weapons)
- **Base palette**: which global palette to use from `assets/images/palette.yaml` (metal_set, flesh_set, nature_set, stone_set)
- **Size**: width x height in characters (each character = 1 pixel)
- **Design description**: what the image should look like

## 2. Check Global Palettes

Read `assets/images/palette.yaml` to see available palettes and their character-to-color mappings:

| Palette | Characters |
|---------|------------|
| metal_set | K=dark, S=steel, W=white, G=gold |
| flesh_set | H=skin, R=red, B=blue |
| nature_set | G=green, L=light green, B=brown, D=dark brown, Y=yellow |
| stone_set | K=dark, G=gray, L=light gray, D=dark gray, W=silver |

## 3. Create the YAML File

Write to `assets/images/<category>/<name>.yaml`:

```yaml
# <Name> — <description>
metadata:
  name: "<Name>"
  type: "<Type>"

base_palette: "<palette_name>"

palette:
  ".": "#00000000"      # Transparent (always include)
  # Add image-specific colors here (overrides base_palette)

data: |
  <pixel data - each character maps to a palette color>
```

### Key Rules

- **`.` = transparent**: Always define `.` as `#00000000` in the local palette
- **One char = one pixel**: Each character in `data` maps to one pixel
- **All lines same width**: Pad with `.` to keep lines aligned
- **Local overrides global**: If a character exists in both `base_palette` and `palette`, the local `palette` wins
- **Use `|` for data**: The YAML literal block scalar preserves newlines
- **Hex format**: Colors use `#RRGGBB` (opaque) or `#RRGGBBAA` (with alpha)

## 4. Generate the PNG

After creating the YAML, run:

```bash
just image-generate
```

This generates the PNG to `game/FarmGame/Content/Images/<category>/<name>.png`.

## 5. Link to Item Definition (if applicable)

If this image is for an existing item in `game/FarmGame/Content/Items/`, update its `visuals` section:

```yaml
visuals:
  color: ""                          # Clear color (use image instead)
  origin_point: "top_left"
  background:
    enabled: true
    image_path: "Images/<category>/<name>"   # No .png extension
    display_mode: "stretch"          # stretch | tile | center
    offset_x: 0
    offset_y: 0
```

- Use `stretch` for items that fill their tile area
- Use `tile` for repeating patterns (e.g., water)
- Use `center` for items smaller than their tile area

## 6. Existing Images for Reference

| Category | File | Size | Base Palette | Description |
|----------|------|------|--------------|-------------|
| rocks | rock.yaml | 16x16 | stone_set | Gray boulder |
| trees | tree.yaml | 24x32 | nature_set | Oak tree with trunk |
| fences | fence.yaml | 16x16 | (custom) | Wooden fence posts |
| water | water_body.yaml | 16x16 | (custom) | Water tile pattern |
| portals | portal.yaml | 16x16 | (custom) | Purple portal swirl |
| weapons | excalibur.yaml | 11x12 | metal_set | Legendary sword |

## 7. Adding a New Global Palette

If the user needs a new palette, add it to `assets/images/palette.yaml`:

```yaml
global_palettes:
  new_palette_name:
    "A": "#RRGGBB"    # Description
    "B": "#RRGGBB"    # Description
```
