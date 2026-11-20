"""Nal Bumerangı mermisi: Mıknato'nun at nalı (kırmızı gövde, gümüş uçlar).

Düz yatan U (Blender XY düzlemi = Unity yer düzlemi), pivot merkezde; Unity'de Y ekseni etrafında döner.
Renkler CityPalette.png hücrelerinden (UV tek piksele), Mat_CityPalette ile çizilir.
Çalıştırma: blender -b --factory-startup --python build_horseshoe.py -- [--export]
Çıktı: Assets/_Project/Art/City/X_Horseshoe.fbx (Skill Setini Kur → FX_Nal).
"""
import math
import os
import sys

import bmesh
import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "X_Horseshoe.fbx"))

PALETTE_W, PALETTE_H = 64, 40
RED = (51.5, 11.5)     # (200, 68, 63)
SILVER = (3.5, 19.5)   # (183, 190, 199)

RADIUS = 0.2           # Unity birimi: küçük blob (çap 0.5) yanında okunur boy
WIDTH = 0.085          # kolun eni (radyal)
THICK = 0.06           # kalınlık
OPEN_DEG = 70          # U ağzı
SEGMENTS = 22
TIP_DEG = 28           # uçtan bu kadar derece gümüş


def uv_of(cell):
    return (cell[0] / PALETTE_W, 1.0 - cell[1] / PALETTE_H)


def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    mesh = bpy.data.meshes.new("X_Horseshoe")
    obj = bpy.data.objects.new("X_Horseshoe", mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    start = math.radians(90 + OPEN_DEG / 2)           # ağız +Y yönünde
    sweep = math.radians(360 - OPEN_DEG)
    rings = []
    for i in range(SEGMENTS + 1):
        a = start + sweep * i / SEGMENTS
        c, s = math.cos(a), math.sin(a)
        r0, r1 = RADIUS - WIDTH / 2, RADIUS + WIDTH / 2
        z0, z1 = -THICK / 2, THICK / 2
        rings.append([bm.verts.new((r * c, r * s, z)) for r, z in ((r0, z0), (r1, z0), (r1, z1), (r0, z1))])

    def paint(face, deg_from_end):
        cell = SILVER if deg_from_end < TIP_DEG else RED
        for loop in face.loops:
            loop[uv].uv = uv_of(cell)

    total_deg = 360 - OPEN_DEG
    for i in range(SEGMENTS):
        mid = (i + 0.5) / SEGMENTS * total_deg
        from_end = min(mid, total_deg - mid)
        a, b = rings[i], rings[i + 1]
        for k in range(4):
            f = bm.faces.new((a[k], a[(k + 1) % 4], b[(k + 1) % 4], b[k]))
            paint(f, from_end)
    paint(bm.faces.new(list(reversed(rings[0]))), 0)
    paint(bm.faces.new(rings[-1]), 0)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = False
    return obj


def export(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={"MESH"},
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP",
                             mesh_smooth_type="FACE", add_leaf_bones=False)
    print("Yazıldı:", OUT)


def main(do_export):
    obj = build()
    if do_export:
        export(obj)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    main("--export" in argv)
