# Generate all pixel art images from YAML definitions
image-generate:
	./scripts/gen-image.sh

# Clean generated images
image-clean:
	rm -rf game/FarmGame/Content/Images
	echo "Cleaned generated images"
