set shell := ['/bin/sh', '-cu']

# Default invocation prints the command list
default:
	@just --list

# Start the game
start:
	dotnet run --project FarmGame/FarmGame.csproj

# Build the project
build:
	dotnet build FarmGame.sln

# Clean build artifacts
clean:
	dotnet clean FarmGame.sln

publish_flags := "-c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"

# Publish for all platforms (macOS, Windows, Linux)
release: release-osx-arm64 release-osx-x64 release-win-x64 release-linux-x64

# Publish for macOS (Apple Silicon)
release-osx-arm64:
	dotnet publish FarmGame/FarmGame.csproj {{publish_flags}} -r osx-arm64 -o dist/osx-arm64/

# Publish for macOS (Intel)
release-osx-x64:
	dotnet publish FarmGame/FarmGame.csproj {{publish_flags}} -r osx-x64 -o dist/osx-x64/

# Publish for Windows (x64)
release-win-x64:
	dotnet publish FarmGame/FarmGame.csproj {{publish_flags}} -r win-x64 -o dist/win-x64/

# Publish for Linux (x64)
release-linux-x64:
	dotnet publish FarmGame/FarmGame.csproj {{publish_flags}} -r linux-x64 -o dist/linux-x64/
