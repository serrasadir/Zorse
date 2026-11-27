"""Ortaçağ kalesi haritası (2. harita) model kiti.

Referans: ChatGPT sheet'leri (Bahar, 2026-11-20). Stil: low-poly, düz gölgeli, tek palet dokusu.
Her parça kutu/silindir/çatı ilkellerinden; renk = MedievalPalette.png hücresi (UV tek hücreye).
Çalıştırma: blender -b --factory-startup --python build_medieval.py -- [--export] [--render <png>] [--only a,b]
Çıktı: Assets/_Project/Art/City/Medieval/M_*.fbx + MedievalPalette.png
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Medieval"))
PALETTE_PNG = os.path.join(OUT_DIR, "MedievalPalette.png")

# Palet: 8x8 hücre, 8 sütun. Sıra değişmez (UV'ler buna bağlı); yeni renk sona eklenir.
PALETTE = [
    ("stone", (0.62, 0.60, 0.57)), ("stone_dark", (0.45, 0.44, 0.43)), ("stone_light", (0.78, 0.76, 0.72)), ("moss", (0.47, 0.62, 0.25)),
    ("wood", (0.66, 0.42, 0.22)), ("wood_dark", (0.40, 0.25, 0.14)), ("wood_light", (0.80, 0.58, 0.33)), ("plaster", (0.95, 0.90, 0.78)),
    ("straw", (0.96, 0.78, 0.30)), ("straw_dark", (0.82, 0.60, 0.18)), ("roof", (0.80, 0.30, 0.22)), ("roof_dark", (0.62, 0.22, 0.17)),
    ("iron", (0.35, 0.36, 0.40)), ("iron_light", (0.62, 0.64, 0.68)), ("blue", (0.22, 0.36, 0.72)), ("gold", (0.98, 0.76, 0.20)),
    ("grass", (0.52, 0.74, 0.30)), ("leaf", (0.42, 0.66, 0.24)), ("leaf_dark", (0.30, 0.50, 0.18)), ("window", (0.99, 0.82, 0.38)),
    ("water", (0.25, 0.60, 0.88)), ("red", (0.82, 0.22, 0.20)), ("cream", (0.98, 0.95, 0.86)), ("dark", (0.18, 0.16, 0.16)),
    ("skin", (0.96, 0.78, 0.62)), ("dirt", (0.68, 0.48, 0.30)), ("fire", (1.00, 0.55, 0.15)), ("white", (0.97, 0.97, 0.95)),
    ("roof_brown", (0.55, 0.34, 0.22)), ("roof_beige", (0.86, 0.72, 0.50)), ("roof_slate", (0.40, 0.45, 0.58)), ("maroon", (0.50, 0.14, 0.26)),
]
CELL, COLS = 8, 8
ROWS = (len(PALETTE) + COLS - 1) // COLS
INDEX = {name: i for i, (name, _) in enumerate(PALETTE)}


def uv_of(color):
    i = INDEX[color]
    col, row = i % COLS, i // COLS
    w, h = COLS * CELL, ROWS * CELL
    return ((col * CELL + CELL / 2) / w, 1.0 - (row * CELL + CELL / 2) / h)


def write_palette():
    w, h = COLS * CELL, ROWS * CELL
    img = bpy.data.images.new("MedievalPalette", w, h, alpha=False)
    px = [0.0] * (w * h * 4)
    for i, (_, rgb) in enumerate(PALETTE):
        col, row = i % COLS, i // COLS
        for y in range(row * CELL, row * CELL + CELL):
            for x in range(col * CELL, col * CELL + CELL):
                k = ((h - 1 - y) * w + x) * 4  # Blender görüntüsü alttan başlar
                px[k:k + 4] = [rgb[0], rgb[1], rgb[2], 1.0]
    img.pixels = px
    os.makedirs(OUT_DIR, exist_ok=True)
    img.filepath_raw = PALETTE_PNG
    img.file_format = "PNG"
    img.save()
    return img


# ---------- ilkeller (hepsi tek bmesh'e eklenir, renk UV'si yüz başına) ----------

class Part:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def _paint(self, faces, color):
        u = uv_of(color)
        for f in faces:
            for loop in f.loops:
                loop[self.uv].uv = u

    def box(self, size, loc, color, top=None, rot_z=0.0):
        """size (x,y,z), loc = taban merkezi. top: üst yüz rengi (yoksa aynı)."""
        r = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = r["verts"]
        for v in verts:
            v.co = Vector((v.co.x * size[0], v.co.y * size[1], (v.co.z + 0.5) * size[2]))
        if rot_z:
            bmesh.ops.rotate(self.bm, verts=verts, cent=(0, 0, 0), matrix=_rz(rot_z))
        bmesh.ops.translate(self.bm, verts=verts, vec=Vector(loc))
        faces = list({f for v in verts for f in v.link_faces})
        self._paint(faces, color)
        if top:
            self._paint([f for f in faces if f.normal.z > 0.9], top)
        return faces

    def cyl(self, radius, height, loc, color, segments=8, top=None, radius_top=None, rot=None):
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                  radius1=radius, radius2=radius if radius_top is None else radius_top, depth=height)
        verts = r["verts"]
        bmesh.ops.translate(self.bm, verts=verts, vec=Vector((0, 0, height / 2)))
        if rot:
            bmesh.ops.rotate(self.bm, verts=verts, cent=(0, 0, 0), matrix=rot)
        bmesh.ops.translate(self.bm, verts=verts, vec=Vector(loc))
        faces = list({f for v in verts for f in v.link_faces})
        self._paint(faces, color)
        if top:
            self._paint([f for f in faces if f.normal.z > 0.9], top)
        return faces

    def roof(self, width, depth, height, loc, color, overhang=0.12, gable=None, rot_z=0.0, rows=4, edge=None):
        """Beşik çatı, kademeli (kiremit/hasır sıraları): sırt X ekseninde. loc = saçak tabanı merkezi.
        gable: alın üçgeni rengi, edge: her sıranın alt kenar rengi (yoksa aynı)."""
        w, d = width / 2 + overhang, depth / 2 + overhang
        t = 0.1
        bm = self.bm
        faces, lips = [], []
        for side in (-1, 1):
            for r in range(rows):
                a0, a1 = r / rows, (r + 1) / rows + 0.08          # sıralar üst üste biner
                a1 = min(a1, 1.0)
                y0, y1 = side * d * (1 - a0), side * d * (1 - a1)
                z0, z1 = height * a0, height * a1
                lift = 0.05 * (rows - r)                            # alttaki sıra biraz dışarıda: basamak
                p = [Vector((-w, y0 + side * lift, z0)), Vector((w, y0 + side * lift, z0)),
                     Vector((w, y1, z1)), Vector((-w, y1, z1))]
                q = [v + Vector((0, 0, t)) for v in p]
                vs = [bm.verts.new(v) for v in p + q]
                fs = []
                for idx in ((0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (3, 2, 6, 7), (0, 3, 7, 4)):
                    try:
                        fs.append(bm.faces.new([vs[i] for i in idx]))
                    except ValueError:
                        pass
                lips.append(fs[2] if len(fs) > 2 else fs[0])
                faces += fs
                if rot_z:
                    bmesh.ops.rotate(bm, verts=vs, cent=(0, 0, 0), matrix=_rz(rot_z))
                bmesh.ops.translate(bm, verts=vs, vec=Vector(loc))
        self._paint(faces, color)
        if edge:
            self._paint(lips, edge)
        # sırt kirişi
        ridge = self.box((width + overhang * 2 + 0.05, 0.22, 0.18), (0, 0, height - 0.02), edge or color)
        if rot_z:
            bmesh.ops.rotate(bm, verts=list({v for f in ridge for v in f.verts}), cent=(0, 0, 0), matrix=_rz(rot_z))
        bmesh.ops.translate(bm, verts=list({v for f in ridge for v in f.verts}), vec=Vector(loc))
        # alın üçgenleri (duvar renginde)
        gw, gd = width / 2, depth / 2
        for sx in (-1, 1):
            vs = [bm.verts.new(Vector((sx * gw, -gd, 0))), bm.verts.new(Vector((sx * gw, gd, 0))),
                  bm.verts.new(Vector((sx * gw, 0, height)))]
            f = bm.faces.new(vs if sx > 0 else list(reversed(vs)))
            if rot_z:
                bmesh.ops.rotate(bm, verts=vs, cent=(0, 0, 0), matrix=_rz(rot_z))
            bmesh.ops.translate(bm, verts=vs, vec=Vector(loc))
            self._paint([f], gable or color)
        return faces

    def frame(self, w, h, loc, glass="window", wood="wood_dark", face=-1):
        """Ön yüze (−Y) çerçeveli pencere/kapı: loc = alt kenar ortası (duvar yüzeyinde)."""
        x, y, z = loc
        e = 0.12
        self.box((w + e * 2, 0.12, h + e * 2), (x, y + face * 0.02, z - e), wood)
        self.box((w, 0.14, h), (x, y + face * 0.04, z), glass)
        if glass == "window":
            self.box((0.07, 0.16, h), (x, y + face * 0.05, z), wood)
            self.box((w, 0.16, 0.07), (x, y + face * 0.05, z + h / 2 - 0.035), wood)

    def finish(self):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0001)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for p in mesh.polygons:
            p.use_smooth = False
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.collection.objects.link(obj)
        return obj


def _rz(a):
    from mathutils import Matrix
    return Matrix.Rotation(a, 3, "Z")


# ---------- parçalar: kategori modülleri (kit_*.py), her biri BUILDERS = {ad: fonksiyon} ----------

MODULES = ["kit_buildings", "kit_props", "kit_walls", "kit_characters", "kit_bosses", "kit_weapons", "kit_decor", "kit_village"]


def rz(a):
    return _rz(a)


def all_builders(reload=False):
    import importlib
    if HERE not in sys.path:
        sys.path.insert(0, HERE)
    out = {}
    for name in MODULES:
        try:
            mod = importlib.import_module(name)
            if reload:
                mod = importlib.reload(mod)
        except ModuleNotFoundError:
            continue
        except Exception as e:  # başka kategori yarım kalmışsa diğerleri yine kurulsun
            print("[medieval] %s yüklenemedi: %r" % (name, e))
            continue
        for key, fn in getattr(mod, "BUILDERS", {}).items():
            out[key] = fn
            BUILDER_MODULE[key] = name
    return out


BUILDER_MODULE = {}


# ---------- çıktı ----------

def material(palette_img):
    mat = bpy.data.materials.new("MedievalPalette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = palette_img
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs[0], bsdf.inputs[0])
    return mat


# Oyun ölçeği: kit insanı 1.7 m modellendi, şehirdeki yaya ~1.16 birim → dışa aktarırken ×0.7.
GAME_SCALE = 0.7

# Unity'deki şehir hattı davranışı ön ekten okur (CityKitPipeline): B_ bina (engel + NavMesh deliği, yenir),
# P_ sokak eşyası (yenir), L_ zemin (çarpışmasız), H_ karakter, X_ efekt/mermi/boss parçası, G_ silah görünümü.
MODULE_PREFIX = {"kit_buildings": "B_", "kit_props": "P_", "kit_walls": "B_", "kit_characters": "H_",
                 "kit_bosses": "X_", "kit_weapons": "G_", "kit_decor": "D_",
                 "kit_village": "P_"}
PREFIX_OVERRIDE = {"M_Tile": "L_", "M_Windmill": "P_", "M_WindmillSails": "X_", "M_OilWarning": "X_",
                   "M_PortcullisGrid": "X_",
                   "M_HouseL": "B_", "M_LongHouse": "B_", "M_CottageSlate": "B_", "M_CottageRed": "B_",
                   **{"M_PathStone%d" % i: "D_" for i in range(1, 6)}}


def export_name(module, obj_name):
    """M_Cottage (kit_buildings) → B_MedCottage. Şehir kitiyle ad çakışmasın diye 'Med' eklenir."""
    base = obj_name[2:] if obj_name.startswith("M_") else obj_name
    prefix = MODULE_PREFIX.get(module, "X_")
    for key in sorted(PREFIX_OVERRIDE, key=len, reverse=True):
        if obj_name == key or (key == "M_Tile" and obj_name.startswith(key)):
            prefix = PREFIX_OVERRIDE[key]
            break
    return prefix + "Med" + base


def export(obj, name=None):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT_DIR, (name or obj.name) + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, global_scale=GAME_SCALE,
                             apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE", path_mode="STRIP")
    print("Yazıldı:", path)


def render(objs, path):
    # Yan yana diz, oyun açısından (60° eğik) çek.
    x = 0.0
    for o in objs:
        width = o.dimensions.x
        o.location.x = x + width / 2
        x += width + 1.2
    center = Vector((x / 2 - 0.6, 0, 1.2))
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    bpy.context.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(8.0, x * 0.75)
    cam.rotation_euler = (math.radians(55), 0, math.radians(-20))
    cam.location = center + Vector((-math.sin(math.radians(20)) * 0 + 4.0, -14.0, 10.0))
    direction = center - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene = bpy.context.scene
    scene.camera = cam
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "TEXTURE"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = 1400, 700
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("Render:", path)


# ---------- "Soft" görünüm (Bahar seçti, 2026-11-20): tek kademe kenar yuvarlatma + yumuşak normaller + köşe karartması ----------
# Köşe karartması (AO) "Col" köşe rengine pişirilir; Unity'de Mat_MedievalPalette (BlobSurvivor/PaletteAO) dokuyla çarpar.
# Poligon ~2–3.5×. Zemin karosu (L_) ve efekt parçaları dokunulmaz.
SOFT_WIDTH = {"kit_buildings": 0.05, "kit_walls": 0.045, "kit_props": 0.025, "kit_characters": 0.02,
              "kit_bosses": 0.035, "kit_weapons": 0.015, "kit_village": 0.04}
AO_FLOOR = 0.6     # en koyu köşe (1 = karartma yok)
AO_SAMPLES = 32


def soften(obj, module, all_objs):
    width = SOFT_WIDTH.get(module)
    if width is None or obj.name.startswith("M_Tile"):
        # Yuvarlatma yok ama shader köşe rengini çarpar: beyaz (karartmasız) köşe rengi şart.
        me = obj.data
        col = me.color_attributes.get("Col") or me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        for d in col.data:
            d.color = (1.0, 1.0, 1.0, 1.0)
        return
    m = obj.modifiers.new("Bevel", "BEVEL")
    m.width = width
    m.segments = 1
    m.limit_method = "ANGLE"
    m.angle_limit = 0.9
    w = obj.modifiers.new("WN", "WEIGHTED_NORMAL")
    w.keep_sharp = True
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = True
    bake_ao(obj, all_objs)


def _ground():
    g = bpy.data.objects.get("AO_Ground")
    if g is None:
        bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, 0))
        g = bpy.context.object
        g.name = "AO_Ground"
    return g


def bake_ao(obj, all_objs):
    """Tek başına (diğer parçalar gizli), zemine göre AO → "Col" köşe rengi, AO_FLOOR..1 aralığına sıkıştırılır."""
    scene = bpy.context.scene
    ground = _ground()
    hidden = []
    for o in all_objs:
        if o is not obj and not o.hide_render:
            o.hide_render = True
            hidden.append(o)
    me = obj.data
    col = me.color_attributes.get("Col") or me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    me.color_attributes.active_color = col
    scene.render.engine = "CYCLES"
    scene.cycles.samples = AO_SAMPLES
    scene.render.bake.target = "VERTEX_COLORS"
    if scene.world is None:
        scene.world = bpy.data.worlds.new("W")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.bake(type="AO")
    finally:
        for o in hidden:
            o.hide_render = False
    for d in col.data:
        c = d.color
        v = (c[0] + c[1] + c[2]) / 3.0
        v = AO_FLOOR + (1.0 - AO_FLOOR) * (max(0.0, min(1.0, v)) ** 0.5)
        d.color = (v, v, v, 1.0)
    ground.hide_render = False


def main(argv):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    img = write_palette()
    mat = material(img)
    only = None
    if "--only" in argv:
        only = argv[argv.index("--only") + 1].split(",")
    objs, keys = [], []
    for key, fn in all_builders().items():
        if only and key not in only:
            continue
        o = fn()
        o.data.materials.append(mat)
        objs.append(o)
        keys.append(key)
    if "--export" in argv:
        soft_on = "--hard" not in argv
        for o, key in zip(objs, keys):
            if soft_on:
                soften(o, BUILDER_MODULE.get(key, ""), objs)
            export(o, export_name(BUILDER_MODULE.get(key, ""), o.name))
    if "--render" in argv:
        render(objs, argv[argv.index("--render") + 1])


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
