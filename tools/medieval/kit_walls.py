"""Ortaçağ kiti: duvarlar, kapılar, delikler, tehlikeler, zemin, yel değirmeni (Sheet 6).
Duvar parçaları 3 m uzunluğunda, X ekseni boyunca, merkez orijinde; ön yüz −Y.
Köşe parçaları: köşe noktası orijinde, kollar +X ve +Y yönünde 1.5 m (dış yüzler −Y ve −X).
Fare deliği: duvar dibinde ~0.5 m koyu kemer + önünde altın kırıntılar."""
import math
import random

import bmesh
from mathutils import Matrix, Vector

from build_medieval import Part, rz

RX90 = Matrix.Rotation(math.pi / 2, 3, "X")       # silindir ekseni −Y (ön yüze bakan disk)


def _ry(a):
    return Matrix.Rotation(a, 3, "Y")


# ---------- yardımcılar ----------

def _obox(p, size, center, color, mat=None):
    """Merkezli, serbest döndürülmüş kutu (kemer taşı, halat, yel değirmeni kanadı)."""
    r = bmesh.ops.create_cube(p.bm, size=1.0)
    vs = r["verts"]
    for v in vs:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    if mat is not None:
        bmesh.ops.rotate(p.bm, verts=vs, cent=(0, 0, 0), matrix=mat)
    bmesh.ops.translate(p.bm, verts=vs, vec=Vector(center))
    faces = list({f for v in vs for f in v.link_faces})
    p._paint(faces, color)
    return faces


def _beam(p, a, b, thick, color):
    """a'dan b'ye uzanan kare kesitli çubuk."""
    a, b = Vector(a), Vector(b)
    d = b - a
    mat = Vector((0, 1, 0)).rotation_difference(d.normalized()).to_matrix()
    return _obox(p, (thick, d.length, thick), (a + b) / 2, color, mat)


def B(p, size, loc, color, top=None, swap=False):
    """box; swap = X/Y ayna (köşe parçasının ikinci kolu)."""
    if swap:
        size = (size[1], size[0], size[2])
        loc = (loc[1], loc[0], loc[2])
    return p.box(size, loc, color, top=top)


def _arch(p, cx, y, zs, r, t, d, color, n=5, alt=None):
    """XZ düzleminde yarım daire kemer taşları: iç yarıçap r, kalınlık t, derinlik d."""
    rm = r + t / 2
    seg = 2 * rm * math.sin(math.pi / (2 * n)) * 1.15
    for i in range(n):
        a = math.pi * (i + 0.5) / n
        c = alt if (alt and i % 2) else color
        _obox(p, (t, d, seg), (cx + rm * math.cos(a), y, zs + rm * math.sin(a)), c, _ry(-a))


def _mouse_hole(p, x, face_y, rim="stone_light", w=0.44, h=0.52, crumbs=True, rnd=None):
    """Ön yüze (face_y, negatif) koyu kemerli delik; çerçeve öne taşar, delik içeride görünür."""
    r = w / 2
    zs = h - r
    p.box((w, 0.08, zs), (x, face_y + 0.01, 0), "dark")
    p.cyl(r, 0.08, (x, face_y + 0.05, zs), "dark", segments=10, rot=RX90)
    for sx in (-1, 1):
        p.box((0.1, 0.1, zs), (x + sx * (r + 0.05), face_y - 0.02, 0), rim)
    _arch(p, x, face_y - 0.02, zs, r, 0.1, 0.1, rim, n=5)
    if crumbs:
        _crumbs(p, x, face_y - 0.15, rnd or random.Random(3))


def _crumbs(p, x, y, rnd, n=5):
    for _ in range(n):
        s = rnd.uniform(0.07, 0.12)
        p.box((s, s * rnd.uniform(0.8, 1.2), 0.05), (x + rnd.uniform(-0.35, 0.35), y - rnd.uniform(0.05, 0.55), 0),
              "gold", rot_z=rnd.uniform(0, math.pi))


def _tufts(p, pts, rnd):
    for x, y in pts:
        p.box((0.26, 0.18, 0.14), (x, y, 0), "leaf", rot_z=rnd.uniform(-0.6, 0.6))
        p.box((0.16, 0.14, 0.2), (x + 0.07, y + 0.02, 0), "grass", rot_z=rnd.uniform(-0.6, 0.6))


def _crown(p, x, y, z, s=1.0):
    p.box((0.32 * s, 0.04, 0.1 * s), (x, y, z), "gold")
    for dx in (-0.12, 0.0, 0.12):
        p.box((0.07 * s, 0.04, 0.12 * s), (x + dx * s, y, z + 0.1 * s), "gold")


def _banner(p, x, face_y, z_top, w=0.7, h=1.3):
    """Mavi taçlı sancak, ön yüze asılı (çatallı uç)."""
    p.box((w + 0.18, 0.08, 0.08), (x, face_y - 0.06, z_top), "gold")
    p.box((w, 0.04, h), (x, face_y - 0.03, z_top - h), "blue")
    for sx in (-1, 1):
        p.box((w * 0.42, 0.04, 0.22), (x + sx * w * 0.29, face_y - 0.03, z_top - h - 0.22), "blue")
        p.box((0.05, 0.05, h), (x + sx * (w / 2 - 0.025), face_y - 0.04, z_top - h), "gold")
    _crown(p, x, face_y - 0.06, z_top - h * 0.55, s=w / 0.7)


def _flag(p, x, y, z, pole=1.8):
    p.cyl(0.05, pole, (x, y, z), "wood_dark", segments=6)
    p.cyl(0.09, 0.12, (x, y, z + pole), "gold", segments=6)
    p.box((0.9, 0.04, 0.55), (x + 0.47, y, z + pole - 0.62), "blue")
    _crown(p, x + 0.5, y - 0.04, z + pole - 0.45, s=0.7)


def _bricks(p, x0, x1, z0, z1, face_y, rnd, n, avoid=(), swap=False, back=True):
    """Ön (ve arka) yüze hafif çıkıntılı taş blokları."""
    rows = max(1, int((z1 - z0) / 0.42))
    placed = 0
    tries = 0
    used = []
    while placed < n and tries < n * 8:
        tries += 1
        z = z0 + rnd.randrange(rows) * 0.42 + 0.06
        w = rnd.uniform(0.4, 0.7)
        x = rnd.uniform(x0 + w / 2, x1 - w / 2)
        if any(ax0 - w / 2 < x < ax1 + w / 2 and az0 - 0.3 < z < az1 for ax0, ax1, az0, az1 in avoid):
            continue
        side = -1 if (not back or placed % 2 == 0) else 1
        if any(s == side and abs(z - uz) < 0.1 and abs(x - ux) < (w + uw) / 2 + 0.08 for s, ux, uz, uw in used):
            continue
        used.append((side, x, z, w))
        col = rnd.choice(("stone_light", "stone_light", "stone_dark"))
        B(p, (w, 0.08, 0.3), (x, side * (abs(face_y) + 0.02), z), col, swap=swap)
        placed += 1


# ---------- 1. ahşap çit (zayıf) ----------

def _fence_run(p, x0, x1, swap=False, rail_gap=None):
    n = int(round((x1 - x0) / 0.5))
    for i in range(n):
        x = x0 + 0.25 + i * 0.5
        B(p, (0.38, 0.16, 1.05), (x, 0, 0), "wood", swap=swap)
        loc = (x, 0, 1.05) if not swap else (0, x, 1.05)
        p.cyl(0.22, 0.3, loc, "wood_light", segments=4, radius_top=0.0, rot=rz(math.radians(45)))
    for z in (0.3, 0.75):
        spans = [(x0, x1)]
        if rail_gap and z < 0.5:
            spans = [(x0, rail_gap[0]), (rail_gap[1], x1)]
        for a, b in spans:
            B(p, (b - a, 0.12, 0.17), ((a + b) / 2, -0.13, z), "wood_dark", swap=swap)
        for x in (x0 + 0.25, x1 - 0.25):
            B(p, (0.14, 0.14, 0.22), (x, -0.16, z - 0.02), "iron", swap=swap)


def fence():
    p = Part("M_Fence")
    _fence_run(p, -1.5, 1.5)
    return p.finish()


def fence_corner():
    p = Part("M_FenceCorner")
    _fence_run(p, 0.0, 1.5)
    _fence_run(p, 0.0, 1.5, swap=True)
    p.box((0.34, 0.34, 1.25), (-0.1, -0.1, 0), "wood_dark")
    p.cyl(0.26, 0.32, (-0.1, -0.1, 1.25), "wood", segments=4, radius_top=0.0, rot=rz(math.radians(45)))
    for z in (0.28, 0.73):
        p.box((0.4, 0.4, 0.2), (-0.1, -0.1, z), "iron")
    return p.finish()


def fence_hole():
    p = Part("M_FenceHole")
    _fence_run(p, -1.5, 1.5, rail_gap=(-0.42, 0.42))
    p.box((0.9, 0.08, 0.62), (0, 0.04, 0), "wood_dark")                     # delik arkası tahta
    _mouse_hole(p, 0, -0.1, rim="wood_dark", w=0.86, h=0.8, rnd=random.Random(11))
    return p.finish()


# ---------- 2. alçak taş duvar (orta) ----------

def _stone_run(p, x0, x1, rnd, swap=False, hole_x=None):
    L = x1 - x0
    B(p, (L, 0.9, 0.2), ((x0 + x1) / 2, 0, 0), "stone_dark", swap=swap)
    z = 0.2
    for row in range(3):
        x = x0 + (0.0 if row % 2 == 0 else 0.35)
        if row % 2:
            B(p, (0.33, 0.84, 0.38), (x0 + 0.17, 0, z), "stone", swap=swap)
        while x < x1 - 0.05:
            w = min(rnd.uniform(0.55, 0.85), x1 - x)
            if w < 0.2:
                break
            B(p, (w - 0.05, 0.84, 0.38), (x + w / 2, 0, z), rnd.choice(("stone", "stone", "stone_light")), swap=swap)
            x += w
        z += 0.4
    B(p, (L, 0.96, 0.12), ((x0 + x1) / 2, 0, z), "stone_light", swap=swap)
    for _ in range(2):
        mx = rnd.uniform(x0 + 0.4, x1 - 0.4)
        B(p, (rnd.uniform(0.45, 0.7), 0.5, 0.1), (mx, rnd.uniform(-0.1, 0.15), z + 0.12), "moss", swap=swap)
        B(p, (0.25, 0.08, 0.3), (mx, -0.47, z - 0.2), "moss", swap=swap)    # sarkan yosun
    pts = [(x0 + 0.35, -0.52), (x1 - 0.4, -0.55)]
    if hole_x is None:
        pts.append(((x0 + x1) / 2 + 0.2, -0.53))
    for x, y in pts:
        if swap:
            x, y = y, x
        _tufts(p, [(x, y)], rnd)
    return z + 0.12


def stonewall():
    p = Part("M_StoneWall")
    _stone_run(p, -1.5, 1.5, random.Random(7))
    return p.finish()


def stonewall_corner():
    p = Part("M_StoneWallCorner")
    rnd = random.Random(8)
    _stone_run(p, -0.45, 1.5, rnd)
    _stone_run(p, 0.45, 1.5, rnd, swap=True)
    p.box((1.0, 1.0, 0.2), (0, 0, 1.4), "stone_light")                      # köşe başlığı
    p.box((0.6, 0.6, 0.1), (0.05, 0.05, 1.6), "moss")
    return p.finish()


def stonewall_hole():
    p = Part("M_StoneWallHole")
    rnd = random.Random(9)
    _stone_run(p, -1.5, 1.5, rnd, hole_x=0.0)
    _mouse_hole(p, 0, -0.46, rim="stone_light", w=0.86, h=0.82, rnd=rnd)
    return p.finish()


# ---------- 3. yüksek kale duvarı (güçlü) ----------

CW_D, CW_H, CW_T = 1.2, 3.4, 0.6        # derinlik, mazgal altı yükseklik, mazgal dişi


def _castle_run(p, x0, x1, rnd, merlons, swap=False, avoid=(), n_bricks=10, mw=0.75):
    L, cx = x1 - x0, (x0 + x1) / 2
    B(p, (L + 0.06, CW_D + 0.16, 0.35), (cx, 0, 0), "stone_dark", swap=swap)
    B(p, (L, CW_D, CW_H), (cx, 0, 0), "stone", swap=swap)
    B(p, (L + 0.04, CW_D + 0.12, 0.14), (cx, 0, CW_H - 0.14), "stone_light", swap=swap)
    for mx in merlons:
        B(p, (mw, CW_D, CW_T), (mx, 0, CW_H), "stone", top="stone_light", swap=swap)
    _bricks(p, x0, x1, 0.45, CW_H - 0.3, CW_D / 2, rnd, n_bricks, avoid=avoid, swap=swap)


def _castle_tufts(p, rnd, xs):
    _tufts(p, [(x, -CW_D / 2 - 0.12) for x in xs], rnd)


def castle_wall():
    p = Part("M_CastleWall")
    rnd = random.Random(21)
    _castle_run(p, -1.5, 1.5, rnd, (-1.125, 0, 1.125), avoid=[(-0.5, 0.5, 1.6, 3.4)], n_bricks=14)
    _banner(p, 0, -CW_D / 2, 3.15)
    _castle_tufts(p, rnd, (-1.2, 1.25))
    return p.finish()


def castle_wall_corner():
    p = Part("M_CastleWallCorner")
    rnd = random.Random(22)
    _castle_run(p, -0.6, 1.5, rnd, (1.2,), n_bricks=6, mw=0.6)
    _castle_run(p, 0.6, 1.5, rnd, (1.2,), swap=True, n_bricks=4, mw=0.6)
    p.box((1.5, 1.5, CW_H + 0.2), (0, 0, 0), "stone")                        # köşe kulesi gövdesi
    p.box((1.6, 1.6, 0.4), (0, 0, 0), "stone_dark")
    p.box((1.62, 1.62, 0.14), (0, 0, CW_H + 0.06), "stone_light")
    for x, y in ((-0.5, -0.5), (0.5, -0.5), (-0.5, 0.5)):
        p.box((0.5, 0.5, 0.7), (x, y, CW_H + 0.2), "stone", top="stone_light")
    _banner(p, 0, -0.75, 3.15, w=0.6, h=1.15)
    _castle_tufts(p, rnd, (-0.4, 1.3))
    _tufts(p, [(-0.75, 1.2)], rnd)
    return p.finish()


def castle_wall_hole():
    p = Part("M_CastleWallHole")
    rnd = random.Random(23)
    _castle_run(p, -1.5, 1.5, rnd, (-1.125, 0, 1.125),
                avoid=[(-0.5, 0.5, 1.9, 3.4), (-0.5, 0.5, 0, 0.8)], n_bricks=14)
    _banner(p, 0, -CW_D / 2, 3.15, h=1.05)
    _mouse_hole(p, 0, -CW_D / 2 - 0.08, rim="stone_light", w=0.86, h=0.85, rnd=rnd)
    _castle_tufts(p, rnd, (-1.2, 1.25))
    return p.finish()


# ---------- parmaklıklı kapı (portcullis) ----------

GATE_W, GATE_SPRING = 2.2, 1.9          # açıklık genişliği, kemer başlangıcı (tepe 3.0)


def _grid(p, w, h, z0, y=0.0):
    n = 7
    for i in range(n):
        x = -w / 2 + 0.12 + i * (w - 0.24) / (n - 1)
        p.box((0.09, 0.09, h - 0.2), (x, y, z0 + 0.2), "iron")
        p.cyl(0.07, 0.25, (x, y, z0 + 0.25), "iron_light", segments=4, radius_top=0.0,
              rot=Matrix.Rotation(math.pi, 3, "X"))
    z = z0 + 0.45
    while z < z0 + h - 0.1:
        p.box((w, 0.13, 0.09), (0, y, z), "iron")
        z += 0.55


def portcullis_grid():
    """Düşen parmaklık; pivot alt orta (kapıda kapalıyken y=0, z=0)."""
    p = Part("M_PortcullisGrid")
    _grid(p, GATE_W + 0.1, 3.2, 0.0)
    return p.finish()


def _gate_arch(p, rnd):
    hw = GATE_W / 2
    for sx in (-1, 1):
        x = sx * (hw + 0.4)
        p.box((0.9, CW_D + 0.16, 0.4), (x, 0, 0), "stone_dark")
        p.box((0.8, CW_D, 3.9), (x, 0, 0), "stone")
        p.box((0.84, CW_D + 0.06, 0.3), (x, 0, 0.4), "stone_light")
        _banner(p, x, -CW_D / 2, 2.85, w=0.5, h=0.95)
    p.box((GATE_W + 1.6, CW_D, 0.9), (0, 0, 3.0), "stone")                  # lento
    p.box((GATE_W + 1.64, CW_D + 0.12, 0.14), (0, 0, 3.76), "stone_light")
    _arch(p, 0, 0, GATE_SPRING, hw, 0.45, CW_D + 0.02, "stone_light", n=7, alt="stone")
    for mx in (-1.5, 0, 1.5):
        p.box((0.8, CW_D, 0.6), (mx, 0, 3.9), "stone", top="stone_light")
    p.box((GATE_W + 0.1, 0.5, 0.06), (0, 0, 0), "stone_dark")               # eşik


def gate_open():
    p = Part("M_GateOpen")
    _gate_arch(p, random.Random(31))
    _grid(p, GATE_W + 0.1, 1.4, 2.05)                                        # yukarıda: sadece alt kısmı görünür
    return p.finish()


def gate_closed():
    p = Part("M_GateClosed")
    _gate_arch(p, random.Random(32))
    _grid(p, GATE_W + 0.1, 3.2, 0.0)
    return p.finish()


# ---------- köşe kulesi + ana kapı ----------

def corner_tower():
    p = Part("M_CornerTower")
    rnd = random.Random(41)
    seg = 10
    p.cyl(1.6, 0.5, (0, 0, 0), "stone_dark", segments=seg)
    p.cyl(1.45, 4.7, (0, 0, 0.5), "stone", segments=seg, radius_top=1.35)
    p.cyl(1.6, 0.3, (0, 0, 4.9), "stone_light", segments=seg, radius_top=1.65)
    p.cyl(1.55, 0.12, (0, 0, 5.2), "stone_dark", segments=seg)
    for i in range(8):
        a = i * math.tau / 8 + math.pi / 8
        p.box((0.75, 0.45, 0.65), (1.35 * math.cos(a), 1.35 * math.sin(a), 5.25), "stone", top="stone_light",
              rot_z=a + math.pi / 2)
    for i in range(12):                                                      # gövdede taş blokları
        a = rnd.uniform(-math.pi, math.pi)
        z = 0.7 + rnd.randrange(9) * 0.45
        rr = 1.45 - (z - 0.5) / 4.7 * 0.1 + 0.02
        p.box((0.45, 0.1, 0.3), (rr * math.cos(a), rr * math.sin(a), z),
              rnd.choice(("stone_light", "stone_dark", "stone_light")), rot_z=a + math.pi / 2)
    for x, z in ((-0.5, 2.6), (0.5, 2.6), (0, 4.0)):                          # ok delikleri
        y = -math.sqrt(1.42 ** 2 - x * x)
        p.box((0.3, 0.12, 0.8), (x, y - 0.02, z - 0.08), "stone_light", rot_z=math.atan2(x, -y) * -1)
        p.box((0.13, 0.14, 0.62), (x, y - 0.04, z), "dark", rot_z=math.atan2(x, -y) * -1)
    _flag(p, 0, 0, 5.2, pole=2.0)
    _tufts(p, [(-1.2, -1.0), (1.3, -0.8), (0.3, -1.6)], rnd)
    return p.finish()


def main_gate():
    p = Part("M_MainGate")
    rnd = random.Random(42)
    D = 1.8
    # orta kapı yapısı
    p.box((5.0, D + 0.16, 0.4), (0, 0, 0), "stone_dark")
    p.box((4.9, D, 4.3), (0, 0, 0), "stone")
    p.box((4.96, D + 0.12, 0.14), (0, 0, 4.16), "stone_light")
    for mx in (-1.9, -0.65, 0.65, 1.9):
        p.box((0.75, D, 0.6), (mx, 0, 4.3), "stone", top="stone_light")
    fy = -D / 2
    _bricks(p, -2.4, 2.4, 0.5, 4.0, D / 2, rnd, 12, avoid=[(-1.6, 1.6, 0, 3.3), (-2.45, -1.55, 1.3, 3.8),
                                                         (1.55, 2.45, 1.3, 3.8)])
    # kemer + ahşap kapı kanatları
    r, zs = 1.2, 1.6
    _arch(p, 0, fy - 0.02, zs, r, 0.38, 0.2, "stone_light", n=7, alt="stone")
    for sx in (-1, 1):
        p.box((0.36, 0.2, zs), (sx * (r + 0.19), fy - 0.02, 0), "stone_light")
    p.box((2 * r, 0.12, zs), (0, fy - 0.02, 0), "wood")
    p.cyl(r, 0.12, (0, fy + 0.04, zs), "wood", segments=12, rot=RX90)
    for x in (-0.8, -0.4, 0.4, 0.8):                                         # tahta aralıkları
        h = zs + math.sqrt(max(r * r - x * x, 0)) - 0.05
        p.box((0.05, 0.14, h), (x, fy - 0.04, 0), "wood_dark")
    p.box((0.1, 0.16, zs + r - 0.04), (0, fy - 0.05, 0), "wood_dark")       # kanat ortası
    for z in (0.45, 1.75):                                                   # demir kuşaklar
        hw = r if z < zs else math.sqrt(r * r - (z - zs + 0.06) ** 2)
        p.box((2 * hw - 0.1, 0.16, 0.12), (0, fy - 0.06, z), "iron")
    for sx in (-1, 1):                                                       # halka tokmaklar
        p.cyl(0.13, 0.05, (sx * 0.25, fy - 0.08, 1.0), "iron_light", segments=8, rot=RX90)
        p.cyl(0.07, 0.07, (sx * 0.25, fy - 0.08, 1.06), "iron", segments=6, rot=RX90)
    for sx in (-1, 1):
        _banner(p, sx * 1.95, fy, 3.6, w=0.6, h=1.3)
    # iki yan kule
    for sx in (-1, 1):
        x = sx * 3.45
        p.box((2.2, 2.4, 0.45), (x, 0, 0), "stone_dark")
        p.box((2.0, 2.2, 5.6), (x, 0, 0), "stone")
        p.box((2.16, 2.36, 0.16), (x, 0, 5.44), "stone_light")
        for dx, dy in ((-0.7, -0.8), (0.7, -0.8), (-0.7, 0.8), (0.7, 0.8)):
            p.box((0.6, 0.6, 0.65), (x + dx, dy, 5.6), "stone", top="stone_light")
        for z in (2.2, 3.9):
            p.box((0.3, 0.1, 0.8), (x, -1.12, z - 0.08), "stone_light")
            p.box((0.13, 0.1, 0.62), (x, -1.15, z), "dark")
        _bricks(p, x - 0.95, x + 0.95, 0.5, 5.2, 1.1, rnd, 6, avoid=[(x - 0.3, x + 0.3, 1.8, 4.9)])
        _flag(p, x, 0, 5.6, pole=1.9)
    _tufts(p, [(-2.2, -1.1), (2.3, -1.15), (-4.5, -1.3), (4.4, -1.3)], rnd)
    return p.finish()


# ---------- tehlikeler ----------

def murder_hole():
    """Kızgın yağ dökülen duvar; yer uyarı halkası ayrı model (M_OilWarning)."""
    p = Part("M_MurderHole")
    rnd = random.Random(51)
    _castle_run(p, -1.5, 1.5, rnd, (-1.125, 0, 1.125), avoid=[(-0.6, 0.6, 1.4, 3.1)], n_bricks=12)
    fy = -CW_D / 2
    p.box((1.0, 0.14, 0.18), (0, fy - 0.04, 1.95), "stone_light")           # açıklık çerçevesi
    p.box((1.0, 0.14, 0.18), (0, fy - 0.04, 2.75), "stone_light")
    for sx in (-1, 1):
        p.box((0.18, 0.14, 0.62), (sx * 0.41, fy - 0.04, 2.13), "stone_light")
    p.box((0.64, 0.1, 0.62), (0, fy, 2.13), "dark")
    p.box((0.5, 0.4, 0.1), (0, fy - 0.2, 2.13), "iron")                     # oluk
    p.box((0.4, 0.3, 0.12), (0, fy - 0.22, 2.2), "fire")                    # taşan yağ
    p.box((0.36, 0.12, 0.3), (0, fy - 0.36, 1.92), "fire")                   # oluk ağzından dökülen
    p.cyl(0.18, 1.6, (0, fy - 0.38, 0.4), "fire", segments=6, radius_top=0.14)   # akış
    p.cyl(0.09, 1.3, (0, fy - 0.44, 0.55), "gold", segments=6, radius_top=0.07)  # parlak çekirdek
    for z, dx in ((1.2, 0.27), (0.75, -0.26), (1.55, -0.25), (0.45, 0.24)):     # damlalar
        p.box((0.1, 0.1, 0.15), (dx, fy - 0.38, z), "fire")
    p.cyl(0.55, 0.05, (0, fy - 0.4, 0), "fire", segments=8)                  # yerdeki yağ birikintisi
    p.cyl(0.3, 0.07, (0, fy - 0.4, 0), "gold", segments=8)
    p.cyl(0.24, 0.3, (0, fy - 0.4, 0.05), "fire", segments=6, radius_top=0.1)  # sıçrama
    _castle_tufts(p, rnd, (-1.2, 1.2))
    return p.finish()


def oil_warning():
    """Yere düz parlayan turuncu uyarı diski (çap 2.4 m), yağın düşeceği yer."""
    p = Part("M_OilWarning")
    seg = 16
    p.cyl(1.2, 0.02, (0, 0, 0), "fire", segments=seg)
    p.cyl(0.95, 0.03, (0, 0, 0), "gold", segments=seg)
    p.cyl(0.75, 0.04, (0, 0, 0), "fire", segments=seg)
    p.cyl(0.4, 0.05, (0, 0, 0), "window", segments=seg)
    return p.finish()


def arrow_slit():
    p = Part("M_ArrowSlit")
    rnd = random.Random(52)
    _castle_run(p, -1.5, 1.5, rnd, (-1.125, 0, 1.125), avoid=[(-0.5, 0.5, 1.0, 3.2)], n_bricks=14)
    fy = -CW_D / 2
    p.box((0.46, 0.12, 1.6), (0, fy - 0.03, 1.15), "stone_light")
    p.box((0.7, 0.12, 0.14), (0, fy - 0.03, 1.15), "stone_dark")            # pervaz
    p.box((0.14, 0.12, 1.2), (0, fy - 0.06, 1.35), "dark")
    p.box((0.38, 0.12, 0.12), (0, fy - 0.06, 2.0), "dark")                  # haç biçimli mazgal
    _castle_tufts(p, rnd, (-1.1, 1.25))
    return p.finish()


def rope_bridge():
    """2 m geniş (X), 5 m uzun (Y) halatlı tahta köprü; uçlar zemin hizasında taş ayaklarda."""
    p = Part("M_RopeBridge")
    rnd = random.Random(53)
    for sy in (-1, 1):
        p.box((2.5, 0.9, 0.3), (0, sy * 2.15, 0), "stone", top="stone_light")
        p.box((0.6, 0.5, 0.2), (sy * 0.9, sy * 2.35, 0.3), "stone_light")
    for sx in (-0.8, 0.8):
        p.box((0.16, 5.0, 0.18), (sx, 0, 0.18), "wood_dark")                  # kirişler
    for i in range(10):
        y = -2.25 + i * 0.5
        p.box((2.0, 0.44, 0.1), (rnd.uniform(-0.04, 0.04), y, 0.36), "wood" if i % 2 else "wood_light",
              rot_z=rnd.uniform(-0.03, 0.03))
    for sx in (-1.05, 1.05):
        for sy in (-2.3, 2.3):
            p.box((0.22, 0.22, 1.25), (sx, sy, 0.3), "wood")
            p.box((0.28, 0.28, 0.12), (sx, sy, 1.3), "straw_dark")           # halat sargısı
        top_a, mid, top_b = (sx, -2.3, 1.38), (sx, 0, 1.0), (sx, 2.3, 1.38)
        _beam(p, top_a, (sx, -1.15, 1.12), 0.07, "straw")
        _beam(p, (sx, -1.15, 1.12), mid, 0.07, "straw")
        _beam(p, mid, (sx, 1.15, 1.12), 0.07, "straw")
        _beam(p, (sx, 1.15, 1.12), top_b, 0.07, "straw")
        for y, z in ((-1.15, 1.12), (0, 1.0), (1.15, 1.12)):
            p.box((0.04, 0.04, z - 0.46), (sx, y, 0.46), "straw_dark")
    return p.finish()


def moat_edge():
    """Hendek kenarı 3 x 3 m: arkada (+Y) taş rıhtım, önde (−Y) su."""
    p = Part("M_MoatEdge")
    rnd = random.Random(54)
    p.box((3.0, 2.1, 0.06), (0, -0.45, 0), "dark")
    p.box((3.0, 2.1, 0.08), (0, -0.45, 0.02), "water")
    for x, y, w in ((-0.9, -0.9, 0.5), (0.4, -0.4, 0.6), (0.9, -1.2, 0.4), (-0.3, -1.3, 0.45), (-1.1, -0.2, 0.35)):
        p.box((w, 0.06, 0.02), (x, y, 0.1), "white")                        # dalga çizgileri
        p.box((w * 0.5, 0.06, 0.02), (x + w * 0.4, y - 0.08, 0.1), "white")
    p.box((3.0, 0.9, 0.35), (0, 1.05, 0), "stone_dark")
    x = -1.5
    while x < 1.45:
        w = min(rnd.uniform(0.6, 0.9), 1.5 - x)
        p.box((w - 0.05, 0.95, 0.2), (x + w / 2, 1.02, 0.35), rnd.choice(("stone", "stone_light")))
        x += w
    p.box((0.55, 0.45, 0.2), (-0.8, 0.35, 0.0), "stone_light")              # suya düşmüş taşlar
    p.box((0.4, 0.35, 0.15), (-0.35, 0.3, 0.0), "stone")
    p.box((0.5, 0.18, 0.08), (0.9, 0.62, 0.35), "moss")
    return p.finish()


# ---------- zemin karoları 4 x 4 m, 0.1 m ----------

def _sails(p, hub):
    """Dört kanat XZ düzleminde, eksen Y (ön −Y); hub = göbek merkezi."""
    hx, hy, hz = hub
    p.cyl(0.32, 0.4, (hx, hy + 0.2, hz), "wood", segments=8, rot=RX90)
    p.cyl(0.16, 0.12, (hx, hy - 0.18, hz), "gold", segments=8, rot=RX90)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        d = Vector((math.cos(a), 0, math.sin(a)))
        t = Vector((-math.sin(a), 0, math.cos(a)))
        h = Vector(hub)
        _obox(p, (3.0, 0.12, 0.13), h + d * 1.6 + Vector((0, 0.02, 0)), "wood_dark", _ry(-a))   # direk
        c = h + d * 1.8 + t * 0.42 + Vector((0, -0.05, 0))
        _obox(p, (2.2, 0.04, 0.72), c, "cream", _ry(-a))                                       # yelken
        _obox(p, (2.3, 0.07, 0.07), h + d * 1.8 + t * 0.8 + Vector((0, -0.06, 0)), "wood", _ry(-a))
        for r in (0.72, 1.45, 2.15, 2.88):
            _obox(p, (0.07, 0.07, 0.82), h + d * r + t * 0.42 + Vector((0, -0.07, 0)), "wood", _ry(-a))


def windmill():
    p = Part("M_Windmill")
    face = rz(math.pi / 8)
    p.cyl(1.75, 0.7, (0, 0, 0), "stone_dark", segments=8, top="stone", rot=face)
    p.cyl(1.5, 3.8, (0, 0, 0.7), "plaster", segments=8, radius_top=1.2, rot=face)
    p.cyl(1.3, 0.2, (0, 0, 4.5), "wood_dark", segments=8, rot=face)
    p.cyl(1.85, 0.18, (0, 0, 4.7), "roof_dark", segments=8, rot=face)
    p.cyl(1.75, 2.0, (0, 0, 4.88), "roof", segments=8, radius_top=0.0, rot=face)
    p.cyl(0.12, 0.3, (0, 0, 6.85), "gold", segments=6)
    # kapı (kemerli) + pencere
    fy = -1.62
    p.box((1.0, 0.3, 1.1), (0, fy + 0.05, 0), "stone_light")
    p.cyl(0.5, 0.3, (0, fy + 0.2, 1.1), "stone_light", segments=8, rot=RX90)
    p.box((0.7, 0.3, 1.1), (0, fy - 0.0, 0), "wood")
    p.cyl(0.35, 0.3, (0, fy + 0.16, 1.1), "wood", segments=8, rot=RX90)
    p.box((0.05, 0.32, 1.4), (0, fy - 0.01, 0), "wood_dark")
    p.frame(0.55, 0.55, (0, -1.32, 2.55))
    p.box((0.75, 0.2, 0.1), (0, -1.36, 2.4), "wood_dark")
    # aks + kanatlar
    p.cyl(0.14, 1.0, (0, -1.0, 5.3), "wood_dark", segments=6, rot=RX90)
    _sails(p, (0, -2.05, 5.3))
    for x, y in ((-1.5, -1.3), (1.4, -1.4)):
        p.box((0.45, 0.35, 0.35), (x, y, 0), "leaf")
    return p.finish()


def windmill_sails():
    """Sadece kanat çarkı: pivot göbek merkezinde, dönme ekseni Y."""
    p = Part("M_WindmillSails")
    _sails(p, (0, 0, 0))
    return p.finish()


def fence_gate():
    """Köy çitinin kapısı (6 m, iki parça boyu): iki kalın direk + bayrak, ortada kapalı çift kanat tahta kapı."""
    p = Part("M_FenceGate")
    for sx in (-1, 1):
        x = sx * 2.75
        p.box((0.55, 0.55, 2.2), (x, 0, 0), "wood_dark")
        p.cyl(0.36, 0.45, (x, 0, 2.2), "wood", segments=4, radius_top=0.0, rot=rz(math.radians(45)))
        for z in (0.5, 1.5):
            p.box((0.62, 0.62, 0.16), (x, 0, z), "iron")
        _flag(p, x, 0, 2.55, pole=1.3)
    for sx in (-1, 1):                                                       # kanatlar
        cx = sx * 1.25
        p.box((2.4, 0.18, 1.6), (cx, 0, 0.05), "wood")
        for k in range(4):
            p.box((0.08, 0.2, 1.6), (cx - 0.9 + k * 0.6, -0.01, 0.05), "wood_dark")
        p.box((2.4, 0.22, 0.16), (cx, -0.02, 0.35), "iron")
        p.box((2.4, 0.22, 0.16), (cx, -0.02, 1.25), "iron")
    p.box((5.0, 0.22, 0.2), (0, -0.02, 1.85), "wood_dark")                   # üst kiriş
    p.box((1.3, 0.12, 0.55), (0, -0.14, 1.95), "blue")                       # tabela
    _crown(p, 0, -0.22, 2.05, s=0.8)
    return p.finish()


BUILDERS = {
    "fence_gate": fence_gate,
    "fence": fence, "fence_corner": fence_corner, "fence_hole": fence_hole,
    "stonewall": stonewall, "stonewall_corner": stonewall_corner, "stonewall_hole": stonewall_hole,
    "castle_wall": castle_wall, "castle_wall_corner": castle_wall_corner, "castle_wall_hole": castle_wall_hole,
    "portcullis_grid": portcullis_grid, "gate_open": gate_open, "gate_closed": gate_closed,
    "corner_tower": corner_tower, "main_gate": main_gate,
    "murder_hole": murder_hole, "oil_warning": oil_warning, "arrow_slit": arrow_slit,
    "rope_bridge": rope_bridge, "moat_edge": moat_edge,
    "windmill": windmill, "windmill_sails": windmill_sails,
}
