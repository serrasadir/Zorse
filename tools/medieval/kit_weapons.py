"""Ortaçağ kiti: silahlar ve mermiler (Silah Gelişim sheet'i).
Kılıç/Meşale: bıçak +Z (yukarı), sap ortası orijinde. Arbalet: ileri −Y, taban orijinde.
Mermiler: pivot merkezde, yönlüyse ucu −Y. Ölçü metre (oyun yeniden ölçekler)."""
import math
import random

import bmesh
from mathutils import Matrix, Vector

from build_medieval import Part, rz


# ---------- yardımcılar ----------

def _verts(faces):
    return list({v for f in faces for v in f.verts})


def _xf(p, faces, mat, cent=(0, 0, 0)):
    """Yüzleri cent etrafında döndür (mat 3x3)."""
    bmesh.ops.rotate(p.bm, verts=_verts(faces), cent=Vector(cent), matrix=mat)
    return faces


def _rx(a):
    return Matrix.Rotation(a, 3, "X")


def _ry(a):
    return Matrix.Rotation(a, 3, "Y")


def _prism(p, outline, thick, color, side=None, y=0.0):
    """XZ düzleminde çokgen (saat yönü tersine, önden −Y bakış), Y'de thick kalınlıkta."""
    bm = p.bm
    front = [bm.verts.new((x, y - thick / 2, z)) for x, z in outline]
    back = [bm.verts.new((x, y + thick / 2, z)) for x, z in outline]
    faces = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
    n = len(outline)
    sides = []
    for i in range(n):
        j = (i + 1) % n
        sides.append(bm.faces.new([front[i], back[i], back[j], front[j]]))
    p._paint(faces, color)
    p._paint(sides, side or color)
    return faces + sides


def _blade(p, w, length, z0, color, thick=0.07, tip=0.16, edge=None, ridge=None):
    """Düz kılıç bıçağı, sivri uç; z0 tabandan yukarı."""
    z1 = z0 + length - tip
    out = [(-w / 2, z0), (w / 2, z0), (w / 2, z1), (0, z1 + tip), (-w / 2, z1)]
    f = _prism(p, out, thick, color, side=edge)
    if ridge:
        p.box((w * 0.22, thick + 0.02, length - tip * 0.9), (0, 0, z0), ridge)
    return f


def _ico(p, r, loc, color, sub=1, scale=(1, 1, 1), jitter=0.0, seed=1):
    res = bmesh.ops.create_icosphere(p.bm, subdivisions=sub, radius=r)
    vs = res["verts"]
    rnd = random.Random(seed)
    for v in vs:
        k = 1.0 + (rnd.uniform(-jitter, jitter) if jitter else 0.0)
        v.co = Vector((v.co.x * scale[0] * k, v.co.y * scale[1] * k, v.co.z * scale[2] * k)) + Vector(loc)
    faces = list({f for v in vs for f in v.link_faces})
    p._paint(faces, color)
    return faces


def _ring_xz(p, radius, bar, loc, color, segs=8, depth=0.05):
    """XZ düzleminde halka (Y'ye bakan nişangah halkası)."""
    out = []
    seg_len = 2 * radius * math.tan(math.pi / segs) + bar * 0.6
    for i in range(segs):
        a = 2 * math.pi * i / segs
        f = p.box((seg_len, depth, bar), (0, 0, radius - bar / 2), color)
        _xf(p, f, _ry(a))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector(loc))
        out += f
    return out


def _flame(p, base, size, seed=0):
    """Birkaç sivri koni: dışta fire, içte straw/gold."""
    x, y, z = base
    p.cyl(size * 0.55, size * 1.1, (x, y, z), "fire", segments=6, radius_top=0.0)
    for i, (dx, dy, h) in enumerate(((0.35, 0.1, 0.75), (-0.35, -0.05, 0.85), (0.05, -0.35, 0.7), (-0.1, 0.35, 0.65))):
        f = p.cyl(size * 0.3, size * h, (0, 0, 0), "fire", segments=5, radius_top=0.0)
        _xf(p, f, rz(i * 1.3) @ _rx(0.35 * (1 if dy >= 0 else -1)) @ _ry(-0.35 * (1 if dx >= 0 else -1)))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((x + dx * size, y + dy * size, z)))
    p.cyl(size * 0.38, size * 0.95, (x, y - size * 0.2, z), "straw", segments=6, radius_top=0.0)
    p.cyl(size * 0.22, size * 0.6, (x, y - size * 0.36, z), "gold", segments=5, radius_top=0.0)


def _crown(p, x, y, z, s, color="gold", thick=0.03):
    """Düz taç (önden), Y'de ince."""
    out = [(-0.5, 0), (0.5, 0), (0.55, 0.75), (0.28, 0.4), (0, 0.8), (-0.28, 0.4), (-0.55, 0.75)]
    return _prism(p, [(x + a * s, z + b * s) for a, b in out], thick, color, y=y)


def _banner(p, w, h, top_z, y, color="blue", trim="gold"):
    """Sivri uçlu sancak (önden −Y)."""
    out = [(-w / 2, top_z - h), (0, top_z - h - w * 0.4), (w / 2, top_z - h), (w / 2, top_z), (-w / 2, top_z)]
    out = [out[1], out[2], out[3], out[4], out[0]]
    _prism(p, out, 0.04, color, side=trim, y=y)
    p.box((w + 0.04, 0.06, 0.04), (0, y, top_z - 0.02), trim)
    _crown(p, 0, y - 0.03, top_z - h * 0.6, w * 0.55)


# ---------- kılıç ----------

def _hilt(p, grip="wood_dark", pommel="wood_dark"):
    p.cyl(0.045, 0.3, (0, 0, -0.15), grip, segments=6)
    p.cyl(0.07, 0.08, (0, 0, -0.23), pommel, segments=6)


def sword_1():
    p = Part("M_Sword_1")
    _hilt(p)
    p.box((0.3, 0.1, 0.09), (0, 0, 0.15), "wood")
    _blade(p, 0.17, 0.62, 0.24, "wood_light", thick=0.08, edge="wood", ridge="wood")
    return p.finish()


def sword_2():
    p = Part("M_Sword_2")
    _hilt(p, grip="wood")
    for z in (-0.1, 0.0, 0.09):
        p.cyl(0.052, 0.045, (0, 0, z), "cream", segments=6)
    p.box((0.3, 0.1, 0.09), (0, 0, 0.15), "wood")
    _blade(p, 0.17, 0.62, 0.24, "wood_light", thick=0.08, edge="wood")
    # çapraz sarılmış bez
    for z, a in ((0.35, 0.45), (0.35, -0.45), (0.53, 0.45), (0.53, -0.45), (0.68, 0.0)):
        f = p.box((0.2, 0.1, 0.045), (0, 0, -0.022), "cream")
        _xf(p, f, _ry(a))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0, 0, z)))
    return p.finish()


def sword_3():
    p = Part("M_Sword_3")
    _hilt(p, grip="wood", pommel="iron")
    for z in (-0.08, 0.04):
        p.cyl(0.052, 0.05, (0, 0, z), "cream", segments=6)
    p.box((0.38, 0.13, 0.1), (0, 0, 0.14), "iron", top="iron_light")
    for x in (-0.2, 0.2):
        p.box((0.06, 0.15, 0.13), (x, 0, 0.125), "iron")
    p.cyl(0.05, 0.04, (0, -0.065, 0.19), "iron_light", segments=6, rot=_rx(math.pi / 2))
    _blade(p, 0.17, 0.62, 0.24, "wood_light", thick=0.08, edge="wood")
    return p.finish()


def sword_4():
    p = Part("M_Sword_4")
    _hilt(p, grip="wood", pommel="gold")
    p.cyl(0.05, 0.12, (0, 0, -0.06), "gold", segments=6)
    # kanatlı altın balçak
    p.box((0.3, 0.12, 0.1), (0, 0, 0.14), "gold")
    for s in (-1, 1):
        f = p.box((0.13, 0.11, 0.08), (0, 0, 0), "gold")
        _xf(p, f, _ry(-s * 0.55))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((s * 0.19, 0, 0.17)))
    gem = p.cyl(0.055, 0.05, (0, 0, 0), "blue", segments=4, rot=_rx(math.pi / 2))
    bmesh.ops.translate(p.bm, verts=_verts(gem), vec=Vector((0, -0.07, 0.19)))
    gem = p.cyl(0.055, 0.05, (0, 0, 0), "blue", segments=4, rot=_rx(-math.pi / 2))
    bmesh.ops.translate(p.bm, verts=_verts(gem), vec=Vector((0, 0.07, 0.19)))
    _blade(p, 0.18, 0.65, 0.24, "gold", thick=0.08, edge="straw_dark", ridge="straw")
    # kırmızı kurdele balçaktan sarkar
    for i, (x, z, a) in enumerate(((-0.17, 0.15, 0.25), (-0.21, -0.02, 0.5))):
        f = p.box((0.06, 0.03, 0.2), (0, 0, -0.2), "red")
        _xf(p, f, _ry(a))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((x, -0.02 - i * 0.015, z)))
    return p.finish()


def sword_5():
    """Evrim: meşale direği."""
    p = Part("M_Sword_5")
    p.cyl(0.06, 1.15, (0, 0, -0.35), "wood", segments=6)
    p.cyl(0.075, 0.1, (0, 0, -0.38), "iron", segments=6)
    p.cyl(0.07, 0.06, (0, 0, 0.1), "iron", segments=6)
    # kafes
    p.cyl(0.13, 0.08, (0, 0, 0.78), "iron", segments=8, radius_top=0.16)
    p.cyl(0.11, 0.28, (0, 0, 0.82), "wood_dark", segments=8, radius_top=0.15, top="fire")
    for i in range(6):
        a = i * math.pi / 3 + math.pi / 6
        f = p.box((0.05, 0.04, 0.34), (0, 0, 0), "iron")
        _xf(p, f, _ry(0.32))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0.16, 0, 0)))
        _xf(p, f, rz(a))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0, 0, 0.82)))
    p.cyl(0.25, 0.06, (0, 0, 1.1), "iron", segments=8, radius_top=0.26)
    p.cyl(0.22, 0.07, (0, 0, 1.105), "wood_dark", segments=8, top="fire")
    _flame(p, (0, 0, 1.12), 0.36)
    _banner(p, 0.24, 0.32, 0.76, -0.08)
    return p.finish()


# ---------- omuz arbaleti ----------

def _limbs(p, z, y, span=0.42, color="wood", cap=None, thick=0.07):
    """Yay kolları XY düzleminde, uçlar geriye (+Y) kıvrık."""
    segs = 4
    pts = []
    for i in range(-segs, segs + 1):
        t = i / segs
        pts.append((t * span, y + 0.14 * t * t))
    for a, b in zip(pts, pts[1:]):
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        ln = math.hypot(b[0] - a[0], b[1] - a[1]) + 0.02
        ang = math.atan2(b[1] - a[1], b[0] - a[0])
        f = p.box((ln, thick, 0.07), (0, 0, 0), color)
        _xf(p, f, rz(ang))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((mx, my, z)))
    tips = (pts[0], pts[-1])
    if cap:
        for x, yy in tips:
            p.box((0.08, 0.09, 0.09), (x, yy, z - 0.01), cap)
    # kirişler: uçlardan mandala
    nut_y = y + 0.3
    for x, yy in tips:
        dx, dy = -x, nut_y - yy
        ln = math.hypot(dx, dy)
        f = p.box((ln, 0.02, 0.02), (0, 0, 0), "cream")
        _xf(p, f, rz(math.atan2(dy, dx)))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((x + dx / 2, yy + dy / 2, z + 0.025)))
    return tips


def _mount(p, plate=None):
    p.box((0.3, 0.3, 0.08), (0, 0.05, 0), "wood_dark")
    p.box((0.12, 0.12, 0.18), (0, 0.05, 0.08), "wood")
    p.box((0.2, 0.2, 0.06), (0, 0.05, 0.24), "wood_dark")
    if plate:
        p.box((0.32, 0.32, 0.03), (0, 0.05, 0.08), plate)


def _bolt(p, z, y0, y1):
    p.box((0.03, y1 - y0, 0.03), (0, (y0 + y1) / 2, z), "wood_light")
    tip = p.cyl(0.04, 0.09, (0, 0, 0), "iron_light", segments=4, radius_top=0.0, rot=_rx(math.pi / 2))
    bmesh.ops.translate(p.bm, verts=_verts(tip), vec=Vector((0, y0, z + 0.015)))


def _stock(p, z=0.3, plates=False):
    p.box((0.14, 0.8, 0.12), (0, 0.02, z), "wood", top="wood_light")
    p.box((0.12, 0.2, 0.08), (0, 0.38, z - 0.06), "wood_dark")
    if plates:
        for s in (-1, 1):
            p.box((0.03, 0.3, 0.1), (s * 0.075, -0.2, z + 0.01), "iron")
            p.box((0.03, 0.22, 0.1), (s * 0.075, 0.25, z + 0.01), "iron")
            for yy in (-0.2, 0.25):
                p.box((0.02, 0.03, 0.03), (s * 0.092, yy, z + 0.045), "iron_light")
        p.box((0.17, 0.08, 0.14), (0, -0.38, z - 0.01), "iron", top="iron_light")


def _shield_plate(p, y, z):
    """Kalkan plakası: z = üst kenar, sehpanın önünde aşağı sarkar."""
    out = [(0, z - 0.27), (0.14, z - 0.18), (0.14, z), (-0.14, z), (-0.14, z - 0.18)]
    _prism(p, out, 0.04, "blue", side="gold", y=y)
    p.box((0.31, 0.05, 0.035), (0, y, z - 0.02), "gold")
    _crown(p, 0, y - 0.025, z - 0.17, 0.15)


def _sight(p, y, z):
    p.box((0.04, 0.04, 0.16), (0, y, z), "iron")
    _ring_xz(p, 0.08, 0.025, (0, y, z + 0.24), "gold", segs=6)
    p.box((0.14, 0.02, 0.018), (0, y, z + 0.231), "gold")
    p.box((0.018, 0.02, 0.14), (0, y, z + 0.17), "gold")


def crossbow_1():
    p = Part("M_Crossbow_1")
    _mount(p)
    _stock(p)
    _limbs(p, 0.34, -0.3)
    _bolt(p, 0.42, -0.42, 0.0)
    return p.finish()


def crossbow_2():
    p = Part("M_Crossbow_2")
    _mount(p, plate="iron")
    _stock(p, plates=True)
    _limbs(p, 0.34, -0.3, cap="iron")
    _bolt(p, 0.42, -0.46, 0.0)
    return p.finish()


def crossbow_3():
    p = Part("M_Crossbow_3")
    _mount(p, plate="iron")
    _stock(p, plates=True)
    _limbs(p, 0.34, -0.3, cap="gold")
    _bolt(p, 0.42, -0.46, 0.0)
    _sight(p, 0.1, 0.42)
    _shield_plate(p, -0.24, 0.3)
    return p.finish()


def crossbow_4():
    p = Part("M_Crossbow_4")
    _mount(p, plate="gold")
    _stock(p)  # namlular üstü kapatır; plakalar tri bütçesi için yok
    _limbs(p, 0.3, -0.26, cap="gold")
    # çift namlu
    for x in (-0.11, 0.11):
        p.cyl(0.075, 0.6, (x, 0.26, 0.5), "iron", segments=6, rot=_rx(math.pi / 2))
        p.cyl(0.045, 0.02, (x, -0.355, 0.5), "dark", segments=6, rot=_rx(math.pi / 2))
        for yy in (-0.3, 0.1):
            p.cyl(0.088, 0.06, (x, yy + 0.03, 0.5), "gold", segments=6, rot=_rx(math.pi / 2))
    p.box((0.1, 0.5, 0.1), (0, 0.0, 0.43), "iron")
    _sight(p, 0.22, 0.59)
    _shield_plate(p, -0.24, 0.3)
    return p.finish()


# ---------- mermiler ----------

def flaming_arrow():
    p = Part("M_FlamingArrow")
    p.box((0.035, 0.55, 0.035), (0, 0.03, -0.0175), "wood")
    tip = p.cyl(0.06, 0.14, (0, 0, 0), "iron_light", segments=4, radius_top=0.0, rot=_rx(math.pi / 2))
    bmesh.ops.translate(p.bm, verts=_verts(tip), vec=Vector((0, -0.24, 0)))
    for a in (0, math.pi / 2):
        f = p.box((0.14, 0.12, 0.012), (0, 0, -0.006), "red")
        _xf(p, f, _ry(a))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0, 0.24, 0)))
    # alev uca yakın, geriye yatık (uçuşta arkaya savrulur)
    f0 = len(p.bm.faces)
    _flame(p, (0, 0, 0), 0.22)
    flame = list(p.bm.faces)[f0:]
    _xf(p, flame, _rx(-1.25))
    bmesh.ops.translate(p.bm, verts=_verts(flame), vec=Vector((0, -0.16, 0)))
    return p.finish()


def catapult_rock():
    p = Part("M_CatapultRock")
    faces = _ico(p, 0.2, (0, 0, 0), "stone", sub=2, scale=(1.1, 0.95, 0.85), jitter=0.12, seed=4)
    for i, f in enumerate(faces):
        if i % 5 == 0:
            p._paint([f], "stone_light")
        elif i % 7 == 0:
            p._paint([f], "stone_dark")
    return p.finish()


def rotten_tomato():
    p = Part("M_RottenTomato")
    faces = _ico(p, 0.2, (0, 0, 0), "red", sub=2, scale=(1.0, 1.0, 0.82), jitter=0.04, seed=2)
    for f in faces:
        c = f.calc_center_median()
        if c.y < -0.08 and c.z < 0.02 and abs(c.x) < 0.1:
            p._paint([f], "roof_dark")
    # çürük akıntı ön altta
    _ico(p, 0.075, (0.03, -0.165, -0.02), "dirt", sub=1, scale=(1.3, 0.4, 1.0), seed=3)
    _ico(p, 0.04, (0.06, -0.15, -0.1), "dirt", sub=1, scale=(0.9, 0.5, 1.6), seed=5)
    # yaprak yıldızı + sap
    for i in range(5):
        f = p.box((0.13, 0.045, 0.025), (0.06, 0, 0), "leaf_dark")
        _xf(p, f, rz(i * 2 * math.pi / 5))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0, 0, 0.155)))
    p.cyl(0.02, 0.07, (0, 0, 0.16), "leaf_dark", segments=5)
    return p.finish()


def rotten_cabbage():
    p = Part("M_RottenCabbage")
    core = _ico(p, 0.2, (0, 0, 0), "grass", sub=2, scale=(1.0, 1.0, 0.9), jitter=0.05, seed=7)
    rnd = random.Random(11)
    for f in core:
        c = f.calc_center_median()
        if c.y < -0.05 and rnd.random() < 0.3:
            p._paint([f], "dirt")
    # dış yapraklar
    for i in range(5):
        f = _ico(p, 0.13, (0, 0, 0), "leaf", sub=1, scale=(1.15, 0.4, 1.0), jitter=0.08, seed=20 + i)
        _xf(p, f, _rx(0.3))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((0, -0.16, -0.05)))
        _xf(p, f, rz(i * 2 * math.pi / 5 + 0.6))
    # kahverengi çürük lekeleri (ön yüzde)
    for x, z, s in ((-0.06, 0.03, 0.05), (0.08, -0.06, 0.04), (0.02, 0.12, 0.035)):
        _ico(p, s, (x, -0.2, z), "wood_dark", sub=1, scale=(1.2, 0.35, 1.0), seed=int(s * 100))
    return p.finish()


def iron_horseshoe():
    p = Part("M_IronHorseshoe")
    R, w, h = 0.17, 0.09, 0.07
    segs = 7
    for i in range(segs):
        a0 = math.pi + math.pi * i / segs
        a1 = math.pi + math.pi * (i + 1) / segs
        am = (a0 + a1) / 2
        ln = 2 * R * math.sin(math.pi / segs / 2) + 0.03
        f = p.box((ln, w, h), (0, 0, -h / 2), "red")
        _xf(p, f, rz(am + math.pi / 2))
        bmesh.ops.translate(p.bm, verts=_verts(f), vec=Vector((R * math.cos(am), R * math.sin(am), 0)))
    for s in (-1, 1):
        p.box((w, 0.14, h), (s * R, 0.07, -h / 2), "red")
        p.box((w + 0.02, 0.08, h + 0.02), (s * R, 0.18, -h / 2 - 0.01), "iron_light")
        # çivi delikleri
        for yy in (0.0, 0.09):
            p.box((0.03, 0.03, 0.01), (s * R, yy, h / 2), "dark")
    for a in (math.radians(235), math.radians(305)):
        p.box((0.03, 0.03, 0.01), (R * math.cos(a), R * math.sin(a), h / 2), "dark")
    # merkez: U'nun ortası
    bmesh.ops.translate(p.bm, verts=p.bm.verts, vec=Vector((0, -0.02, 0)))
    return p.finish()


BUILDERS = {
    "M_Sword_1": sword_1, "M_Sword_2": sword_2, "M_Sword_3": sword_3, "M_Sword_4": sword_4, "M_Sword_5": sword_5,
    "M_Crossbow_1": crossbow_1, "M_Crossbow_2": crossbow_2, "M_Crossbow_3": crossbow_3, "M_Crossbow_4": crossbow_4,
    "M_FlamingArrow": flaming_arrow, "M_CatapultRock": catapult_rock, "M_RottenTomato": rotten_tomato,
    "M_RottenCabbage": rotten_cabbage, "M_IronHorseshoe": iron_horseshoe,
}
