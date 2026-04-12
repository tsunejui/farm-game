#!/usr/bin/env python3
"""
blender_render.py — Blender headless script: render 8-direction screenshots of a 3D model

Usage (invoked via Blender):
    blender --background --python tools/sprite_pipeline/blender_render.py -- \
        --model build/sprite_pipeline/chicken/model.obj \
        --output-dir build/sprite_pipeline/chicken/renders/ \
        --name chicken \
        --resolution 512 \
        --elevation 30
"""

import sys
import argparse
import math
import os

# Direction enum mapping: name -> camera azimuth angle (degrees)
# Camera orbits the model; 0° = front (facing screen = "Down" direction)
DIRECTIONS = {
    "down": 0,
    "down_right": 45,
    "right": 90,
    "up_right": 135,
    "up": 180,
    "up_left": 225,
    "left": 270,
    "down_left": 315,
}


def parse_args():
    # Blender passes script args after '--'
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Render 3D model from 8 directions")
    parser.add_argument("--model", required=True, help="Input mesh path (.obj/.glb)")
    parser.add_argument("--output-dir", required=True, help="Output directory for renders")
    parser.add_argument("--name", required=True, help="Base name for output files")
    parser.add_argument("--resolution", type=int, default=512, help="Render resolution (default: 512)")
    parser.add_argument("--elevation", type=float, default=30.0,
                        help="Camera elevation angle in degrees (default: 30, 3/4 top-down view)")
    return parser.parse_args(argv)


def setup_scene():
    """Clear default scene and set up rendering environment."""
    import bpy

    # Delete all default objects
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()

    # Set render engine to Cycles for better quality (fallback to EEVEE if needed)
    bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    bpy.context.scene.render.film_transparent = True
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.image_settings.color_mode = "RGBA"


def import_model(model_path):
    """Import OBJ or GLB model."""
    import bpy

    ext = os.path.splitext(model_path)[1].lower()
    if ext == ".obj":
        bpy.ops.wm.obj_import(filepath=model_path)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=model_path)
    else:
        print(f"Error: Unsupported model format: {ext}")
        sys.exit(1)

    # Select all imported mesh objects
    imported = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not imported:
        print("Error: No mesh objects found in model")
        sys.exit(1)

    return imported


def center_and_scale(objects):
    """Center objects at origin and scale to fit in a unit sphere."""
    import bpy
    import mathutils

    # Compute combined bounding box
    all_coords = []
    for obj in objects:
        for corner in obj.bound_box:
            world_corner = obj.matrix_world @ mathutils.Vector(corner)
            all_coords.append(world_corner)

    if not all_coords:
        return

    min_co = mathutils.Vector((
        min(c.x for c in all_coords),
        min(c.y for c in all_coords),
        min(c.z for c in all_coords),
    ))
    max_co = mathutils.Vector((
        max(c.x for c in all_coords),
        max(c.y for c in all_coords),
        max(c.z for c in all_coords),
    ))

    center = (min_co + max_co) / 2
    size = max((max_co - min_co).x, (max_co - min_co).y, (max_co - min_co).z)

    if size == 0:
        size = 1.0

    scale_factor = 2.0 / size

    for obj in objects:
        obj.location -= center
        obj.scale *= scale_factor

    # Apply transforms
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def setup_lighting():
    """Set up 3-point lighting for consistent shading."""
    import bpy

    # Key light (main)
    bpy.ops.object.light_add(type="SUN", location=(3, -3, 5))
    key = bpy.context.object
    key.data.energy = 3.0
    key.name = "KeyLight"

    # Fill light (softer, opposite side)
    bpy.ops.object.light_add(type="SUN", location=(-3, 3, 3))
    fill = bpy.context.object
    fill.data.energy = 1.5
    fill.name = "FillLight"

    # Rim light (behind, for edge definition)
    bpy.ops.object.light_add(type="SUN", location=(0, 4, 4))
    rim = bpy.context.object
    rim.data.energy = 2.0
    rim.name = "RimLight"


def setup_camera(resolution, elevation_deg):
    """Create an orthographic camera."""
    import bpy

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "RenderCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.5  # Fit the normalized model

    bpy.context.scene.camera = camera
    bpy.context.scene.render.resolution_x = resolution
    bpy.context.scene.render.resolution_y = resolution
    bpy.context.scene.render.resolution_percentage = 100

    return camera


def position_camera(camera, azimuth_deg, elevation_deg, distance=5.0):
    """Position camera at given azimuth/elevation, looking at origin."""
    import mathutils

    az = math.radians(azimuth_deg)
    el = math.radians(elevation_deg)

    # Spherical to Cartesian
    x = distance * math.cos(el) * math.sin(az)
    y = distance * math.cos(el) * -math.cos(az)
    z = distance * math.sin(el)

    camera.location = (x, y, z)

    # Point camera at origin
    direction = mathutils.Vector((0, 0, 0)) - camera.location
    rot = direction.to_track_quat("-Z", "Y")
    camera.rotation_euler = rot.to_euler()


def render_directions(camera, output_dir, name, elevation_deg):
    """Render from all 8 directions."""
    import bpy

    os.makedirs(output_dir, exist_ok=True)

    for direction_name, azimuth in DIRECTIONS.items():
        position_camera(camera, azimuth, elevation_deg)

        output_path = os.path.join(output_dir, f"{name}_{direction_name}.png")
        bpy.context.scene.render.filepath = output_path

        print(f"Rendering {direction_name} (azimuth={azimuth}°)...")
        bpy.ops.render.render(write_still=True)
        print(f"  Saved: {output_path}")


def main():
    args = parse_args()

    print(f"Setting up scene...")
    setup_scene()

    print(f"Importing model: {args.model}")
    objects = import_model(args.model)

    print("Centering and scaling model...")
    center_and_scale(objects)

    print("Setting up lighting...")
    setup_lighting()

    print("Setting up camera...")
    camera = setup_camera(args.resolution, args.elevation)

    print(f"Rendering 8 directions at {args.resolution}x{args.resolution}...")
    render_directions(camera, args.output_dir, args.name, args.elevation)

    print("Done! All directions rendered.")


if __name__ == "__main__":
    main()
