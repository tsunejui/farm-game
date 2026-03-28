# Installer

Platform-specific installer configurations for FarmGame.

## Windows

Uses [Inno Setup](https://jrsoftware.org/isinfo.php) via [amake/innosetup](https://hub.docker.com/r/amake/innosetup) Docker image to create `.exe` installers. No Windows machine required.

### Architectures

| Architecture | Setup File | Installer Output |
|---|---|---|
| x64 | `setup-x64.iss` | `FarmGame_Setup_<version>_x64.exe` |
| ARM64 | `setup-arm64.iss` | `FarmGame_Setup_<version>_arm64.exe` |

### Prerequisites

- Docker

### Build Installers

```bash
just installer-win          # Build both x64 and ARM64 installers
just installer-win-x64      # Build x64 only
just installer-win-arm64    # Build ARM64 only
```

Output: `dist/installer/`

### Version

`just installer-win` automatically reads the `VERSION` file and passes it to Inno Setup via `/DAppVersion`. No manual editing needed.

### Features

- Custom install directory (defaults to `%LOCALAPPDATA%\Programs\Farm Game`)
- Optional desktop shortcut
- Start menu entries
- Uninstaller
- No admin rights required (`PrivilegesRequired=lowest`)

## macOS

Uses `hdiutil` (built-in on macOS) to create `.dmg` disk images containing a `.app` bundle.

### Architectures

| Architecture | Installer Output |
|---|---|
| ARM64 (Apple Silicon) | `FarmGame_<version>_arm64.dmg` |
| x64 (Intel) | `FarmGame_<version>_x64.dmg` |

### Build Installers

```bash
just installer-mac          # Build both ARM64 and x64 DMGs
just installer-mac-arm64    # Build ARM64 only
just installer-mac-x64      # Build x64 only
```

Output: `dist/installer/`

### Features

- Proper `.app` bundle with `Info.plist`
- UDZO compressed DMG
- Version auto-read from `VERSION` file

## Linux

TODO: `.AppImage` or `.deb` packaging
