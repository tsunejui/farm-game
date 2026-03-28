# FarmGame

A Stardew Valley-inspired 2D farming game built with MonoGame and .NET 9.

## Build & Run

```bash
mise install          # Install dotnet, just
just build            # Compile
just start            # Run the game
just clean            # Clean build artifacts
just release          # Build for all platforms
```

## Architecture

- **Two-layer tile map**: Terrain (always walkable) + Objects (impassable), both defined in YAML configs
- **Map configs**: `FarmGame/Content/Maps/*.yaml` — loaded at runtime by `MapLoader`
- **Game loop**: MonoGame standard `Initialize → LoadContent → [Update → Draw]`
- **State machine**: TitleScreen → Playing → Paused

## Key Directories

- `FarmGame/Core/` — Constants, game state enum
- `FarmGame/World/` — TerrainType, ObjectType, TileMap, MapLoader
- `FarmGame/Entities/` — Player movement and rendering
- `FarmGame/Camera/` — 2D camera with boundary clamping
- `FarmGame/Screens/` — Title and pause screen UI
- `FarmGame/Content/Maps/` — YAML map config files

## Conventions

- All rendering uses a 1x1 white pixel texture tinted with colors (no sprite assets yet)
- Map dimensions and colors are per-map in YAML, not in code constants
- Terrain types: Grass, Dirt, Path, Sand (always passable)
- Object types: Water, Rock, Fence, Tree (always impassable)
- Adding new types: add to enum in `World/TerrainType.cs` or `World/ObjectType.cs`, then use in YAML

## Documentation

- [Developer Guide](docs/developer-guide.md) — For programmers
- [Designer Guide](docs/designer-guide.md) — For game designers (YAML map editing)
- [Project Structure](docs/project-structure.md) — Codebase architecture
