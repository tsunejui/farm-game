publish_flags := "-c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"
innosetup_image := "amake/innosetup:innosetup6"

# --- Build executables ---

# Build all platform executables
build-all: build-osx-arm64 build-osx-x64 build-win-x64 build-win-arm64 build-linux-x64

# Build for macOS (Apple Silicon)
build-osx-arm64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r osx-arm64 -o dist/osx-arm64/

# Build for macOS (Intel)
build-osx-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r osx-x64 -o dist/osx-x64/

# Build for Windows (x64)
build-win-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r win-x64 -o dist/win-x64/

# Build for Windows (ARM64)
build-win-arm64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r win-arm64 -o dist/win-arm64/

# Build for Linux (x64)
build-linux-x64:
	dotnet publish game/FarmGame/FarmGame.csproj {{publish_flags}} -r linux-x64 -o dist/linux-x64/

# --- Build installers ---

# Build all installers (Windows + macOS)
installer: installer-win installer-mac

# Build all Windows installers
installer-win: installer-win-x64 installer-win-arm64

# Build Windows x64 installer via Docker
installer-win-x64: build-win-x64
	mkdir -p dist/installer
	docker run --rm \
		-v "$(pwd)/installer/windows/setup-x64.iss:/work/setup.iss:ro" \
		-v "$(pwd)/dist/win-x64:/work/source:ro" \
		-v "$(pwd)/dist/installer:/work/output" \
		{{innosetup_image}} /DAppVersion="$(cat VERSION)" setup.iss

# Build Windows ARM64 installer via Docker
installer-win-arm64: build-win-arm64
	mkdir -p dist/installer
	docker run --rm \
		-v "$(pwd)/installer/windows/setup-arm64.iss:/work/setup.iss:ro" \
		-v "$(pwd)/dist/win-arm64:/work/source:ro" \
		-v "$(pwd)/dist/installer:/work/output" \
		{{innosetup_image}} /DAppVersion="$(cat VERSION)" setup.iss

# Build all macOS DMG installers
installer-mac: installer-mac-arm64 installer-mac-x64

# Build macOS ARM64 DMG
installer-mac-arm64: build-osx-arm64
	./installer/macos/build-dmg.sh arm64 "$(cat VERSION)"

# Build macOS x64 DMG
installer-mac-x64: build-osx-x64
	./installer/macos/build-dmg.sh x64 "$(cat VERSION)"

# --- Release ---

# Build everything, package, and create GitHub release
release: build-all installer release-zip release-publish

# Package all installers into a zip for release
release-zip:
	#!/bin/sh
	version="$(cat VERSION)"
	mkdir -p dist/release
	cd dist/installer
	zip -r "../release/FarmGame_${version}_installers.zip" . -x ".*"
	cd ../..
	cd dist/linux-x64
	zip -r "../release/FarmGame_${version}_linux-x64.zip" . -x ".*"
	echo "Release artifacts in dist/release/"

# Create GitHub release and upload artifacts
release-publish:
	#!/bin/sh
	version="$(cat VERSION)"
	tag="v${version}"
	gh release create "${tag}" \
		--title "${tag}" \
		--generate-notes \
		dist/release/FarmGame_${version}_installers.zip \
		dist/release/FarmGame_${version}_linux-x64.zip
	echo "Published release ${tag}"
