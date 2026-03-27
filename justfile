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
