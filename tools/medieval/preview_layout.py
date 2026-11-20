"""Açık Blender'da (MCP) castle_layout.json'u 'CastleLayout' sahnesine kurar (Unity'ye almadan önce göz kontrolü).
exec(open(...).read()) ile çalıştırılır. Unity (x, z) = Blender (x, y); yaw Unity'de üstten saat yönü."""
import importlib
import json
import math
import sys

import bpy
import mathutils

KIT = r"C:/Users/nurba/desktop/zorse/tools/medieval"
if KIT not in sys.path:
    sys.path.insert(0, KIT)
import build_medieval as core
core = importlib.reload(core)

builders = core.all_builders(reload=True)
mat = bpy.data.materials.get("MedievalPalette") or core.material(core.write_palette())
sc = bpy.data.scenes.get("CastleLayout") or bpy.data.scenes.new("CastleLayout")
bpy.context.window.scene = sc
sc.view_settings.view_transform = "Standard"
for o in list(sc.objects):
    bpy.data.objects.remove(o, do_unlink=True)

meshes = {}
for key, fn in builders.items():
    o = fn()
    name = core.export_name(core.BUILDER_MODULE[key], o.name.split(".")[0])
    o.data.materials.append(mat)
    o.data.transform(mathutils.Matrix.Scale(core.GAME_SCALE, 4))
    meshes[name] = o.data
    bpy.data.objects.remove(o, do_unlink=True)

lay = json.load(open(KIT + "/../../Assets/_Project/Art/City/Medieval/castle_layout.json", encoding="utf-8"))
missing = set()
for it in lay["items"]:
    m = meshes.get(it["mesh"])
    if m is None:
        missing.add(it["mesh"])
        continue
    o = bpy.data.objects.new(it["mesh"], m)
    sc.collection.objects.link(o)
    o.location = (it["x"], it["z"], it["y"])
    o.rotation_euler = (0, 0, -math.radians(it["yaw"]))
    o.scale = (it["s"],) * 3
print(len(sc.objects), "obje, eksik:", missing)
