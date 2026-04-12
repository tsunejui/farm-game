# Generate 8-direction pixel sprites from a reference image via AI pipeline
# Usage: just sprite-generate <name> <input_image> [category] [size] [colors]
sprite-generate name input category="items" size="64" colors="32":
    python3 tools/sprite_pipeline/run_pipeline.py \
        --input {{input}} \
        --name {{name}} \
        --category {{category}} \
        --size {{size}} \
        --colors {{colors}}

# Re-run only the pixelation + YAML export steps (skip AI + Blender)
# Usage: just sprite-pixelate <name> [size] [colors]
sprite-pixelate name size="64" colors="32":
    python3 tools/sprite_pipeline/pixelate.py \
        --input-dir build/sprite_pipeline/{{name}}/renders \
        --output-dir build/sprite_pipeline/{{name}}/pixelated \
        --size {{size}} --colors {{colors}}
    python3 tools/sprite_pipeline/yaml_export.py \
        --input-dir build/sprite_pipeline/{{name}}/pixelated \
        --output-dir assets/images/ \
        --name {{name}} \
        --category {{name}}

# Install sprite pipeline Python dependencies and sync TripoSR via vendir
sprite-setup:
    vendir sync
    pip3 install -r tools/requirements-sprite.txt
    CMAKE_POLICY_VERSION_MINIMUM=3.5 pip3 install -r vendor/TripoSR/requirements.txt

# Clean sprite pipeline build artifacts
sprite-clean:
    rm -rf build/sprite_pipeline/
    echo "Cleaned sprite pipeline build artifacts"
