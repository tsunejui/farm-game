# FarmGame

A Stardew Valley-inspired 2D farming game built with [MonoGame](https://monogame.net/) and .NET 9.

## Features

- Data-driven tile map system with YAML terrain, item, and map definitions
- Smooth grid-to-grid player movement with interpolation
- Action system: movement, jump (Space), attack (Z)
- Camera system that follows the player with map boundary clamping
- SQLite database for player state persistence and settings
- Cross-platform installer support (Windows Inno Setup, macOS DMG)
- Structured logging with daily rolling files

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

| Key | Action |
|-----|--------|
| WASD / Arrow Keys | Move / Navigate menu |
| Space | Jump / Confirm menu |
| Z | Attack |
| Enter | Confirm menu |
| ESC | Pause / Resume |

## Available Commands

```bash
just          # List all available commands
just start    # Start the game
just build    # Build the project
just clean    # Clean build artifacts
```

## Documentation

- [Developer Guide](docs/developer-guide.md) — For programmers: environment setup, architecture, and extending the game
- [Designer Guide](docs/designer-guide.md) — For game designers: YAML map config format and content editing
- [Project Structure](docs/project-structure.md) — Codebase architecture and module descriptions
- [Setup Guide](SETUP.md) — Step-by-step project setup process

## Tech Stack

| Category | Technology |
|----------|------------|
| **Language** | C# |
| **Runtime** | .NET 9 |
| **Game Framework** | MonoGame 3.8 (DesktopGL) |
| **Build Tool** | [just](https://github.com/casey/just) — command runner |
| **Version Manager** | [mise](https://mise.jdx.dev/) — manages dotnet, just, etc. |
| **Platforms** | macOS (ARM64 / x64), Windows (x64 / ARM64), Linux (x64) |

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| [MonoGame.Framework.DesktopGL](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL) | 3.8.* | Core game framework (cross-platform OpenGL) |
| [MonoGame.Content.Builder.Task](https://www.nuget.org/packages/MonoGame.Content.Builder.Task) | 3.8.* | Content pipeline build task |
| [MonoGame.Extended](https://www.nuget.org/packages/MonoGame.Extended) | 5.4.0 | Camera, SpriteBatch extensions, input handling |
| [Myra](https://www.nuget.org/packages/Myra) | 1.5.11 | UI framework (buttons, labels, panels) |
| [sqlite-net-pcl](https://www.nuget.org/packages/sqlite-net-pcl) | 1.9.* | SQLite ORM for player state persistence |
| [YamlDotNet](https://www.nuget.org/packages/YamlDotNet) | 16.3.0 | YAML deserialization for game configs |
| [Serilog](https://www.nuget.org/packages/Serilog) | 4.2.* | Structured logging core |
| [Serilog.Sinks.Console](https://www.nuget.org/packages/Serilog.Sinks.Console) | 6.0.* | Console log output |
| [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File) | 6.0.* | Daily rolling file log output |
