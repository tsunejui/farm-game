set shell := ['/bin/sh', '-cu']

import 'justfiles/db.just'
import 'justfiles/download.just'
import 'justfiles/env.just'
import 'justfiles/generate.just'
import 'justfiles/map.just'
import 'justfiles/release.just'

# Default invocation prints the command list
default:
	@just --list

# Start the game
start: env
	dotnet run --no-restore --project game/FarmGame/FarmGame.csproj

# Build the project
build:
	dotnet build FarmGame.sln

# Clean build artifacts
clean:
	dotnet clean FarmGame.sln
	rm -rf build/

# Build and run with map pipeline
start-full: map-build build start
