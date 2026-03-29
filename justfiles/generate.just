# Generate all assets (fonts + images)
generate: font-generate image-generate

# Copy downloaded fonts to game Content directory
font-generate:
	./scripts/gen-font.sh

# Generate all pixel art images from YAML definitions
image-generate:
	./scripts/gen-image.sh

# Clean generated images
image-clean:
	rm -rf game/FarmGame/Content/Images
	echo "Cleaned generated images"
