"""X_FerrisWheelRoll: dönme dolap dönüşümünün çarkı (ayaksız). zorse-blender deposuna taşınacak.

Kaynak: Assets/_Project/Art/City/Kit2/P_FerrisWheel.fbx (build_props2.py'nin çıktısı). Ayrık parçalardan en alt
noktası 1.3'ün altında olanlar (ayaklar, taban, gişe) atılır; kalan çark + kabinler + göbek merkezde (0,0,0) olacak
şekilde taşınır. Aks Blender Y (değişmez), palet CityPaletteV2. Unity: Tools → Blob.io → Dönme Dolap Formunu Kur.

Çalıştırma: blender -b --factory-startup --python build_ferris_roll.py -- [--export]
"""
import os
import sys

import bmesh
import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "_Project", "Art", "City", "Kit2", "P_FerrisWheel.fbx")
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "City", "X_FerrisWheelRoll.fbx")
STAND_TOP = 1.3


def build():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SRC)
    src = [o for o in bpy.data.objects if o not in before][0]

    bm = bmesh.new()
    bm.from_mesh(src.data)
    bm.transform(src.matrix_world)
    bm.verts.ensure_lookup_table()

    # Ayrık parçalar: en altı STAND_TOP'un altındaysa ayak/taban/gişe.
    seen, drop = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, comp = [v], []
        while stack:
            x = stack.pop()
            if x.index in seen:
                continue
            seen.add(x.index)
            comp.append(x)
            stack.extend(e.other_vert(x) for e in x.link_edges)
        if min(c.co.z for c in comp) < STAND_TOP:
            drop.extend(comp)
    bmesh.ops.delete(bm, geom=drop, context="VERTS")

    zs = [v.co.z for v in bm.verts]
    xs = [v.co.x for v in bm.verts]
    center = ((min(xs) + max(xs)) * 0.5, 0.0, (min(zs) + max(zs)) * 0.5)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(-center[0], -center[1], -center[2]))

    me = bpy.data.meshes.new("X_FerrisWheelRoll")
    bm.to_mesh(me)
    bm.free()
    for m in src.data.materials:
        me.materials.append(m)
    obj = bpy.data.objects.new("X_FerrisWheelRoll", me)
    bpy.context.scene.collection.objects.link(obj)
    bpy.data.objects.remove(src, do_unlink=True)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    print("X_FerrisWheelRoll üçgen", tris, "boyut", tuple(round(d, 2) for d in obj.dimensions))
    return obj


def export(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={"MESH"},
                             apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", path_mode="STRIP")
    print("yazıldı", OUT)


def main(do_export):
    old = bpy.data.objects.get("X_FerrisWheelRoll")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    obj = build()
    if do_export:
        export(obj)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    main("--export" in argv)
