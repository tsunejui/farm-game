tiled := "/Applications/Tiled.app/Contents/MacOS/tiled"

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
