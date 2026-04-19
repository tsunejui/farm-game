# FarmGame

A Stardew Valley-inspired 2D farming game built with MonoGame and .NET 9.

## Build & Run

```bash
mise install          # Install dotnet, just, litecli, git-filter-repo, vendir
just build            # Compile
just start            # Run the game (also generates .env.local)
just clean            # Clean build artifacts
just release          # Build for all platforms
just image-generate   # Generate game PNGs/GIFs from YAML palette definitions
```

### AI Sprite Pipeline

Generate 8-direction pixel sprites from a reference image via TripoSR -> Blender -> pixelation -> YAML:

```bash
brew install --cask blender            # One-time (GUI tool, not in mise)
just sprite-setup                      # vendir sync TripoSR + pip install deps
just sprite-generate <name> <input.png> [category]  # Full pipeline
just sprite-pixelate <name> [size] [colors]         # Re-run pixelation only
just sprite-clean                      # Clean build/sprite_pipeline/
```

Intermediate files land in `build/sprite_pipeline/<name>/` (model.obj, renders/, pixelated/); final YAML goes to `assets/images/<category>_<name>/`, which `just image-generate` picks up.

## Architecture

- **Data-driven design**: Terrain, items, and maps defined in YAML configs, loaded by `DataRegistry`
- **Map system**: `GameMap` built from `MapDefinition` via `MapBuilder`, using string-based terrain/item IDs
- **Object system**: Everything in the world is a `WorldObject` with `ObjectState` (HP, faction, damage). Objects have categories: `Item` (rocks, trees, boxes) or `Creature` (player)
- **Spatial indexing**: `GameMap._objectGrid` provides O(1) object lookup by tile coordinate; `WorldObject` pre-computes `EffectiveWidth`/`EffectiveHeight` at construction
- **Action system**: Player behaviors (movement, jump, attack) are separate `IPlayerAction` implementations
- **Persistence**: SQLite database via sqlite-net-pcl ORM for player state and settings
- **Game loop**: MonoGame standard `Initialize → LoadContent → [Update → Draw]`
- **Screen lifecycle**: `IScreen` interface with `Initialize`, `Rebuild`, `OnEnter`, `OnExit`, `Update`, `Draw`
- **Texture cache**: `Game1._textureCache` prevents duplicate disk reads; `UnloadTextures()` disposes on exit
- **Auto-save**: `PlayingScreen` saves every N seconds (configurable via `save.auto_save_interval` in config.yaml)
- **Localization**: JSON language packs in `Content/Locales/<lang>/`, supports English and Chinese
- **HUD**: In-game overlays (map transition, toast alerts) with FontStashSharp Unicode rendering; timing configurable via `hud.*` in config.yaml
- **State machine**: TitleScreen → Settings → Playing → Paused

## Key Directories

- `game/FarmGame/Core/` — GameConstants, GameState, ColorHelper, LocaleManager, FontManager
- `game/FarmGame/Data/` — DataRegistry, GameConfig, terrain/item/map definitions
- `game/FarmGame/World/` — GameMap, MapBuilder, WorldObject, ObjectState
- `game/FarmGame/Combat/` — DamagePipeline, DamageContext, IDamageStep, Steps/
- `game/FarmGame/Entities/` — Player coordinator, Direction enum (8-direction: Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight)
- `game/FarmGame/Entities/Actions/` — IPlayerAction interface, ActionDrawContext
- `game/FarmGame/Entities/Actions/Player/` — MovementAction, JumpAction, AttackAction
- `game/FarmGame/Persistence/` — DatabaseBootstrapper, DatabasePathResolver, MigrationManager
- `game/FarmGame/Persistence/Models/` — PlayerState, PlayerStateRecord, MapStateRecord, MapObjectRecord, Setting, SchemaVersion
- `game/FarmGame/Persistence/Repositories/` — PlayerStateRepository, MapStateRepository, SettingRepository
- `game/FarmGame/Camera/` — 2D camera with boundary clamping
- `game/FarmGame/Screens/` — TitleScreen, PauseScreen, SettingsScreen (Myra UI)
- `game/FarmGame/Screens/HUD/` — MapTransitionOverlay, ToastAlert (in-game overlays)
- `game/FarmGame/Content/` — config.yaml, Maps/, Terrains/, Items/, Locales/, Fonts/, Images/
- `assets/images/` — Source YAML palette-encoded pixel art definitions (consumed by `tools/pixel_artify.py`)
- `tools/sprite_pipeline/` — AI sprite generation pipeline (triposr_convert, blender_render, pixelate, yaml_export, run_pipeline)
- `tools/` — Python tools: pixel_artify.py, png_to_yaml.py, compress_and_yaml.py, yaml_to_tmx.py
- `vendor/TripoSR/` — Vendored image-to-3D model (pinned via vendir, not a submodule)
- `justfiles/` — Modular just recipes (db, download, env, generate, map, release, sprite)

## Conventions

- All rendering uses a 1x1 white pixel texture tinted with colors (no sprite assets yet)
- Game configuration loaded from `Content/config.yaml` at startup
- Terrain types defined in `Content/Terrains/*.yaml` (always passable)
- Item types defined in `Content/Items/*.yaml` (collidable flag per item)
- Adding new types: create a YAML definition file, then use in map configs
- Database stored at platform-specific path (see DatabasePathResolver)
- Player UUID generated on first launch, stored in `setting` table
- Sprites are defined as YAML palette-encoded files in `assets/images/<category>/*.yaml`; `just image-generate` renders them to `game/FarmGame/Content/Images/` via Pillow
- Third-party source code is vendored under `vendor/` via vendir (`vendir.yml`, pinned by commit SHA), not git submodules

## Controls

| Key | Action |
|-----|--------|
| Arrow Keys | Move player (8-direction: combine two keys for diagonal) |
| Space | Jump |
| Z | Attack |
| ESC | Pause / Resume |
| Enter | Confirm menu selection |

## Skills

- `.claude/skills/create-map.md` — Create a new map YAML config
- `.claude/skills/create-item.md` — Create a new item/object YAML definition
- `.claude/skills/create-terrain.md` — Create a new terrain YAML definition
- `.claude/skills/create-effect.md` — Create a new effect (buff/debuff/aura)

## Documentation

- [Developer Guide](docs/developer-guide.md) — For programmers
- [Designer Guide](docs/designer-guide.md) — For game designers (YAML editing)
- [Project Structure](docs/project-structure.md) — Codebase architecture
