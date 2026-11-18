"""Çevik kuvvet (H_RiotPolice + H_RiotShield) ve Motorlu polis (H_MotorPolice) modelleri.

H_RiotShield: kalkan + onu tutan sol kol ayrı parça, pivot sol omuzda (obje konumu). Unity'de
EnemyWalkSetup bunu gövdeye omuzdan bağlar; sersemleyince kalkan iner (ShieldLower).

zorse-blender deposuna taşınacak (bu makinede depo yok). H_Police ile aynı stil ve palet: CityPalette.png
(8 sütun x 5 satır). Her parça düz renk: tüm UV'ler palet hücresinin ortasında. Yüz +Y, pivot taban ortası z=0.

Çalıştırma (Blender içinde ya da):
  blender -b --factory-startup --python build_police_variants.py -- [--export]
--export: FBX'leri Assets/_Project/Art/City/ altına yazar (H_Police ile aynı eksen ayarları).
"""
import math
import os
import sys

import bmesh
import bpy

COLS, ROWS = 8, 5

# CityPalette.png hücreleri (satır üstten, sütun soldan)
PAL = {
    "white": (3, 7),       # f2f0ea
    "offwhite": (2, 5),    # efebdd
    "black": (0, 5),       # 24252a
    "nearblack": (2, 2),   # 2b2d33
    "darkgrey": (0, 0),    # 3a3f47 (polis pantolonu)
    "slate": (2, 4),       # 3f4e5e
    "grey": (2, 1),        # 6c7280
    "lightgrey": (2, 0),   # b7bec7
    "lightblue": (2, 3),   # 9fc4d8
    "policeblue": (1, 7),  # 3a57a8
    "blue": (1, 3),        # 2e6fbf
    "red": (1, 6),         # c8443f
    "skin": (0, 7),        # f2c6a0
    "yellow": (2, 7),      # f6c445
}

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "City")
PALETTE = os.path.join(OUT, "CityPalette.png")


def uv_of(color):
    row, col = PAL[color]
    return ((col + 0.5) / COLS, 1.0 - (row + 0.5) / ROWS)


class M:
    """Dünya koordinatında tek mesh kurucu."""

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def _paint(self, faces, color):
        u = uv_of(color)
        for f in faces:
            for loop in f.loops:
                loop[self.uv].uv = u

    def box(self, x0, y0, z0, x1, y1, z1, color):
        res = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = res["verts"]
        for v in verts:
            v.co.x = x0 if v.co.x < 0 else x1
            v.co.y = y0 if v.co.y < 0 else y1
            v.co.z = z0 if v.co.z < 0 else z1
        faces = {f for v in verts for f in v.link_faces}
        self._paint(faces, color)
        return verts

    def wheel(self, cx, cy, cz, radius, width, color, hub=None):
        # x eksenine paralel silindir (tekerlek)
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=8, radius1=radius, radius2=radius, depth=width)
        verts = res["verts"]
        for v in verts:
            x, y, z = v.co.x, v.co.y, v.co.z
            v.co.x, v.co.y, v.co.z = cx + z, cy + x, cz + y
        faces = {f for v in verts for f in v.link_faces}
        self._paint(faces, color)
        if hub:
            r = radius * 0.45
            self.box(cx - width * 0.55, cy - r, cz - r, cx + width * 0.55, cy + r, cz + r, hub)

    def build(self, name):
        me = bpy.data.meshes.new(name)
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-5)
        self.bm.to_mesh(me)
        self.bm.free()
        obj = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(obj)
        obj.data.materials.append(palette_material())
        return obj


def palette_material():
    mat = bpy.data.materials.get("Mat_CityPalette")
    if mat is None:
        mat = bpy.data.materials.new("Mat_CityPalette")
        mat.use_nodes = True
        bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(PALETTE, check_existing=True)
        tex.interpolation = "Closest"
        mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def head(m, cx, cy, z0, helmet, visor, visor_color="lightblue"):
    """Polis kafası ölçüsünde kafa + kask. z0 = boyun."""
    m.box(cx - 0.17, cy - 0.15, z0, cx + 0.17, cy + 0.17, z0 + 0.26, "skin")
    # kask: kafayı saran kutu (yüz önü açık), üstte kubbe
    m.box(cx - 0.20, cy - 0.19, z0 + 0.12, cx + 0.20, cy + 0.12, z0 + 0.36, helmet)
    m.box(cx - 0.16, cy - 0.15, z0 + 0.36, cx + 0.16, cy + 0.10, z0 + 0.41, helmet)
    if visor:
        m.box(cx - 0.18, cy + 0.12, z0 + 0.10, cx + 0.18, cy + 0.20, z0 + 0.30, visor_color)


def riot_police():
    m = M()
    armor, under = "slate", "nearblack"
    # bacaklar + botlar
    for sx in (-1, 1):
        x0, x1 = (0.03, 0.17) if sx > 0 else (-0.17, -0.03)
        m.box(x0, -0.09, 0.0, x1, 0.11, 0.08, "black")
        m.box(x0, -0.08, 0.08, x1, 0.08, 0.50, under)
        m.box(x0 - 0.01, 0.06, 0.24, x1 + 0.01, 0.11, 0.38, armor)  # dizlik
    # gövde (zırhlı yelek) + kemer
    m.box(-0.22, -0.13, 0.50, 0.22, 0.13, 0.56, "black")
    m.box(-0.24, -0.14, 0.56, 0.24, 0.15, 0.92, armor)
    m.box(-0.18, 0.15, 0.62, 0.18, 0.17, 0.86, "grey")  # göğüs plakası
    # omuz plakaları + kollar (sol kol kalkanı tutar, öne uzanır)
    m.box(-0.33, -0.12, 0.80, -0.22, 0.12, 0.95, armor)
    m.box(0.22, -0.12, 0.80, 0.33, 0.12, 0.95, armor)
    m.box(0.23, -0.07, 0.52, 0.32, 0.07, 0.80, under)
    m.box(0.23, -0.06, 0.46, 0.32, 0.06, 0.53, "black")  # eldiven
    # cop (sağ elde, aşağı)
    m.box(0.25, -0.03, 0.18, 0.30, 0.02, 0.48, "black")
    head(m, 0.0, 0.0, 0.92, helmet="white", visor=True)
    return m.build("H_RiotPolice")


SHIELD_PIVOT = (-0.275, 0.0, 0.78)  # sol omuz: kalkan kolu buradan döner


def riot_shield():
    """Kalkan + kalkan kolu. Mesh pivot'a göre, obje konumu pivot (gövdeyle aynı uzayda durur)."""
    m = M()
    m.box(-0.32, -0.04, 0.62, -0.23, 0.22, 0.80, "nearblack")   # kalkan kolu öne
    # kalkan: gövde boyunda, hafif kavisli (3 dikey parça), önde
    sy, t = 0.30, 0.04
    m.box(-0.20, sy, 0.10, 0.20, sy + t, 0.98, "lightblue")
    m.box(-0.34, sy - 0.05, 0.12, -0.20, sy - 0.05 + t, 0.96, "lightblue")
    m.box(0.20, sy - 0.05, 0.12, 0.34, sy - 0.05 + t, 0.96, "lightblue")
    # kalkan çerçevesi + pencere şeridi
    m.box(-0.20, sy + t, 0.93, 0.20, sy + t + 0.015, 0.98, "lightgrey")
    m.box(-0.20, sy + t, 0.10, 0.20, sy + t + 0.015, 0.15, "lightgrey")
    m.box(-0.14, sy + t, 0.70, 0.14, sy + t + 0.015, 0.80, "white")
    obj = m.build("H_RiotShield")
    px, py, pz = SHIELD_PIVOT
    for v in obj.data.vertices:
        v.co.x -= px; v.co.y -= py; v.co.z -= pz
    obj.location = SHIELD_PIVOT
    return obj


def motor_police():
    m = M()
    # --- motosiklet: beyaz, ön +Y ---
    wr, ww = 0.22, 0.10
    m.wheel(0.0, 0.52, wr, wr, ww, "black", hub="lightgrey")   # ön
    m.wheel(0.0, -0.50, wr, wr, ww, "black", hub="lightgrey")  # arka
    m.box(-0.07, -0.55, 0.40, 0.07, -0.42, 0.46, "white")      # arka çamurluk
    m.box(-0.07, 0.42, 0.42, 0.07, 0.62, 0.47, "white")        # ön çamurluk
    m.box(-0.16, -0.30, 0.26, 0.16, 0.30, 0.52, "white")       # gövde/depo
    m.box(-0.17, -0.20, 0.30, 0.17, 0.15, 0.38, "blue")        # yan mavi şerit
    m.box(-0.12, -0.08, 0.10, 0.12, 0.20, 0.28, "grey")        # motor bloğu
    m.box(-0.13, -0.50, 0.52, 0.13, -0.05, 0.58, "black")      # sele
    m.box(-0.06, 0.32, 0.28, -0.03, 0.40, 0.70, "lightgrey")   # ön maşa sol
    m.box(0.03, 0.32, 0.28, 0.06, 0.40, 0.70, "lightgrey")     # ön maşa sağ
    m.box(-0.14, 0.30, 0.52, 0.14, 0.44, 0.74, "white")        # far kaportası
    m.box(-0.06, 0.44, 0.58, 0.06, 0.46, 0.66, "yellow")       # far
    m.box(-0.13, 0.33, 0.74, 0.13, 0.38, 0.92, "lightblue")    # ön cam
    m.box(-0.28, 0.30, 0.74, 0.28, 0.34, 0.78, "black")        # gidon
    m.box(-0.14, -0.58, 0.58, -0.04, -0.50, 0.66, "red")       # arka tepe lambası (kırmızı)
    m.box(0.04, -0.58, 0.58, 0.14, -0.50, 0.66, "blue")        # (mavi)
    m.box(-0.18, -0.46, 0.34, 0.18, -0.30, 0.52, "white")      # arka çanta
    # --- sürücü: polis mavisi, beyaz kask, öne eğik ---
    m.box(-0.20, -0.30, 0.58, 0.20, 0.0, 0.70, "darkgrey")     # kalça
    for sx in (-1, 1):
        x0, x1 = (0.12, 0.22) if sx > 0 else (-0.22, -0.12)
        m.box(x0, -0.05, 0.36, x1, 0.15, 0.62, "darkgrey")     # bacak (ayak dayanağına)
        m.box(x0, 0.05, 0.28, x1, 0.20, 0.36, "black")         # bot
    m.box(-0.21, -0.22, 0.70, 0.21, 0.08, 1.02, "policeblue")  # gövde (öne kaymış)
    m.box(-0.22, -0.10, 0.80, 0.22, 0.10, 0.90, "yellow")      # yansıtıcı şerit
    for sx in (-1, 1):
        x0, x1 = (0.21, 0.29) if sx > 0 else (-0.29, -0.21)
        m.box(x0, -0.02, 0.78, x1, 0.30, 0.90, "policeblue")   # kol gidona uzanır
        m.box(x0, 0.28, 0.74, x1, 0.34, 0.82, "black")         # eldiven
    head(m, 0.0, -0.02, 1.0, helmet="white", visor=True, visor_color="nearblack")
    return m.build("H_MotorPolice")


def export(obj):
    path = os.path.join(OUT, obj.name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", path_mode="STRIP")
    print("yazıldı", path)


def main(do_export):
    for name in ("H_RiotPolice", "H_RiotShield", "H_MotorPolice"):
        old = bpy.data.objects.get(name)
        if old is not None:
            bpy.data.objects.remove(old, do_unlink=True)
    riot = riot_police()
    shield = riot_shield()
    motor = motor_police()
    motor.location.x = 1.2
    for o in (riot, shield, motor):
        tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
        print(o.name, "üçgen", tris, "boyut", tuple(round(d, 2) for d in o.dimensions))
    if do_export:
        motor.location.x = 0.0
        export(riot)
        export(shield)
        export(motor)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    main("--export" in argv)
