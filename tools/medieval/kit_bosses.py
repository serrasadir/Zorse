"""Ortaçağ kiti: boss'lar. Mini boss'lar: taban merkezi orijinde, ön yüz −Y.
Uçan ejderha: pivot gövde merkezi, baş −Y. Tünemiş ejderha: taban z=0 (kule tepesine konur)."""
import math

import bmesh
from mathutils import Matrix, Vector

from build_medieval import Part, rz


# ---------- yardımcılar ----------

def _verts(faces):
    return list({v for f in faces for v in f.verts})


def move(faces, M):
    """Yüz grubunu 4x4 matrisle taşır/döndürür."""
    for v in _verts(faces):
        v.co = M @ v.co
    return faces


def _paint_new(p, verts, color):
    faces = list({f for v in verts for f in v.link_faces})
    p._paint(faces, color)
    return faces


def ball(p, c, r, color, rot=None, hi=False):
    """Elipsoit: c merkez, r (rx, ry, rz). hi = daha yuvarlak (büyük parçalar)."""
    if hi:
        res = bmesh.ops.create_icosphere(p.bm, subdivisions=2 if hi == 2 else 1, radius=1.0)
    else:
        res = bmesh.ops.create_uvsphere(p.bm, u_segments=8, v_segments=5, radius=1.0)
    vs = res["verts"]
    for v in vs:
        v.co = Vector((v.co.x * r[0], v.co.y * r[1], v.co.z * r[2]))
    if rot:
        for v in vs:
            v.co = rot @ v.co
    bmesh.ops.translate(p.bm, verts=vs, vec=Vector(c))
    return _paint_new(p, vs, color)


def _align(d):
    """+Z'yi d yönüne çeviren 3x3."""
    return Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix()


def tube(p, a, b, r, color, seg=8, r2=None, twist=0.0):
    """a'dan b'ye silindir/koni (r2 = uç yarıçapı)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    res = bmesh.ops.create_cone(p.bm, cap_ends=True, cap_tris=False, segments=seg,
                                radius1=r, radius2=r if r2 is None else r2, depth=d.length)
    vs = res["verts"]
    m = _align(d) @ Matrix.Rotation(twist, 3, "Z")
    for v in vs:
        v.co = m @ (v.co + Vector((0, 0, d.length / 2))) + a
    return _paint_new(p, vs, color)


def spike(p, base, tip, r, color, seg=4):
    return tube(p, base, tip, r, color, seg=seg, r2=0.0)


def bar(p, a, b, w, h, color):
    """a'dan b'ye kesitli kiriş (w genişlik, h kalınlık)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    res = bmesh.ops.create_cube(p.bm, size=1.0)
    vs = res["verts"]
    q = d.to_track_quat("Y", "Z" if abs(d.normalized().z) < 0.95 else "X").to_matrix()
    for v in vs:
        v.co = q @ Vector((v.co.x * w, (v.co.y + 0.5) * d.length, v.co.z * h)) + a
    return _paint_new(p, vs, color)


def prism(p, pts, off, color):
    """Düz levha: pts çokgeni (pts[0]'dan yelpaze), off yönünde kalınlık."""
    bm = p.bm
    off = Vector(off)
    top = [bm.verts.new(Vector(q)) for q in pts]
    bot = [bm.verts.new(Vector(q) + off) for q in pts]
    faces = []
    n = len(pts)
    for i in range(1, n - 1):
        faces.append(bm.faces.new((top[0], top[i], top[i + 1])))
        faces.append(bm.faces.new((bot[0], bot[i + 1], bot[i])))
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((top[i], bot[i], bot[j], top[j])))
    p._paint(faces, color)
    return faces


def wheel(p, x, y, z, r, w, side):
    """Dingil X ekseninde; side = ±1 (dış yüz yönü)."""
    out = []
    out += tube(p, (x - w / 2, y, z), (x + w / 2, y, z), r, "iron", seg=10, twist=math.pi / 10)
    out += tube(p, (x - w / 2 - 0.02, y, z), (x + w / 2 + 0.02, y, z), r * 0.78, "wood", seg=10, twist=math.pi / 10)
    out += tube(p, (x, y, z), (x + side * (w / 2 + 0.1), y, z), r * 0.32, "straw_dark", seg=8)
    return out


def shield(p, cx, y, zb, w, h, depth=0.08, tilt=0.0):
    """Ön yüze (−Y) bakan mavi kalkan-flama, altın kenar ve taç. zb = alt uç; tilt: üst ucun geriye yatışı (derece)."""
    def outline(s, inset):
        hw = w / 2 - inset
        return [(cx, y, zb + inset * 1.4), (cx + hw, y, zb + h * 0.28), (cx + hw, y, zb + h - inset),
                (cx - hw, y, zb + h - inset), (cx - hw, y, zb + h * 0.28)]
    fs = prism(p, outline(1, 0.0), (0, depth, 0), "gold")
    fs += move(prism(p, outline(1, 0.07), (0, depth, 0), "blue"), Matrix.Translation((0, -0.03, 0)))
    fs += crown(p, cx, y - 0.06, zb + h * 0.42, w * 0.42)
    if tilt:
        piv = Vector((cx, y, zb))
        move(fs, Matrix.Translation(piv) @ Matrix.Rotation(math.radians(-tilt), 4, "X") @ Matrix.Translation(-piv))


def crown(p, cx, y, z, s):
    """Öne bakan küçük altın taç; (cx, y, z) alt kenar ortası, s genişlik."""
    f = p.box((s, 0.06, s * 0.3), (cx, y, z), "gold")
    for dx, hh in ((-0.42, 0.55), (0.0, 0.7), (0.42, 0.55)):
        f += spike(p, (cx + dx * s, y, z + s * 0.25), (cx + dx * s, y, z + s * hh), s * 0.16, "gold", seg=4)
    return f


def jitter(faces, amount, seed):
    import random
    rnd = random.Random(seed)
    for v in _verts(faces):
        v.co += Vector((rnd.uniform(-amount, amount), rnd.uniform(-amount, amount), rnd.uniform(-amount, amount)))


# ---------- mini boss 1: mancınık ----------

def siege_catapult():
    p = Part("M_SiegeCatapult")
    wr = 0.42
    for sx in (-1, 1):
        for y in (-0.95, 0.95):
            wheel(p, sx * 0.9, y, wr, wr, 0.26, sx)
        p.box((0.24, 3.0, 0.26), (sx * 0.62, 0, 0.42), "wood", top="wood_light")
    for y in (-0.95, 0.95):
        tube(p, (-0.78, y, wr), (0.78, y, wr), 0.08, "iron", seg=6)
    for y in (-1.38, -0.2, 1.38):
        p.box((1.5, 0.24, 0.24), (0, y, 0.5), "wood", top="wood_light")
    p.box((1.0, 1.9, 0.08), (0, 0.15, 0.6), "wood_dark")
    # A çerçeveleri
    for sx in (-1, 1):
        x = sx * 0.62
        p.box((0.22, 0.22, 1.25), (x, 0.45, 0.68), "wood", top="wood_light")
        bar(p, (x, -0.75, 0.68), (x, 0.38, 1.75), 0.18, 0.18, "wood")
        bar(p, (x, 1.3, 0.68), (x, 0.52, 1.75), 0.18, 0.18, "wood")
        for y in (0.45,):
            p.box((0.28, 0.28, 0.12), (x, y, 1.0), "iron")
        p.box((0.3, 0.3, 0.14), (x, -1.38, 0.62), "iron")
        p.box((0.3, 0.3, 0.14), (x, 1.38, 0.62), "iron")
    p.box((1.58, 0.28, 0.26), (0, 0.45, 1.85), "wood", top="wood_light")
    p.box((0.55, 0.32, 0.2), (0, 0.42, 1.92), "cream")             # yastık
    for sx in (-1, 1):
        p.box((0.3, 0.34, 0.3), (sx * 0.79, 0.45, 1.83), "iron")
    # burgu demeti + kol
    tube(p, (-0.52, -0.5, 0.95), (0.52, -0.5, 0.95), 0.2, "straw", seg=8)
    for x in (-0.3, -0.1, 0.1, 0.3):
        tube(p, (x - 0.04, -0.5, 0.95), (x + 0.04, -0.5, 0.95), 0.23, "cream", seg=8)
    bar(p, (0, -0.5, 0.95), (0, 0.85, 2.3), 0.2, 0.2, "wood_light")
    for t in (0.35, 0.7):
        c = Vector((0, -0.5, 0.95)).lerp(Vector((0, 0.85, 2.3)), t)
        p.box((0.26, 0.26, 0.1), (c.x, c.y, c.z - 0.05), "iron")
    tube(p, (0, 0.92, 2.22), (0, 0.98, 2.42), 0.42, "wood_dark", seg=10, r2=0.5)
    stone = ball(p, (0, 0.98, 2.68), (0.36, 0.36, 0.32), "stone", hi=True)
    jitter(stone, 0.05, 3)
    # arka vinç
    tube(p, (-0.62, 1.15, 0.9), (0.62, 1.15, 0.9), 0.14, "wood_dark", seg=8)
    tube(p, (-0.3, 1.15, 0.9), (0.3, 1.15, 0.9), 0.18, "cream", seg=8)
    for sx in (-1, 1):
        p.box((0.08, 0.08, 0.4), (sx * 0.76, 1.15, 0.9), "iron")
    # ön flama
    shield(p, 0, -1.55, 0.3, 1.0, 1.15, tilt=32)
    return p.finish()


def catapult_stone():
    p = Part("M_CatapultStone")
    f = ball(p, (0, 0, 0), (0.32, 0.3, 0.28), "stone", hi=True)
    jitter(f, 0.04, 7)
    f2 = ball(p, (0.08, -0.06, 0.12), (0.18, 0.16, 0.16), "stone_light", hi=True)
    jitter(f2, 0.02, 9)
    return p.finish()


# ---------- mini boss 2: kuşatma kulesi ----------

def frustum(p, w0, d0, w1, d1, z0, z1, color, top=None):
    res = bmesh.ops.create_cube(p.bm, size=1.0)
    vs = res["verts"]
    for v in vs:
        if v.co.z > 0:
            v.co = Vector((v.co.x * w1, v.co.y * d1, z1))
        else:
            v.co = Vector((v.co.x * w0, v.co.y * d0, z0))
    f = _paint_new(p, vs, color)
    if top:
        p._paint([q for q in f if q.normal.z > 0.9], top)
    return f


def siege_tower():
    p = Part("M_SiegeTower")
    W0, D0, W1, D1, Z0, Z1 = 2.3, 2.4, 1.8, 1.9, 0.75, 3.75

    def half_w(z):
        return (W0 + (W1 - W0) * (z - Z0) / (Z1 - Z0)) / 2

    def half_d(z):
        return (D0 + (D1 - D0) * (z - Z0) / (Z1 - Z0)) / 2

    for sx in (-1, 1):
        for y in (-1.05, 0.0, 1.05):
            wheel(p, sx * 1.22, y, 0.45, 0.45, 0.28, sx)
    for y in (-1.05, 0.0, 1.05):
        tube(p, (-1.1, y, 0.45), (1.1, y, 0.45), 0.08, "iron", seg=6)
    p.box((2.4, 3.1, 0.32), (0, 0, 0.45), "wood_dark", top="wood")
    frustum(p, W0, D0, W1, D1, Z0, Z1, "wood_light")
    # kat bantları
    for z in (Z0, 1.75, 2.75):
        frustum(p, half_w(z) * 2 + 0.1, half_d(z) * 2 + 0.1, half_w(z + 0.18) * 2 + 0.1, half_d(z + 0.18) * 2 + 0.1,
                z, z + 0.18, "wood_dark")
    # köşe dikmeleri
    for sx in (-1, 1):
        for sy in (-1, 1):
            bar(p, (sx * (W0 / 2 + 0.02), sy * (D0 / 2 + 0.02), Z0), (sx * (W1 / 2 + 0.02), sy * (D1 / 2 + 0.02), Z1),
                0.2, 0.2, "wood_dark")
            p.box((0.3, 0.3, 0.25), (sx * (W1 / 2 + 0.02), sy * (D1 / 2 + 0.02), Z1 - 0.3), "iron")
            p.box((0.32, 0.32, 0.25), (sx * (W0 / 2 + 0.02), sy * (D0 / 2 + 0.02), Z0 + 0.05), "iron")
    # arka ve yan çapraz destekler (alt kat)
    za, zb = Z0 + 0.18, 1.75
    for sy in (1,):
        ya, yb = half_d(za) + 0.04, half_d(zb) + 0.04
        bar(p, (-half_w(za) + 0.1, ya, za), (half_w(zb) - 0.1, yb, zb), 0.14, 0.1, "wood_dark")
        bar(p, (half_w(za) - 0.1, ya, za), (-half_w(zb) + 0.1, yb, zb), 0.14, 0.1, "wood_dark")
    za2, zb2 = 1.93, 2.75
    ya, yb = half_d(za2) + 0.04, half_d(zb2) + 0.04
    bar(p, (-half_w(za2) + 0.1, ya, za2), (half_w(zb2) - 0.1, yb, zb2), 0.14, 0.1, "wood_dark")
    bar(p, (half_w(za2) - 0.1, ya, za2), (-half_w(zb2) + 0.1, yb, zb2), 0.14, 0.1, "wood_dark")
    for sx in (-1,):
        xa, xb = half_w(za) + 0.04, half_w(zb) + 0.04
        bar(p, (sx * xa, -half_d(za) + 0.1, za), (sx * xb, half_d(zb) - 0.1, zb), 0.1, 0.14, "wood_dark")
        bar(p, (sx * xa, half_d(za) - 0.1, za), (sx * xb, -half_d(zb) + 0.1, zb), 0.1, 0.14, "wood_dark")
    # üst katta mavi zırh levhaları (yanlar), altın kenarlı
    for sx in (-1, 1):
        z0, z1 = 2.25, 3.65
        for y, hw in (((-0.05, 0.62),) if sx < 0 else ((-0.4, 0.42),)):
            for col, ins, off in (("gold", 0.0, 0.05), ("blue", 0.07, 0.09)):
                x0, x1 = half_w(z0) + off, half_w(z1) + off
                prism(p, [(sx * x0, y - hw + ins, z0 + ins), (sx * x0, y + hw - ins, z0 + ins),
                          (sx * x1, y + hw - ins, z1 - ins), (sx * x1, y - hw + ins, z1 - ins)], (-sx * 0.06, 0, 0), col)
            for yy in (y - hw + 0.16, y + hw - 0.16):
                for zz in (z0 + 0.2, z1 - 0.25):
                    p.box((0.08, 0.1, 0.1), (sx * (half_w(zz) + 0.12), yy, zz), "iron_light")
    # ön: koçbaşı açıklığı + koçbaşı
    yf = -half_d(Z0) - 0.03
    p.box((1.1, 0.1, 0.9), (0, yf, 0.8), "dark")
    tube(p, (0, 0.6, 1.2), (0, -2.0, 1.2), 0.22, "wood_light", seg=8)
    for y in (-0.6, -1.3):
        tube(p, (0, y - 0.06, 1.2), (0, y + 0.06, 1.2), 0.25, "iron", seg=8)
    tube(p, (0, -1.85, 1.2), (0, -2.05, 1.2), 0.3, "iron", seg=8)
    ball(p, (0, -2.3, 1.2), (0.42, 0.4, 0.42), "iron", hi=True)
    for sx in (-1, 1):
        bar(p, (sx * 0.2, -0.9, 1.33), (sx * 0.35, yf, 1.72), 0.05, 0.05, "dark")  # askı zinciri
    # ön flama
    shield(p, 0, -half_d(1.8) - 0.06, 1.8, 1.2, 1.5, tilt=5)
    # tepe: platform + mazgallar
    p.box((2.1, 2.2, 0.18), (0, 0, Z1), "wood_dark", top="wood")
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.box((0.46, 0.46, 0.7), (sx * 0.84, sy * 0.89, Z1 + 0.18), "wood", top="wood_light")
            p.box((0.5, 0.5, 0.1), (sx * 0.84, sy * 0.89, Z1 + 0.88), "iron")
        p.box((0.3, 0.55, 0.5), (sx * 0.9, 0.0, Z1 + 0.18), "wood", top="wood_light")
        p.box((0.55, 0.3, 0.5), (0.0, sx * 0.95, Z1 + 0.18), "wood", top="wood_light")
        for sy in (-1, 1):
            p.box((0.3, 0.3, 0.25), (sx * 0.9, sy * 0.45, Z1 + 0.18), "wood_dark")
            p.box((0.3, 0.3, 0.25), (sy * 0.45, sx * 0.95, Z1 + 0.18), "wood_dark")
    p.box((0.6, 0.6, 0.05), (-0.2, 0.2, Z1 + 0.18), "wood_dark")    # kapak
    # bayrak
    fz = Z1 + 0.18
    tube(p, (0.4, 0.35, fz), (0.4, 0.35, fz + 2.0), 0.06, "wood_dark", seg=6)
    ball(p, (0.4, 0.35, fz + 2.07), (0.11, 0.11, 0.11), "gold")
    prism(p, [(0.44, 0.35, fz + 1.95), (1.6, 0.35, fz + 1.75), (1.3, 0.35, fz + 1.5), (1.6, 0.35, fz + 1.25), (0.44, 0.35, fz + 1.25)],
          (0, 0.06, 0), "blue")
    crown(p, 0.85, 0.33, fz + 1.45, 0.3)
    # yan merdiven (+X)
    lx = lambda z: half_w(z) + 0.18
    for y in (0.15, 0.75):
        bar(p, (lx(0.6), y, 0.6), (lx(Z1 + 0.3), y, Z1 + 0.3), 0.09, 0.09, "wood_light")
    z = 0.95
    while z < Z1:
        tube(p, (lx(z), 0.15, z), (lx(z), 0.75, z), 0.04, "wood", seg=5)
        z += 0.36
    return p.finish()


# ---------- final boss: ejderha ----------

HEAD_SCALE = 1.35


def dragon_head(p):
    """Baş, yerel: merkez orijin, burun −Y."""
    f = []
    f += ball(p, (0, 0, 0), (0.62, 0.58, 0.52), "red", hi=True)
    f += ball(p, (0, -0.52, -0.12), (0.46, 0.46, 0.3), "red", hi=True)
    f += ball(p, (0, -0.48, -0.3), (0.4, 0.4, 0.17), "cream")
    for sx in (-1, 1):
        f += ball(p, (sx * 0.14, -0.86, 0.1), (0.06, 0.04, 0.035), "dark")                   # burun deliği
        f += ball(p, (sx * 0.26, -0.43, 0.14), (0.19, 0.13, 0.22), "white")
        f += ball(p, (sx * 0.23, -0.54, 0.12), (0.09, 0.05, 0.12), "dark")
        f += bar(p, (sx * 0.44, -0.49, 0.41), (sx * 0.08, -0.61, 0.31), 0.09, 0.07, "roof_dark")    # kızgın kaş
        f += spike(p, (sx * 0.3, 0.05, 0.3), (sx * 0.58, 0.62, 0.92), 0.14, "gold", seg=5)         # boynuz
        f += spike(p, (sx * 0.1, 0.22, 0.42), (sx * 0.14, 0.62, 0.75), 0.08, "gold", seg=4)
        f += spike(p, (sx * 0.52, 0.1, -0.05), (sx * 0.92, 0.42, 0.05), 0.11, "gold", seg=4)       # yanak
        f += spike(p, (sx * 0.18, -0.8, -0.25), (sx * 0.18, -0.82, -0.42), 0.05, "white", seg=4)  # diş
    return f


# Kanat, sağ (+X) taraf, kök orijinde: omuz, bilek, parmak ucu / çukur sırası (yelpaze kökten).
WING = [(0.65, -0.35, 0.35), (1.75, -0.4, 0.75),                      # ön kenar → bilek
        (3.05, 0.15, 0.7), (2.55, 0.45, 0.5), (2.75, 1.25, 0.4),      # 1. parmak, çukur, 2. parmak
        (2.1, 1.0, 0.3), (1.75, 1.9, 0.15), (0.75, 1.25, 0.0), (0.0, 1.05, -0.1)]
WRIST, FINGERS = 1, (2, 4, 6)


def wing(p, sx):
    """Yarasa kanadı; sx = ±1. Kök orijinde, döndürmek için yüzleri döndürür."""
    pts = [Vector((0, 0, 0))] + [Vector((sx * x, y, z)) for x, y, z in WING]
    f = prism(p, pts, (0, 0, -0.08), "fire")
    rim = pts[1:]
    f += tube(p, pts[0], rim[0], 0.12, "roof", seg=6, r2=0.1)
    f += tube(p, rim[0], rim[WRIST], 0.1, "roof", seg=6, r2=0.08)
    for i in FINGERS:
        f += tube(p, rim[WRIST], rim[i], 0.07, "roof", seg=5, r2=0.03)
    f += spike(p, rim[WRIST], rim[WRIST] + Vector((sx * 0.1, -0.3, 0.12)), 0.07, "gold", seg=4)
    return f


def dragon_flying():
    p = Part("M_DragonFlying")
    # gövde (pivot merkez)
    ball(p, (0, 0, 0), (0.95, 1.1, 0.82), "red", hi=2)
    ball(p, (0, -0.25, -0.3), (0.76, 0.86, 0.6), "cream", hi=True)
    ball(p, (0, -0.95, 0.3), (0.55, 0.5, 0.5), "red", hi=True)                     # boyun
    move(dragon_head(p), Matrix.Translation((0, -1.55, 0.62)) @ Matrix.Rotation(math.radians(-6), 4, "X")
         @ Matrix.Scale(HEAD_SCALE, 4))
    # kuyruk
    tail = [((0, 1.0, -0.1), (0.58, 0.58, 0.48)), ((0.02, 1.48, -0.2), (0.46, 0.45, 0.37)),
            ((0.1, 1.9, -0.28), (0.36, 0.38, 0.28)), ((0.22, 2.28, -0.35), (0.27, 0.33, 0.2)),
            ((0.38, 2.62, -0.4), (0.18, 0.28, 0.14))]
    for c, r in tail:
        ball(p, c, r, "red", hi=r[0] > 0.4)
    prism(p, [(0.5, 2.85, -0.4), (0.85, 3.0, -0.4), (0.62, 3.45, -0.4), (0.25, 3.05, -0.4)], (0, 0, -0.12), "roof_dark")
    # sırt dikenleri (üstten görünür)
    spine = [((0, -0.55, 0.78), 0.15), ((0, -0.05, 0.86), 0.2), ((0, 0.5, 0.74), 0.19),
             ((0, 1.05, 0.38), 0.16), ((0.05, 1.65, 0.07), 0.13), ((0.2, 2.2, -0.14), 0.1), ((0.4, 2.62, -0.28), 0.07)]
    for c, s in spine:
        c = Vector(c)
        spike(p, c - Vector((0, 0, 0.1)), c + Vector((0, 0.25, s * 1.9)), s, "gold", seg=4)
    # bacaklar (toplanmış)
    for sx in (-1, 1):
        ball(p, (sx * 0.68, 0.45, -0.45), (0.36, 0.48, 0.36), "red", hi=True)
        ball(p, (sx * 0.66, 0.9, -0.72), (0.2, 0.3, 0.15), "red")
        for dx in (-0.09, 0.09):
            spike(p, (sx * 0.66 + dx, 1.12, -0.75), (sx * 0.66 + dx, 1.34, -0.82), 0.06, "cream", seg=4)
        ball(p, (sx * 0.55, -0.75, -0.5), (0.22, 0.3, 0.22), "red")                     # kollar
        ball(p, (sx * 0.45, -1.0, -0.72), (0.16, 0.16, 0.13), "red")
        for dx in (-0.07, 0.07):
            spike(p, (sx * 0.45 + dx, -1.08, -0.78), (sx * 0.45 + dx, -1.22, -0.86), 0.045, "cream", seg=4)
    return p.finish()


# Kanatlar gövdeden ayrı (Unity'de omuzdan çırpar, WingFlap); gövdeyle aynı uzayda, kök = omuz.
WING_ROOT = (0.6, -0.4, 0.45)


def dragon_wing(sx):
    p = Part("M_DragonWing" + ("R" if sx > 0 else "L"))
    move(wing(p, sx), Matrix.Translation((sx * WING_ROOT[0], WING_ROOT[1], WING_ROOT[2])) @ Matrix.Scale(0.98, 4))
    return p.finish()


def fireball():
    """Ejderha ateş topu: önde (−Y) sarı çekirdek, alev dilleri arkaya (+Y) savrulur."""
    p = Part("M_Fireball")
    ball(p, (0, -0.1, 0), (0.4, 0.4, 0.4), "window", hi=True)
    ball(p, (0, 0.18, 0), (0.43, 0.5, 0.43), "fire", hi=True)
    for k in range(7):
        a = k * 2 * math.pi / 7
        c, sn = math.cos(a), math.sin(a)
        spike(p, (c * 0.28, 0.15, sn * 0.28), (c * 0.2, 0.9 + 0.15 * (k % 3), sn * 0.2), 0.17,
              "roof" if k % 2 else "fire", seg=4)
    spike(p, (0, 0.3, 0), (0, 1.3, 0), 0.2, "roof", seg=5)
    return p.finish()


def dragon_perched():
    p = Part("M_DragonPerched")
    # oturan gövde
    ball(p, (0, 0.15, 0.72), (1.0, 0.92, 0.72), "red", hi=2)                        # kalça
    ball(p, (0, -0.05, 1.5), (0.85, 0.75, 0.9), "red", hi=2)                        # göğüs
    ball(p, (0, -0.5, 1.3), (0.62, 0.42, 0.8), "cream", hi=True)                   # karın
    ball(p, (0, -0.25, 2.2), (0.5, 0.48, 0.42), "red", hi=True)                     # boyun
    move(dragon_head(p), Matrix.Translation((0, -0.45, 2.82)) @ Matrix.Rotation(math.radians(4), 4, "X")
         @ Matrix.Scale(HEAD_SCALE, 4))
    for sx in (-1, 1):
        # arka bacaklar
        ball(p, (sx * 0.82, -0.12, 0.5), (0.46, 0.62, 0.48), "red", hi=True)
        ball(p, (sx * 0.78, -0.72, 0.13), (0.3, 0.42, 0.13), "red")
        for dx in (-0.13, 0.0, 0.13):
            spike(p, (sx * 0.78 + dx, -1.02, 0.1), (sx * 0.78 + dx, -1.24, 0.04), 0.065, "cream", seg=4)
        # ön kollar: bükük (dirsek dışarıda), pençeler önde yerde
        sh, el, pw = Vector((sx * 0.68, -0.4, 1.7)), Vector((sx * 0.95, -0.72, 1.0)), Vector((sx * 0.62, -1.05, 0.2))
        ball(p, sh, (0.3, 0.3, 0.32), "red")
        tube(p, sh, el, 0.24, "red", seg=7, r2=0.2)
        ball(p, el, (0.21, 0.21, 0.21), "red")
        tube(p, el, pw, 0.2, "red", seg=7, r2=0.18)
        ball(p, pw, (0.26, 0.3, 0.18), "red")
        for dx in (-0.11, 0.0, 0.11):
            spike(p, (pw.x + dx, -1.28, 0.15), (pw.x + dx, -1.45, 0.05), 0.055, "cream", seg=4)
        # yarı katlanmış kanatlar: yukarı kalkık, geriye süpürülmüş
        M = (Matrix.Translation((sx * 0.55, 0.3, 1.95)) @ Matrix.Rotation(sx * math.radians(35), 4, "Z")
             @ Matrix.Rotation(-sx * math.radians(50), 4, "Y") @ Matrix.Scale(0.62, 4))
        move(wing(p, sx), M)
    # kuyruk: arkadan sağa dolanır
    tail = [((0.25, 0.95, 0.32), (0.5, 0.55, 0.32)), ((0.62, 1.25, 0.27), (0.44, 0.44, 0.27)),
            ((1.0, 1.35, 0.22), (0.36, 0.36, 0.22)), ((1.35, 1.18, 0.18), (0.3, 0.3, 0.18)),
            ((1.56, 0.92, 0.15), (0.24, 0.26, 0.15)), ((1.68, 0.65, 0.12), (0.18, 0.22, 0.12))]
    for c, r in tail:
        ball(p, c, r, "red", hi=r[0] > 0.45)
    prism(p, [(1.7, 0.42, 0.1), (2.0, 0.3, 0.1), (1.78, -0.08, 0.1), (1.45, 0.25, 0.1)], (0, 0, 0.12), "roof_dark")
    # sırt dikenleri
    spine = [((0, 0.15, 2.45), 0.1), ((0, 0.52, 2.0), 0.13), ((0, 0.7, 1.5), 0.14), ((0, 0.92, 1.0), 0.13),
             ((0.3, 1.32, 0.55), 0.1), ((0.8, 1.6, 0.48), 0.09), ((1.3, 1.6, 0.38), 0.08), ((1.68, 1.15, 0.3), 0.07)]
    for c, s in spine:
        c = Vector(c)
        spike(p, c, c + Vector((0, s * 1.5, s * 1.4)), s, "gold", seg=4)
    return p.finish()


BUILDERS = {
    "siege_catapult": siege_catapult,
    "catapult_stone": catapult_stone,
    "siege_tower": siege_tower,
    "dragon_flying": dragon_flying,
    "dragon_wing_l": lambda: dragon_wing(-1),
    "dragon_wing_r": lambda: dragon_wing(1),
    "fireball": fireball,
    "dragon_perched": dragon_perched,
}
