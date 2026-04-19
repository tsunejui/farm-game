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

# Direction enum mapping: name -> model rotation around Z axis (degrees)
# Camera is fixed top-down; we rotate the MODEL so its front faces different
# on-screen directions. "down" means the character's front faces the bottom
# of the screen (so the viewer sees the front of the character).
# TripoSR output convention: front of model points toward +Z in model space,
# which after trimesh/GLB import shows front along -Y in Blender.
DIRECTIONS = {
    "down": 0,       # character front faces screen-down (viewer sees front)
    "down_right": 45,
    "right": 90,
    "up_right": 135,
    "up": 180,       # character front faces screen-up (viewer sees back)
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
    parser.add_argument("--resolution", type=int, default=1024, help="Render resolution (default: 1024)")
    parser.add_argument("--elevation", type=float, default=70.0,
                        help="Camera elevation in degrees (default: 70, near top-down for RPG view)")
    return parser.parse_args(argv)


def setup_scene():
    """Clear default scene and set up rendering environment."""
    import bpy

    # Delete all default objects
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()

    # Cycles gives better shading for vertex-colored meshes; low samples since we downscale
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    try:
        scene.cycles.device = "GPU"
    except Exception:
        pass
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    # Brighter ambient so flat-lit vertex colors stay readable
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[1].default_value = 1.0


def apply_vertex_color_material(objects):
    """Create per-object materials that sample each mesh's actual color attribute."""
    import bpy

    for obj in objects:
        if obj.type != "MESH":
            continue

        mesh = obj.data
        color_attrs = getattr(mesh, "color_attributes", None)
        color_name = color_attrs[0].name if color_attrs and len(color_attrs) > 0 else ""

        mat = bpy.data.materials.new(name=f"VC_{obj.name}")
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        for n in list(nodes):
            nodes.remove(n)

        output = nodes.new("ShaderNodeOutputMaterial")
        output.location = (400, 0)

        bsdf = nodes.new("ShaderNodeBsdfPrincipled")
        bsdf.location = (100, 0)
        bsdf.inputs["Roughness"].default_value = 0.9

        # Use Attribute node for maximum compatibility across PLY/GLB/OBJ
        attr = nodes.new("ShaderNodeAttribute")
        attr.location = (-200, 0)
        attr.attribute_name = color_name
        attr.attribute_type = "GEOMETRY"

        links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
        links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])

        print(f"  Material on {obj.name}: sampling color attribute '{color_name}'")

        mesh.materials.clear()
        mesh.materials.append(mat)


def import_model(model_path):
    """Import PLY/OBJ/GLB model."""
    import bpy

    ext = os.path.splitext(model_path)[1].lower()
    if ext == ".ply":
        bpy.ops.wm.ply_import(filepath=model_path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=model_path)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=model_path)
    else:
        print(f"Error: Unsupported model format: {ext}")
        sys.exit(1)

    imported = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not imported:
        print("Error: No mesh objects found in model")
        sys.exit(1)

    # TripoSR output convention: Y-up in model space, front along -Z.
    # Blender PLY importer loads as-is, so we rotate to stand the character upright
    # with front along -Y (toward default camera).
    for obj in imported:
        obj.rotation_euler = (math.radians(90), 0, math.radians(180))

    bpy.ops.object.select_all(action="DESELECT")
    for obj in imported:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = imported[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

    # Report color attributes to aid debugging
    for obj in imported:
        mesh = obj.data
        color_attrs = [a.name for a in getattr(mesh, "color_attributes", [])]
        print(f"  {obj.name}: color attributes = {color_attrs}")

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
    """Set up soft top-biased lighting for a top-down view."""
    import bpy

    # Key from above-front
    bpy.ops.object.light_add(type="SUN", location=(2, -2, 6))
    key = bpy.context.object
    key.data.energy = 4.0
    key.name = "KeyLight"

    # Fill from opposite
    bpy.ops.object.light_add(type="SUN", location=(-2, 2, 4))
    fill = bpy.context.object
    fill.data.energy = 2.0
    fill.name = "FillLight"


def setup_camera(resolution, elevation_deg, ortho_scale, distance=8.0):
    """Create a fixed orthographic camera at given elevation, looking down at origin."""
    import bpy
    import mathutils

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "RenderCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale

    el = math.radians(elevation_deg)
    y = -distance * math.cos(el)
    z = distance * math.sin(el)
    camera.location = (0, y, z)

    direction = mathutils.Vector((0, 0, 0)) - camera.location
    rot = direction.to_track_quat("-Z", "Y")
    camera.rotation_euler = rot.to_euler()

    bpy.context.scene.camera = camera
    bpy.context.scene.render.resolution_x = resolution
    bpy.context.scene.render.resolution_y = resolution
    bpy.context.scene.render.resolution_percentage = 100

    return camera


def compute_ortho_scale(objects, elevation_deg, padding=1.15):
    """Compute ortho scale so the model fits tightly at worst-case rotation."""
    import mathutils

    all_coords = []
    for obj in objects:
        for corner in obj.bound_box:
            all_coords.append(obj.matrix_world @ mathutils.Vector(corner))
    if not all_coords:
        return 2.5

    # Worst-case diameter in the XY plane (since we rotate around Z)
    xs = [c.x for c in all_coords]
    ys = [c.y for c in all_coords]
    zs = [c.z for c in all_coords]
    xy_diameter = math.hypot(max(xs) - min(xs), max(ys) - min(ys))
    z_extent = max(zs) - min(zs)

    el = math.radians(elevation_deg)
    # At elevation el, visible height in the image = xy_diameter*cos(el) + z_extent*sin(el)
    # ortho_scale defines the larger axis of the camera view
    height = xy_diameter * math.cos(el) + z_extent * math.sin(el)
    width = xy_diameter
    return max(width, height) * padding


def render_directions(objects, output_dir, name):
    """Render from all 8 directions by rotating the MODEL (camera stays fixed)."""
    import bpy

    os.makedirs(output_dir, exist_ok=True)

    for direction_name, rotation_z_deg in DIRECTIONS.items():
        rad = math.radians(rotation_z_deg)
        for obj in objects:
            obj.rotation_euler = (0, 0, rad)

        output_path = os.path.join(output_dir, f"{name}_{direction_name}.png")
        bpy.context.scene.render.filepath = output_path

        print(f"Rendering {direction_name} (model rot Z={rotation_z_deg}°)...")
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

    print("Applying vertex color material...")
    apply_vertex_color_material(objects)

    print("Setting up lighting...")
    setup_lighting()

    print("Computing camera framing...")
    ortho_scale = compute_ortho_scale(objects, args.elevation)
    print(f"  ortho_scale = {ortho_scale:.3f}")

    print("Setting up camera...")
    setup_camera(args.resolution, args.elevation, ortho_scale)

    print(f"Rendering 8 directions at {args.resolution}x{args.resolution}...")
    render_directions(objects, args.output_dir, args.name)

    print("Done! All directions rendered.")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        # Blender swallows non-zero exits on some errors; force exit code.
        import traceback
        traceback.print_exc()
        sys.exit(1)
