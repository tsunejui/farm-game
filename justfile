set shell := ['/bin/sh', '-cu']

tiled := "/Applications/Tiled.app/Contents/MacOS/tiled"

# Default invocation prints the command list
default:
	@just --list

# Start the game
start: env
	dotnet run --no-restore --project game/FarmGame/FarmGame.csproj

# Generate local environment config (.env.local)
env:
	#!/bin/sh
	case "$(uname)" in \
		Darwin) db_dir="$HOME/Library/Application Support/Farm_Game" ;; \
		Linux) db_dir="${XDG_DATA_HOME:-$HOME/.local/share}/Farm_Game" ;; \
		MINGW*|MSYS*|CYGWIN*) db_dir="$LOCALAPPDATA/Farm_Game" ;; \
		*) db_dir="$HOME/.local/share/Farm_Game" ;; \
	esac; \
	cat > .env.local <<EOF
	# Auto-generated local environment config — do not commit
	# Generated at: $(date -u '+%Y-%m-%dT%H:%M:%SZ')
	OS=$(uname)
	GAME_TITLE=Farm Game
	DB_PATH=$db_dir/game.db
	EOF
	echo "Generated .env.local"

# Build the project
build:
	dotnet build FarmGame.sln

# Clean build artifacts
clean:
	dotnet clean FarmGame.sln
	rm -rf build/

# Interactive map pipeline menu
map:
	@INTERACTIVE_HEADER="Map Pipeline" ./scripts/interactive.sh \
		"Build All (YAML → TMX → JSON)::just map-build" \
		"Generate TMX (YAML → TMX)::just map-generate" \
		"Export JSON (TMX → JSON via Tiled)::just map-export" \
		"Clean map build cache::just map-clean" \
		"Build + Run Game::just start-full"

# Full map pipeline: YAML → TMX → JSON
map-build: map-generate map-export

# Generate TMX from all YAML map sources
map-generate:
	pip3 install -q -r tools/requirements.txt
	mkdir -p build/maps
	for f in game/FarmGame/Content/Maps/*.yaml; do \
		python3 tools/yaml_to_tmx.py "$f" \
			--terrains-dir game/FarmGame/Content/Terrains \
			--items-dir game/FarmGame/Content/Items \
			--output build/maps/; \
	done

# Validate and export all TMX maps to JSON via Tiled CLI
map-export:
	for f in build/maps/*.tmx; do \
		name=$(basename "$f" .tmx); \
		{{tiled}} --export-map --embed-tilesets --resolve-types-and-properties \
			"$f" "game/FarmGame/Content/Maps/$name.json"; \
		echo "Exported: game/FarmGame/Content/Maps/$name.json"; \
	done

# Clean intermediate map build files
map-clean:
	rm -rf build/maps
	rm -f game/FarmGame/Content/Maps/*.json
	echo "Cleaned map build cache"

# Build and run with map pipeline
start-full: map-build build start

publish_flags := "-c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"

# Publish for all platforms (macOS, Windows, Linux)
release: release-osx-arm64 release-osx-x64 release-win-x64 release-linux-x64

# Publish for macOS (Apple Silicon)
release-osx-arm64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r osx-arm64 -o dist/osx-arm64/

# Publish for macOS (Intel)
release-osx-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r osx-x64 -o dist/osx-x64/

# Publish for Windows (x64)
release-win-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r win-x64 -o dist/win-x64/

# Publish for Linux (x64)
release-linux-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r linux-x64 -o dist/linux-x64/
