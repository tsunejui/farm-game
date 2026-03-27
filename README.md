# FarmGame

A Stardew Valley-inspired 2D farming game built with [MonoGame](https://monogame.net/) and .NET 9.

## Features

- Tile-based world with top-down perspective
- Smooth grid-to-grid player movement with interpolation
- Camera system that follows the player with map boundary clamping
- Multiple terrain types: grass, dirt, water, and paths
- Collision detection (water tiles block movement)

## Prerequisites

- [mise](https://mise.jdx.dev/) — manages tool versions automatically

## Getting Started

```bash
# Install tools (dotnet, just)
mise install

# Build the project
just build

# Start the game
just start
```

## Controls

| Key              | Action     |
|------------------|------------|
| WASD / Arrow Keys | Move       |
| ESC              | Quit       |

## Available Commands

```bash
just          # List all available commands
just start    # Start the game
just build    # Build the project
just clean    # Clean build artifacts
```

## Documentation

- [Setup Guide](SETUP.md) — Step-by-step project setup process
- [Project Structure](docs/project-structure.md) — Codebase architecture and module descriptions

## Tech Stack

- **Framework**: MonoGame 3.8 (DesktopGL)
- **Runtime**: .NET 9
- **Language**: C#
- **Tools**: mise, just
