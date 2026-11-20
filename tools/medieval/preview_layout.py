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

# Zemin: dikdörtgenler, yol şeritleri (Unity'deki şerit mesh'in aynısı), daireler. Renkli düz materyal.
GROUND_COL = {"Grass": (0.32, 0.55, 0.22), "Dirt": (0.62, 0.45, 0.28), "Cobble": (0.55, 0.55, 0.58),
              "Flagstone": (0.45, 0.45, 0.50), "Water": (0.20, 0.50, 0.85)}


def gmat(tex):
    m = bpy.data.materials.get("G_" + tex) or bpy.data.materials.new("G_" + tex)
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs[0].default_value = (*GROUND_COL.get(tex, (1, 0, 1)), 1)
    bsdf.inputs["Roughness"].default_value = 1.0
    return m


def gobj(name, verts, faces, tex, y):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(v[0], v[1], y) for v in verts], [], faces)
    me.materials.append(gmat(tex))
    o = bpy.data.objects.new(name, me)
    sc.collection.objects.link(o)


meta = lay["meta"]
k = 0
for r in meta.get("ground", []):
    k += 1
    gobj("Ground_" + r["tex"], [(r["x0"], r["z0"]), (r["x1"], r["z0"]), (r["x1"], r["z1"]), (r["x0"], r["z1"])], [(0, 1, 2, 3)], r["tex"], k * 0.003)
for p in meta.get("paths", []):
    k += 1
    f = p["pts"]
    pts = [(f[i], f[i + 1]) for i in range(0, len(f), 2)]
    n = len(pts)
    vs = []
    for i, (x, z) in enumerate(pts):
        a_, b_ = pts[(i - 1) % n if p["loop"] else max(i - 1, 0)], pts[(i + 1) % n if p["loop"] else min(i + 1, n - 1)]
        dx, dz = b_[0] - a_[0], b_[1] - a_[1]
        L = math.hypot(dx, dz) or 1.0
        nx, nz = -dz / L * p["hw"], dx / L * p["hw"]
        vs += [(x + nx, z + nz), (x - nx, z - nz)]
    segs = n if p["loop"] else n - 1
    faces = [(2 * i, 2 * ((i + 1) % n), 2 * ((i + 1) % n) + 1, 2 * i + 1) for i in range(segs)]
    gobj("Path_" + p["tex"], vs, faces, p["tex"], k * 0.003)
for d in meta.get("discs", []):
    k += 1
    n = 40
    vs = [(d["x"], d["z"])] + [(d["x"] + d["r"] * math.cos(i * 2 * math.pi / n), d["z"] + d["r"] * math.sin(i * 2 * math.pi / n)) for i in range(n)]
    gobj("Disc_" + d["tex"], vs, [(0, 1 + i, 1 + (i + 1) % n) for i in range(n)], d["tex"], k * 0.003)
print(len(sc.objects), "obje, eksik:", missing)
