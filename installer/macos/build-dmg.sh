#!/bin/sh
# =============================================================================
# build-dmg.sh — Package FarmGame .app bundle into a .dmg
#
# Usage:
#   ./installer/macos/build-dmg.sh <arch> <version>
#   arch: arm64 or x64
#
# Prerequisites:
#   - Run `just release-osx-<arch>` before this script
#   - macOS with hdiutil (built-in)
# =============================================================================

set -e

ARCH="$1"
VERSION="$2"
APP_NAME="Farm Game"
BUNDLE_NAME="FarmGame.app"
EXECUTABLE="FarmGame"
IDENTIFIER="com.farmgame.app"

SOURCE_DIR="dist/osx-${ARCH}"
STAGING_DIR="dist/dmg-staging-${ARCH}"
OUTPUT_DIR="dist/installer"
DMG_NAME="FarmGame_${VERSION}_${ARCH}.dmg"

if [ -z "$ARCH" ] || [ -z "$VERSION" ]; then
    echo "Usage: $0 <arch> <version>"
    echo "  arch: arm64 or x64"
    exit 1
fi

if [ ! -f "${SOURCE_DIR}/${EXECUTABLE}" ]; then
    echo "Error: ${SOURCE_DIR}/${EXECUTABLE} not found. Run 'just release-osx-${ARCH}' first."
    exit 1
fi

echo "Building ${DMG_NAME}..."

# Clean staging
rm -rf "${STAGING_DIR}"
mkdir -p "${STAGING_DIR}/${BUNDLE_NAME}/Contents/MacOS"
mkdir -p "${STAGING_DIR}/${BUNDLE_NAME}/Contents/Resources"
mkdir -p "${OUTPUT_DIR}"

# Copy executable and content
cp -R "${SOURCE_DIR}/"* "${STAGING_DIR}/${BUNDLE_NAME}/Contents/MacOS/"

# Create Info.plist
cat > "${STAGING_DIR}/${BUNDLE_NAME}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>${IDENTIFIER}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleExecutable</key>
    <string>${EXECUTABLE}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

# Make executable
chmod +x "${STAGING_DIR}/${BUNDLE_NAME}/Contents/MacOS/${EXECUTABLE}"

# Create DMG
rm -f "${OUTPUT_DIR}/${DMG_NAME}"
hdiutil create \
    -volname "${APP_NAME}" \
    -srcfolder "${STAGING_DIR}" \
    -ov \
    -format UDZO \
    "${OUTPUT_DIR}/${DMG_NAME}"

# Clean staging
rm -rf "${STAGING_DIR}"

echo "Created: ${OUTPUT_DIR}/${DMG_NAME}"
