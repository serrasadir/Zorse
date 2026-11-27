"""Build Crownreach.blend and four editable multi-mesh FBX models from extract_atlas.cjs output."""
import collections
import json
import math
from pathlib import Path
import struct
import sys
import zlib
import bpy
from mathutils import Matrix, Vector

project = Path(__file__).resolve().parents[2]
art = project / "Assets/_Project/Art/Crownreach"
art.mkdir(parents=True, exist_ok=True)
data = json.loads(Path(sys.argv[sys.argv.index("--") + 1]).read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
regions = ["01_Farmland", "02_WalledTown", "03_Garrison", "04_RoyalCastle"]
collections_by_region = []
for name in regions:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    collections_by_region.append(collection)

# A point-sampled palette keeps every placed mesh on one material.
colors = sorted({shape["color"] for shape in data["shapes"]})
grid = 2 ** math.ceil(math.log2(math.ceil(math.sqrt(len(colors)))))
cells = {color: ((i % grid + .5) / grid, 1 - (i // grid + .5) / grid) for i, color in enumerate(colors)}
rows = []
cell = 8
for y in range(grid):
    row = b"\0" + b"".join(bytes.fromhex(colors[min(y * grid + x, len(colors)-1)]) * cell for x in range(grid))
    rows.extend([row] * cell)
def chunk(key, contents):
    return struct.pack(">I", len(contents)) + key + contents + struct.pack(">I", zlib.crc32(key + contents))
png = art / "AtlasPalette.png"
png.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", grid*cell,grid*cell,8,2,0,0,0)) + chunk(b"IDAT", zlib.compress(b"".join(rows))) + chunk(b"IEND", b""))
palette = bpy.data.images.load(str(png))
palette.pack()
mat = bpy.data.materials.new("CrownreachPalette")
mat.use_nodes = True
bsdf = mat.node_tree.nodes.get("Principled BSDF")
bsdf.inputs["Roughness"].default_value = 1
texture = mat.node_tree.nodes.new("ShaderNodeTexImage")
texture.image = palette
texture.interpolation = "Closest"
mat.node_tree.links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])

def district(z):
    return 0 if z > 19 else 1 if z > -8 else 2 if z > -32 else 3

groups = collections.defaultdict(list)
for shape in data["shapes"]:
    groups[shape["name"]].append(shape)
for name, shapes in groups.items():
    vertices, faces, uv = [], [], []
    for shape in shapes:
        m = Matrix([shape["matrix"][i:i+4] for i in range(0, 16, 4)]).transposed()
        geom = data["geometry"][shape["type"]]
        raw = geom["vertices"]
        index_map, unique = [], {}
        for i in range(0,len(raw),3):
            key = tuple(round(v,6) for v in raw[i:i+3])
            if key not in unique:
                x,y,z = m @ Vector(key)
                unique[key] = len(vertices)
                vertices.append(Vector((x,-z,y)))
            index_map.append(unique[key])
        indices = geom["indices"]
        for i in range(0,len(indices),3):
            face = tuple(index_map[v] for v in indices[i:i+3])
            if len(set(face)) == 3:
                faces.append(face)
                uv.extend(cells[shape["color"]] * 3)
    lo = Vector(min(v[i] for v in vertices) for i in range(3))
    hi = Vector(max(v[i] for v in vertices) for i in range(3))
    pivot = (lo + hi) * .5
    if not name.startswith("MillBlades"):
        pivot.z = lo.z
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([v-pivot for v in vertices], [], faces)
    mesh.update()
    layer = mesh.uv_layers.new(name="PaletteUV")
    layer.data.foreach_set("uv", uv)
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    collections_by_region[district(-pivot.y)].objects.link(obj)
    obj.location = pivot

scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        space = area.spaces.active
        space.region_3d.view_distance = 118
        space.region_3d.view_rotation = Vector((-40,80,-79)).to_track_quat('-Z','Y')
        space.region_3d.view_perspective = 'ORTHO'
        space.shading.color_type = 'TEXTURE'
        space.shading.show_shadows = True
        space.overlay.show_overlays = False
bpy.context.preferences.filepaths.save_version = 0
blend = project / "blender/Crownreach.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
for collection in collections_by_region:
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(art / (collection.name + ".fbx")), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        bake_anim=False, add_leaf_bones=False, path_mode='STRIP')
print("CROWNREACH_EXPORT_OK", len(groups), "meshes", [len(c.objects) for c in collections_by_region])
