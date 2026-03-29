# Create Effect

When the user asks to create a new effect, follow these steps:

## 1. Gather Information

Ask the user for the following (provide sensible defaults):
- **Effect ID**: unique snake_case identifier (used as filename and registry key)
- **Display Name**: human-readable name (English)
- **Display Name (zh-TW)**: Chinese name for localization
- **Description (en)**: English description of the effect behavior
- **Description (zh-TW)**: Chinese description
- **Effect Type**: what the effect does — choose from:
  - `modify_damage` — modifies incoming damage (e.g. reduce to 0, halve, amplify)
  - `on_tick` — periodic aura/DoT effect (runs every ~1 second)
  - `both` — combines modify_damage and on_tick
- **Icon**: generate a 16x16 pixel art icon for the effect

## 2. Validate

- Ensure effect_id is unique: check no file exists at `game/FarmGame/Content/Effects/<effect_id>.yaml`
- Ensure no C# class already uses this ID in `game/FarmGame/World/Effects/`

## 3. Create the Effect YAML Definition

Write to `game/FarmGame/Content/Effects/<effect_id>.yaml`:

```yaml
effect_id: "<effect_id>"
description: "<English description>"
image_path: "Images/effects/<effect_id>"
```

## 4. Create the C# Effect Class

Write to `game/FarmGame/World/Effects/<PascalCaseName>Effect.cs`:

```csharp
namespace FarmGame.World.Effects;

public class <PascalCaseName>Effect : IEffect
{
    public string Id => "<effect_id>";
    public string DisplayName => "<Display Name>";

    // For modify_damage type:
    public int ModifyDamage(WorldObject obj, int damage) => /* logic */;

    // For on_tick type:
    public void OnTick(WorldObject owner, GameMap map)
    {
        // Aura/DoT logic here
    }
}
```

### IEffect Interface Methods

| Method | When Called | Purpose |
|--------|-----------|---------|
| `ModifyDamage(obj, damage)` | Before damage is applied (in ApplyEffectsHandler) | Return modified damage. Return 0 to negate. Return damage unchanged to pass through. |
| `OnTick(owner, map)` | Every ~1 second (in RefreshEffectsEvent) | Periodic effects: damage nearby objects, apply buffs, etc. Use `map.Objects` to find targets. Use `obj.EnqueueEvent(new TakeDamageEvent(amount))` to deal damage. |

## 5. Register in EffectRegistry

Add to the static constructor in `game/FarmGame/World/Effects/EffectRegistry.cs`:

```csharp
static EffectRegistry()
{
    Register(new IndestructibleEffect());
    Register(new HighTemperatureEffect());
    Register(new <PascalCaseName>Effect());  // ← add here
}
```

## 6. Add Locale Entries

Add the effect description to both locale files:
- `game/FarmGame/Content/Locales/en/effects.json` — English description
- `game/FarmGame/Content/Locales/zh-TW/effects.json` — Chinese description

## 7. Create Icon Image

Create a 16x16 pixel art YAML definition in `assets/images/effects/<effect_id>.yaml`:

```yaml
metadata:
  name: "<effect_id>"
type: "png"
output_dir: "Images/effects"
palette:
  "R": "#FF0000"
data:
  - |
    ................
    ....RRRR........
    ...
```

Then run `just image-generate` to render to `game/FarmGame/Content/Images/effects/<effect_id>.png`.
The icon appears in the object status panel when the effect is active.

## 8. Key Rules

- `effect_id` must be unique and match the YAML filename
- C# class name should be PascalCase of effect_id + "Effect" (e.g. `high_temperature` → `HighTemperatureEffect`)
- Effects with `modify_damage` are called sequentially through the object's Effects array — order matters
- Effects with `on_tick` should check `obj.State.IsAlive` before dealing damage
- Use `obj.EnqueueEvent(new TakeDamageEvent(amount))` to deal damage (goes through the event queue)
- TTL is configured per-object in item YAML `default_effects`, not in the effect itself
- TTL = 0 means permanent (never expires)

## 9. Optional: Add to Item as Default Effect

If the effect should be applied when an object spawns, add to the item's YAML:

```yaml
logic:
  default_effects:
    - effect_id: "<effect_id>"
      ttl: 0              # 0 = permanent, >0 = seconds until expiry
```

## 10. Existing Effects for Reference

| effect_id | Type | Behavior | Used by |
|-----------|------|----------|---------|
| indestructible | modify_damage | Sets all damage to 0 | dummy |
| high_temperature | on_tick | 50% chance per second to deal 1-3 damage at distance 0 | campfire, firewood |
