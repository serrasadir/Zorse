"""Skill efekt modelleri (skill-deneme branch'i). Blender MCP ile üretildi; yerine konmalı: zorse-blender/build_skill_fx.py.

Çalıştırma: Blender -b --factory-startup --python build_skill_fx.py -- [--export]
Çıktılar (Unity Resources'tan koddan yüklenir):
  FX_Quake     Resources/FX/Quake/FX_Quake.fbx      Yer Sarsıntısı halkası (yarıçap 1, CityPalette.png UV, Mat_ShieldPalette)
  FX_Gas       Resources/FX/Gas/FX_Gas.fbx          Sindirim Gazı bulutları (yarıçap 1, köşe rengi + alfa, Mat_SpeedTrail)
  FX_Slime     Resources/FX/Slime/FX_Slime.fbx      Kaygan Zemin lekesi (yarıçap 0.5, köşe rengi + alfa)
  FX_SplitDrop Resources/FX/Split/FX_SplitDrop.fbx  Bölünme jöle parçası (çap 1, köşe rengi + alfa)
Animasyon yok; hepsi koddan (QuakeRingFx, DigestGas, SlipTrail, SplitDrops).
"""
import bpy, bmesh, math, random, os, sys
from mathutils import Vector, Matrix

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project")
PALETTE = os.path.join(ROOT, "Art", "City", "CityPalette.png")   # 8 x 5 renk
OUT = os.path.join(ROOT, "Resources", "FX")


def uv_of(cell):
    return ((cell[0] + 0.5) / 8, (cell[1] + 0.5) / 5)


ASPHALT, ASPHALT_SIDE, CONCRETE, CONCRETE_SIDE, DUST, DUST2, CRACK = (2, 2), (5, 4), (0, 2), (1, 2), (4, 4), (4, 3), (5, 4)


def palette_material():
    mat = bpy.data.materials.new("FXPalette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(PALETTE, check_existing=True)
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def vcol_material():
    mat = bpy.data.materials.new("FXVertexColor")
    mat.use_nodes = True
    nt = mat.node_tree
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    nt.links.new(attr.outputs["Color"], b.inputs["Base Color"])
    nt.links.new(attr.outputs["Alpha"], b.inputs["Alpha"])
    mat.surface_render_method = 'BLENDED'
    return mat


def finish(bm, name, mat):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    print(name, "tris", sum(len(p.vertices) - 2 for p in me.polygons))
    return ob


# ------------------------------------------------------------------ Yer Sarsıntısı
def quake(mat):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")

    def face(vs, cell):
        f = bm.faces.new(vs)
        for l in f.loops:
            l[uvl].uv = uv_of(cell)

    def box(m, pts, top, side):
        vs = [bm.verts.new(m @ p) for p in pts]
        for i, f in enumerate([(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]):
            face([vs[k] for k in f], top if i == 1 else side)

    rnd = random.Random(11)
    n = 12
    for i in range(n):  # kırık asfalt plakaları: dış kenar yerde, iç kenar kalkık
        a = i / n * math.tau + rnd.uniform(-0.1, 0.1)
        r = rnd.uniform(0.86, 0.95)
        w = rnd.uniform(0.28, 0.38); d = rnd.uniform(0.26, 0.36); t = 0.07
        pts = []
        for z in (0.0, t):
            for (x, y) in ((-1, -1), (1, -1), (1, 0), (-1, 0)):
                jx = rnd.uniform(-0.1, 0.1) * w * (-y)
                pts.append(Vector((x * w * 0.5 + jx, y * d, z - t * 0.5)))
        tilt = -math.radians(rnd.uniform(35, 55))
        m = Matrix.Translation((math.cos(a) * r, math.sin(a) * r, 0)) @ Matrix.Rotation(a - math.pi / 2, 4, 'Z') @ Matrix.Rotation(tilt, 4, 'X')
        top, side = (ASPHALT, ASPHALT_SIDE) if i % 3 else (CONCRETE, CONCRETE_SIDE)
        box(m, pts, top, side)
    for i in range(9):  # dışa saçılan taşlar
        a = rnd.uniform(0, math.tau); r = rnd.uniform(1.0, 1.12); s = rnd.uniform(0.045, 0.07)
        m = Matrix.Translation((math.cos(a) * r, math.sin(a) * r, rnd.uniform(0.08, 0.3))) @ \
            Matrix.Rotation(rnd.uniform(0, 3), 4, Vector((rnd.random(), rnd.random(), 1)).normalized())
        box(m, [Vector((x * s, y * s, z * s)) for z in (-1, 1) for (x, y) in ((-1, -1), (1, -1), (1, 1), (-1, 1))], DUST, DUST2)
    seg = 40
    for i in range(seg):  # kenarda kesik toz halkası
        if i % 5 == 4:
            continue
        a0, a1 = i / seg * math.tau, (i + 1) / seg * math.tau
        face([bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, 0.012)) for a, rr in ((a0, 0.98), (a0, 1.06), (a1, 1.06), (a1, 0.98))], DUST)
    for i in range(n):  # merkezden çatlaklar
        a = (i + 0.5) / n * math.tau + rnd.uniform(-0.08, 0.08)
        d = Vector((math.cos(a), math.sin(a), 0)); nn = Vector((-d.y, d.x, 0))
        r0, r1 = rnd.uniform(0.15, 0.25), rnd.uniform(0.62, 0.72)
        mid = (r0 + r1) * 0.5; kink = nn * rnd.uniform(-0.06, 0.06); w = 0.022
        face([bm.verts.new(p + Vector((0, 0, 0.01))) for p in (d * r0, d * mid + kink - nn * w, d * r1, d * mid + kink + nn * w)], CRACK)
    bm.normal_update()
    for f in bm.faces:
        if f.calc_center_median().z < 0.02 and f.normal.z < -0.9:
            f.normal_flip()
    return finish(bm, "FX_Quake", mat)


# ------------------------------------------------------------------ Sindirim Gazı
def gas(mat):
    # Sadece kenar halkası: alanın sınırını gösterir, içi açık (blob çevresini Vakum doldurur, düşmanlar görünsün).
    top, bottom = (0.70, 1.0, 0.45, 0.30), (0.30, 0.65, 0.20, 0.12)
    rnd = random.Random(8)
    bm = bmesh.new()
    col = bm.loops.layers.color.new("Col")

    def lump(center, r, squash=0.75, segs=8, rings=5):
        vs = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)["verts"]
        for v in vs:
            nrm = v.co.copy()
            v.co = Vector((nrm.x * r, nrm.y * r, max(nrm.z, -0.2) * r * squash)) + center
        for f in {f for v in vs for f in v.link_faces}:
            f.smooth = True
            for l in f.loops:
                h = max(0.0, min(1.0, l.vert.co.z / 0.45))
                l[col] = tuple(bottom[i] + (top[i] - bottom[i]) * h for i in range(4))

    n = 10
    for i in range(n):
        a = i / n * math.tau + rnd.uniform(-0.1, 0.1)
        rr = rnd.uniform(0.93, 1.0)
        s = rnd.uniform(0.13, 0.17)
        c = Vector((math.cos(a) * rr, math.sin(a) * rr, 0))
        lump(c + Vector((0, 0, s * 0.5)), s)
        t = Vector((-math.sin(a), math.cos(a), 0))
        s2 = s * rnd.uniform(0.55, 0.7)
        lump(c + t * s * rnd.choice((-0.8, 0.8)) + Vector((0, 0, s2 * 0.45)), s2)
    for i in range(5):  # kenardan yükselen kabarcıklar
        a = rnd.uniform(0, math.tau)
        lump(Vector((math.cos(a) * 0.95, math.sin(a) * 0.95, rnd.uniform(0.35, 0.6))), rnd.uniform(0.03, 0.045), 1.0, 6, 4)
    return finish(bm, "FX_Gas", mat)


# ------------------------------------------------------------------ Kaygan Zemin
def slime(mat):
    # Turkuaz sabunlu su + beyaz köpük (yeşil gazla karışmasın).
    edge, mid, shine, foam = (0.20, 0.62, 0.74, 0.50), (0.45, 0.86, 0.93, 0.58), (0.95, 1.0, 1.0, 0.85), (1.0, 1.0, 1.0, 0.9)
    rnd = random.Random(3)
    bm = bmesh.new()
    col = bm.loops.layers.color.new("Col")

    def paint(f, c):
        for l in f.loops:
            l[col] = c

    def blob(center, radius, segs, wobble, height, seed, highlight=True):
        r = random.Random(seed)
        ph = [r.uniform(0, math.tau) for _ in range(3)]
        ring, inner = [], []
        for i in range(segs):
            a = i / segs * math.tau
            k = 1 + wobble * (0.5 * math.sin(3 * a + ph[0]) + 0.3 * math.sin(5 * a + ph[1]) + 0.2 * math.sin(7 * a + ph[2]))
            p = Vector((math.cos(a) * radius * k, math.sin(a) * radius * k, 0))
            ring.append(bm.verts.new(center + p))
            inner.append(bm.verts.new(center + p * 0.62 + Vector((0, 0, height))))
        tip = bm.verts.new(center + Vector((0, 0, height * 1.25)))
        for i in range(segs):
            j = (i + 1) % segs
            f = bm.faces.new([ring[i], ring[j], inner[j], inner[i]]); paint(f, edge); f.smooth = True
            f2 = bm.faces.new([inner[i], inner[j], tip]); paint(f2, mid); f2.smooth = True
        if highlight:
            hc = center + Vector((-radius * 0.25, radius * 0.2, height * 1.3))
            paint(bm.faces.new([bm.verts.new(hc + Vector((math.cos(a) * radius * 0.16, math.sin(a) * radius * 0.1, 0.002)))
                                for a in [i / 6 * math.tau for i in range(6)]]), shine)

    blob(Vector((0, 0, 0)), 0.5, 18, 0.22, 0.035, 1)
    for i in range(5):
        a = rnd.uniform(0, math.tau); d = rnd.uniform(0.62, 0.85)
        blob(Vector((math.cos(a) * d, math.sin(a) * d, 0)), rnd.uniform(0.05, 0.09), 8, 0.1, 0.02, 10 + i, highlight=False)
    for i in range(7):  # sabun köpüğü
        a = rnd.uniform(0, math.tau); d = rnd.uniform(0.1, 0.38); r = rnd.uniform(0.03, 0.06)
        vs = bmesh.ops.create_uvsphere(bm, u_segments=6, v_segments=4, radius=r)["verts"]
        for v in vs:
            v.co.z = max(v.co.z, 0) * 0.9
            v.co += Vector((math.cos(a) * d, math.sin(a) * d, 0.035))
        for f in {f for v in vs for f in v.link_faces}:
            f.smooth = True
            paint(f, foam)
    return finish(bm, "FX_Slime", mat)


# ------------------------------------------------------------------ Bölünme
def split_drop(mat):
    bottom, top, shine = (0.78, 0.22, 0.42, 0.85), (1.0, 0.55, 0.72, 0.9), (1.0, 0.95, 0.97, 1.0)
    bm = bmesh.new()
    col = bm.loops.layers.color.new("Col")
    for v in bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.5)["verts"]:
        z = v.co.z
        k = 1.0 if z < 0 else 1.0 - 0.25 * (z / 0.5) ** 2
        v.co.x *= k; v.co.y *= k
        v.co.z = (max(z, -0.32) + 0.32) * 0.85
    for f in bm.faces:
        f.smooth = True
        for l in f.loops:
            h = min(1.0, l.vert.co.z / 0.65)
            l[col] = tuple(bottom[i] + (top[i] - bottom[i]) * h for i in range(4))
    c = Vector((-0.14, -0.12, 0.6))
    f = bm.faces.new([bm.verts.new(c + Vector((math.cos(a) * 0.09, math.sin(a) * 0.06, 0))) for a in [i / 6 * math.tau for i in range(6)]])
    for l in f.loops:
        l[col] = shine
    return finish(bm, "FX_SplitDrop", mat)


def export(ob, folder):
    os.makedirs(os.path.join(OUT, folder), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, folder, ob.name + ".fbx"), use_selection=True, object_types={'MESH'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, mesh_smooth_type='FACE', colors_type='SRGB',
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP')


if __name__ == "__main__":
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    pal, vc = palette_material(), vcol_material()
    built = {"Quake": quake(pal), "Gas": gas(vc), "Slime": slime(vc), "Split": split_drop(vc)}
    if "--export" in sys.argv:
        for folder, ob in built.items():
            export(ob, folder)
