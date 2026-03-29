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

# Full release: bump version, build, package, commit, tag, publish
release: release-version build-all installer release-zip release-tag release-publish

# Bump patch version and confirm via interactive prompt
release-version:
	#!/bin/sh
	current=$(cat VERSION)
	major=$(echo "$current" | cut -d. -f1)
	minor=$(echo "$current" | cut -d. -f2)
	patch=$(echo "$current" | cut -d. -f3)
	next_patch=$((patch + 1))
	suggested="${major}.${minor}.${next_patch}"
	echo "Current version: $current"
	echo "Suggested next:  $suggested"
	version=$(gum input --placeholder "$suggested" --value "$suggested" --header "Enter release version:")
	if [ -z "$version" ]; then \
		echo "Cancelled."; \
		exit 1; \
	fi
	echo "$version" > VERSION
	echo "Version set to: $version"

# Package each platform installer into separate zip files
release-zip:
	#!/bin/sh
	version="$(cat VERSION)"
	mkdir -p dist/release
	# Windows x64 installer
	if [ -f "dist/installer/FarmGame_Setup_${version}_x64.exe" ]; then \
		cd dist/installer && zip -j "../release/FarmGame_${version}_windows-x64.zip" "FarmGame_Setup_${version}_x64.exe" && cd ../..; \
	fi
	# Windows ARM64 installer
	if [ -f "dist/installer/FarmGame_Setup_${version}_arm64.exe" ]; then \
		cd dist/installer && zip -j "../release/FarmGame_${version}_windows-arm64.zip" "FarmGame_Setup_${version}_arm64.exe" && cd ../..; \
	fi
	# macOS ARM64 DMG
	if [ -f "dist/installer/FarmGame_${version}_arm64.dmg" ]; then \
		cd dist/installer && zip -j "../release/FarmGame_${version}_macos-arm64.zip" "FarmGame_${version}_arm64.dmg" && cd ../..; \
	fi
	# macOS x64 DMG
	if [ -f "dist/installer/FarmGame_${version}_x64.dmg" ]; then \
		cd dist/installer && zip -j "../release/FarmGame_${version}_macos-x64.zip" "FarmGame_${version}_x64.dmg" && cd ../..; \
	fi
	# Linux x64
	if [ -d "dist/linux-x64" ]; then \
		cd dist/linux-x64 && zip -r "../release/FarmGame_${version}_linux-x64.zip" . -x ".*" && cd ../..; \
	fi
	echo "Release artifacts:"
	ls -lh dist/release/FarmGame_${version}_*.zip 2>/dev/null

# Commit VERSION and create git tag
release-tag:
	#!/bin/sh
	version="$(cat VERSION)"
	tag="v${version}"
	git add VERSION
	git commit -m "release: ${tag}"
	git tag -a "${tag}" -m "Release ${tag}"
	git push
	git push origin "${tag}"
	echo "Committed and tagged: ${tag}"

# Create GitHub release and upload all platform zips
release-publish:
	#!/bin/sh
	version="$(cat VERSION)"
	tag="v${version}"
	gh release create "${tag}" \
		--title "${tag}" \
		--generate-notes \
		dist/release/FarmGame_${version}_*.zip
	echo "Published release ${tag}"
