"""Şehir kitine "Soft" görünüm (Bahar, 2026-11-20): mevcut FBX'leri açar, tek kademe kenar yuvarlatma + yumuşak normaller +
köşe karartması (AO → "Col" köşe rengi; Unity'de BlobSurvivor/PaletteAO shader'ı çarpar) uygular, AYNI yola geri yazar.
Kaynak script'ler zorse-blender'da (build_kit2.py vb.): onlar tekrar çalışırsa bu işlem yeniden yapılmalı.

Çalıştırma: blender -b --factory-startup --python soften_city.py -- [--only B_Bakery,...] [--check]
--check: sadece ilk dosyayı işler ve önce/sonra boyutlarını yazar (ölçek/yön kayması kontrolü), dosyaya yazmaz.
"""
import glob
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "medieval"))
import build_medieval as core  # bake_ao

ART = os.path.join(ROOT, "Assets", "_Project", "Art", "City")
FOLDERS = [ART, os.path.join(ART, "Kit2"), os.path.join(ART, "People")]
# önek → kenar yuvarlatma genişliği (oyun birimi; şehir kiti oyun boyunda)
WIDTH = {"B_": 0.04, "W_": 0.04, "P_": 0.02, "V_": 0.025, "C_": 0.015, "H_": 0.012}


def files(only):
    out = []
    for folder in FOLDERS:
        for path in sorted(glob.glob(os.path.join(folder, "*.fbx"))):
            name = os.path.splitext(os.path.basename(path))[0]
            if name[:2] not in WIDTH:
                continue
            if only and name not in only:
                continue
            out.append(path)
    return out


def clear():
    bpy.ops.wm.read_homefile(use_empty=True)


def soften_file(path, write):
    clear()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        return None
    if any(o.data.color_attributes.get("Col") is not None for o in meshes):
        return "zaten soft"  # tekrar çalıştırmak güvenli: yuvarlatma iki kez uygulanmaz
    before = [tuple(round(v, 3) for v in o.dimensions) for o in meshes]
    width = WIDTH[os.path.basename(path)[:2]]
    for o in meshes:
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        # Uygulanmış ölçek modifier'ı bozmasın: genişlik nesne ölçeğine bölünür
        s = max(1e-4, max(abs(o.scale.x), abs(o.scale.y), abs(o.scale.z)))
        m = o.modifiers.new("Bevel", "BEVEL")
        m.width = width / s
        m.segments = 1
        m.limit_method = "ANGLE"
        m.angle_limit = 0.9
        w = o.modifiers.new("WN", "WEIGHTED_NORMAL")
        w.keep_sharp = True
        for mod in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        for p in o.data.polygons:
            p.use_smooth = True
        core.bake_ao(o, meshes)
    after = [tuple(round(v, 3) for v in o.dimensions) for o in meshes]
    if write:
        bpy.ops.object.select_all(action="DESELECT")
        for o in bpy.context.scene.objects:
            if o.type in ("MESH", "EMPTY") and o.name != "AO_Ground":
                o.select_set(True)
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
                                 apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", path_mode="STRIP")
    return before, after


def main(argv):
    only = set(argv[argv.index("--only") + 1].split(",")) if "--only" in argv else None
    check = "--check" in argv
    todo = files(only)
    if check:
        todo = todo[:1] if not only else todo
    for path in todo:
        r = soften_file(path, write=not check)
        print("SOFT", os.path.basename(path), r)
    if check:
        # dışa aktar → tekrar içe al: boyut ve dönüş aynı mı?
        path = todo[0]
        tmp = os.path.join(os.environ.get("TEMP", "/tmp"), "soft_check.fbx")
        clear()
        bpy.ops.import_scene.fbx(filepath=path)
        orig = {o.name: (tuple(round(v, 3) for v in o.dimensions), tuple(round(v, 3) for v in o.rotation_euler),
                         tuple(round(v, 3) for v in o.location)) for o in bpy.context.scene.objects}
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(filepath=tmp, use_selection=True, object_types={"MESH", "EMPTY"},
                                 apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", path_mode="STRIP")
        clear()
        bpy.ops.import_scene.fbx(filepath=tmp)
        again = {o.name: (tuple(round(v, 3) for v in o.dimensions), tuple(round(v, 3) for v in o.rotation_euler),
                          tuple(round(v, 3) for v in o.location)) for o in bpy.context.scene.objects}
        print("CHECK orig ", orig)
        print("CHECK again", again)


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
