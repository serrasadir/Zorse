"""Ortaçağ kiti: sokak eşyaları ve yerdeki yiyecekler (Sheet 2). Taban merkezi orijinde, ön yüz −Y."""
import math

import bmesh
from mathutils import Matrix, Vector

from build_medieval import Part, rz

PI = math.pi


def rx(a):
    return Matrix.Rotation(a, 3, "X")


def ry(a):
    return Matrix.Rotation(a, 3, "Y")


AX_X = ry(PI / 2)   # silindir ekseni Z -> X
AX_Y = rx(PI / 2)   # silindir ekseni Z -> Y


def _xf(p, faces, rot, center):
    verts = list({v for f in faces for v in f.verts})
    if rot is not None:
        bmesh.ops.rotate(p.bm, verts=verts, cent=(0, 0, 0), matrix=rot)
    bmesh.ops.translate(p.bm, verts=verts, vec=Vector(center))
    return faces


def tbox(p, size, center, color, rot, top=None):
    """Merkezden döndürülmüş kutu (center = kutunun merkezi)."""
    return _xf(p, p.box(size, (0, 0, -size[2] / 2), color, top=top), rot, center)


def tcyl(p, r, h, center, color, rot, segments=8, radius_top=None, top=None):
    """Merkezden döndürülmüş silindir (center = silindirin ortası)."""
    return _xf(p, p.cyl(r, h, (0, 0, -h / 2), color, segments, top=top, radius_top=radius_top), rot, center)


def blob(p, r, c, color, seg=7, spin=0.0):
    """Yaprak/top kümesi: iki kesik koniden yuvarlakça yığın, c = taban merkezi."""
    x, y, z = c
    p.cyl(r * 0.7, r * 0.55, (x, y, z), color, seg, radius_top=r, rot=rz(spin))
    p.cyl(r, r * 0.7, (x, y, z + r * 0.55), color, seg, radius_top=r * 0.45, rot=rz(spin))


def ring(p, radius, width, thick, center, color, n=10, axis=None):
    """XZ düzleminde (ekseni Y) parçalı çember; axis ile ayrıca döndürülür."""
    A = axis if axis is not None else Matrix.Identity(3)
    chord = 2 * radius * math.sin(PI / n) * 1.08
    for i in range(n):
        a = 2 * PI * i / n
        pos = A @ Vector((radius * math.cos(a), 0, radius * math.sin(a)))
        tbox(p, (chord, width, thick), Vector(center) + pos, color, A @ ry(-a + PI / 2))


def spoked_wheel(p, R, center, axis=None, n=10, spokes=6, rim="wood_dark", spoke="wood", hub="iron", width=0.12):
    A = axis if axis is not None else Matrix.Identity(3)
    ring(p, R - 0.06, width, 0.12, center, rim, n=n, axis=A)
    for i in range(spokes // 2):
        a = PI * i / (spokes // 2)
        tbox(p, (R * 2 - 0.12, width * 0.6, 0.06), center, spoke, A @ ry(a))
    tcyl(p, R * 0.2, width * 1.6, center, hub, A @ AX_Y, segments=8)


def simple_wheel(p, R, center, axis=None):
    """Ucuz araba tekerleği: disk + çapraz parmaklar + göbek."""
    A = axis if axis is not None else Matrix.Identity(3)
    tcyl(p, R, 0.1, center, "wood_dark", A @ AX_Y, segments=8)
    for a in (PI / 4, -PI / 4):
        tbox(p, (R * 1.75, 0.14, 0.08), center, "wood_light", A @ ry(a))
    tcyl(p, R * 0.22, 0.2, center, "iron", A @ AX_Y, segments=6)


# ===================== TINY (avuç içi) =====================

def bread_crust():
    p = Part("M_BreadCrust")
    for i in range(4):
        a = -0.95 + i * 0.63
        x, y = 0.13 * math.sin(a), -0.13 * math.cos(a) + 0.06
        p.box((0.11, 0.075, 0.065), (x, y, 0), "wood", top="wood_light", rot_z=a)
        p.box((0.1, 0.03, 0.045), (x * 0.78, (y - 0.06) * 0.78 + 0.06, 0), "cream", rot_z=a)
    return p.finish()


def drumstick():
    p = Part("M_Drumstick")
    tcyl(p, 0.05, 0.08, (-0.13, 0, 0.075), "wood", AX_X, segments=8, radius_top=0.085)
    tcyl(p, 0.085, 0.1, (-0.04, 0, 0.075), "wood_light", AX_X, segments=8, radius_top=0.035)
    tcyl(p, 0.018, 0.11, (0.06, 0, 0.06), "cream", AX_X, segments=6)
    for dy in (-0.022, 0.022):
        tcyl(p, 0.026, 0.045, (0.12, dy, 0.06), "white", AX_X, segments=6)
    return p.finish()


def apple_core():
    p = Part("M_AppleCore")
    p.cyl(0.07, 0.04, (0, 0, 0), "red", 8, radius_top=0.06)
    p.cyl(0.035, 0.08, (0, 0, 0.04), "cream", 8, radius_top=0.04)
    p.cyl(0.06, 0.045, (0, 0, 0.12), "red", 8, radius_top=0.065, top="cream")
    p.box((0.015, 0.015, 0.05), (0, 0, 0.165), "wood_dark")
    p.box((0.012, 0.025, 0.012), (0, 0.025, 0.07), "dark")
    return p.finish()


def cheese_wedge():
    p = Part("M_CheeseWedge")
    p.cyl(0.16, 0.13, (0, 0, 0), "gold", 3, top="straw", rot=rz(PI / 2))
    for x, z in ((-0.06, 0.06), (0.05, 0.08), (0.01, 0.02)):
        p.box((0.045, 0.03, 0.04), (x, -0.07, z), "straw_dark")
    p.box((0.05, 0.05, 0.01), (0.0, 0.02, 0.13), "straw_dark")
    return p.finish()


def wooden_mug():
    p = Part("M_WoodenMug")
    p.cyl(0.075, 0.18, (0, 0, 0), "wood", 8, top="cream")
    for z in (0.02, 0.14):
        p.cyl(0.082, 0.025, (0, 0, z), "iron", 8)
    p.box((0.03, 0.03, 0.12), (0.12, 0, 0.03), "wood_dark")
    for z in (0.03, 0.12):
        p.box((0.06, 0.03, 0.03), (0.095, 0, z), "wood_dark")
    return p.finish()


def candle_stub():
    p = Part("M_CandleStub")
    p.cyl(0.11, 0.025, (0, 0, 0), "cream", 7)
    p.cyl(0.05, 0.12, (0, 0, 0.025), "cream", 8, top="plaster")
    p.box((0.03, 0.025, 0.05), (0.045, -0.02, 0.06), "plaster")
    p.box((0.01, 0.01, 0.03), (0, 0, 0.145), "dark")
    p.cyl(0.018, 0.05, (0, 0, 0.165), "fire", 5, radius_top=0.002)
    return p.finish()


def small_bone():
    p = Part("M_SmallBone")
    tcyl(p, 0.022, 0.2, (0, 0, 0.03), "cream", AX_X, segments=6)
    for x in (-0.11, 0.11):
        for dy in (-0.028, 0.028):
            p.cyl(0.03, 0.055, (x, dy, 0), "white", 6)
    return p.finish()


def turnip():
    p = Part("M_Turnip")
    p.cyl(0.012, 0.035, (0, 0, 0), "cream", 5, radius_top=0.04)
    p.cyl(0.04, 0.08, (0, 0, 0.035), "white", 8, radius_top=0.105)
    p.cyl(0.105, 0.03, (0, 0, 0.115), "roof_dark", 8, radius_top=0.08)
    p.cyl(0.08, 0.02, (0, 0, 0.145), "roof_dark", 8, radius_top=0.035)
    for a in (0.3, 2.4, 4.5):
        tbox(p, (0.07, 0.02, 0.13), (0.04 * math.cos(a), 0.04 * math.sin(a), 0.22), "leaf",
             rz(a) @ ry(0.45))
    p.box((0.03, 0.03, 0.05), (0, 0, 0.16), "leaf_dark")
    return p.finish()


# ===================== LITTLE (diz boyu) =====================

def chicken():
    p = Part("M_Chicken")
    for x in (-0.06, 0.06):
        p.box((0.025, 0.025, 0.12), (x, 0.0, 0), "gold")
        p.box((0.06, 0.08, 0.02), (x, -0.02, 0), "gold")
    p.box((0.28, 0.34, 0.22), (0, 0.02, 0.11), "white")
    p.box((0.24, 0.12, 0.16), (0, -0.16, 0.14), "white")
    for x in (-0.15, 0.15):
        p.box((0.04, 0.2, 0.13), (x, 0.04, 0.15), "cream")
    tbox(p, (0.18, 0.12, 0.16), (0, 0.2, 0.31), "cream", rx(-0.7))
    p.box((0.17, 0.16, 0.16), (0, -0.17, 0.3), "white")
    p.box((0.05, 0.12, 0.07), (0, -0.16, 0.46), "red")
    p.box((0.04, 0.05, 0.05), (0, -0.25, 0.37), "gold")
    p.box((0.035, 0.03, 0.05), (0, -0.24, 0.32), "red")
    for x in (-0.087, 0.087):
        p.box((0.01, 0.03, 0.03), (x, -0.2, 0.38), "dark")
    return p.finish()


def wooden_bucket():
    p = Part("M_WoodenBucket")
    p.cyl(0.18, 0.36, (0, 0, 0), "wood", 10, radius_top=0.22, top="water")
    for z, r in ((0.05, 0.19), (0.27, 0.215)):
        p.cyl(r + 0.012, 0.035, (0, 0, z), "iron", 10)
    for x in (-0.225, 0.225):
        p.box((0.03, 0.04, 0.2), (x, 0, 0.3), "iron")
    p.box((0.48, 0.04, 0.03), (0, 0, 0.5), "iron")
    return p.finish()


def stool():
    p = Part("M_Stool")
    p.cyl(0.22, 0.07, (0, 0, 0.38), "wood_light", 10, top="wood_light")
    for i in range(3):
        a = 2 * PI * i / 3 + PI / 2
        x, y = 0.13 * math.cos(a), 0.13 * math.sin(a)
        tbox(p, (0.06, 0.06, 0.4), (x, y, 0.19), "wood", rz(a) @ ry(0.18) @ rz(-a))
    return p.finish()


def small_crate():
    p = Part("M_SmallCrate")
    s = 0.5
    p.box((s, s, s), (0, 0, 0), "wood", top="wood_light")
    # kenar çıtaları
    for x in (-1, 1):
        for y in (-1, 1):
            p.box((0.06, 0.06, s), (x * (s / 2 - 0.02), y * (s / 2 - 0.02), 0), "wood_dark")
    for z in (0, s - 0.06):
        p.box((s + 0.02, 0.06, 0.06), (0, -s / 2 + 0.02, z), "wood_dark")
        p.box((0.06, s + 0.02, 0.06), (-s / 2 + 0.02, 0, z), "wood_dark")
        p.box((0.06, s + 0.02, 0.06), (s / 2 - 0.02, 0, z), "wood_dark")
    # ön ve yan çapraz
    d = s * 1.25
    tbox(p, (d * 0.92, 0.04, 0.06), (0, -s / 2 - 0.005, s / 2), "wood_dark", ry(PI / 4))
    tbox(p, (d * 0.92, 0.04, 0.06), (0, -s / 2 - 0.005, s / 2), "wood_dark", ry(-PI / 4))
    tbox(p, (0.04, d * 0.92, 0.06), (-s / 2 - 0.005, 0, s / 2), "wood_dark", rx(PI / 4))
    tbox(p, (0.04, d * 0.92, 0.06), (-s / 2 - 0.005, 0, s / 2), "wood_dark", rx(-PI / 4))
    return p.finish()


def flower_pot():
    p = Part("M_FlowerPot")
    p.cyl(0.14, 0.26, (0, 0, 0), "roof", 8, radius_top=0.19, top="dirt")
    p.cyl(0.22, 0.07, (0, 0, 0.22), "roof_dark", 8, top="dirt")
    for a in (0, 1.6, 3.2, 4.7):
        tbox(p, (0.18, 0.08, 0.03), (0.08 * math.cos(a), 0.08 * math.sin(a), 0.33), "leaf", rz(a) @ ry(-0.4))
    p.box((0.03, 0.03, 0.18), (0, 0, 0.29), "leaf_dark")
    for i in range(5):
        a = 2 * PI * i / 5
        p.box((0.08, 0.08, 0.03), (0.06 * math.cos(a), 0.06 * math.sin(a), 0.46), "white", rot_z=a)
    p.cyl(0.04, 0.04, (0, 0, 0.47), "gold", 6)
    return p.finish()


def hay_bundle():
    p = Part("M_HayBundle")
    p.cyl(0.24, 0.24, (0, 0, 0), "straw", 8, radius_top=0.19)
    p.cyl(0.2, 0.06, (0, 0, 0.22), "straw_dark", 8)
    p.cyl(0.19, 0.28, (0, 0, 0.28), "straw", 8, radius_top=0.25, top="straw_dark")
    for i in range(5):
        a = 2 * PI * i / 5 + 0.3
        tbox(p, (0.08, 0.04, 0.12), (0.18 * math.cos(a), 0.18 * math.sin(a), 0.58), "straw", rz(a) @ ry(0.4))
    return p.finish()


# ===================== SMALL (insan boyu) =====================

def barrel():
    p = Part("M_Barrel")
    p.cyl(0.38, 0.5, (0, 0, 0), "wood", 10, radius_top=0.45)
    p.cyl(0.45, 0.5, (0, 0, 0.5), "wood", 10, radius_top=0.38, top="wood_light")
    for z, r in ((0.1, 0.405), (0.78, 0.405)):
        p.cyl(r + 0.02, 0.1, (0, 0, z), "iron", 10)
    p.cyl(0.47, 0.06, (0, 0, 0.47), "wood_dark", 10)
    p.cyl(0.3, 0.02, (0, 0, 1.0), "wood_dark", 10)
    return p.finish()


def water_well():
    p = Part("M_WaterWell")
    p.cyl(0.75, 0.8, (0, 0, 0), "stone", 10)
    p.cyl(0.82, 0.16, (0, 0, 0.8), "stone_light", 10, top="stone_light")
    p.cyl(0.52, 0.165, (0, 0, 0.8), "dark", 10, top="water")
    for x, y, a in ((0.4, -0.75, 0), (-0.45, -0.73, 0.2), (0.76, 0.1, 1.5), (-0.6, 0.47, 2.4)):
        p.box((0.3, 0.06, 0.18), (x, y, 0.3), "stone_dark", rot_z=a)
    for x in (-0.7, 0.7):
        p.box((0.14, 0.14, 1.65), (x, 0, 0.8), "wood")
    tcyl(p, 0.05, 1.5, (0, 0, 2.0), "wood_dark", AX_X, segments=6)
    p.box((0.03, 0.03, 0.55), (0, 0, 1.45), "straw_dark")
    p.cyl(0.13, 0.2, (0, 0, 1.25), "wood", 8, radius_top=0.15, top="water")
    p.cyl(0.15, 0.04, (0, 0, 1.38), "iron", 8)
    tbox(p, (0.08, 0.3, 0.08), (0.85, -0.12, 2.0), "wood_dark", None)
    p.roof(1.7, 1.2, 0.75, (0, 0, 2.35), "roof", overhang=0.22, gable="wood", rows=3, edge="roof_dark")
    p.box((1.6, 0.12, 0.12), (0, 0, 2.3), "wood_dark")
    return p.finish()


def market_stall():
    p = Part("M_MarketStall")
    w, d = 2.0, 1.0
    p.box((w, d, 0.8), (0, 0, 0), "wood", top="wood_light")
    p.box((w + 0.06, 0.08, 0.12), (0, -d / 2 - 0.02, 0.7), "wood_dark")
    p.box((w + 0.06, 0.08, 0.1), (0, -d / 2 - 0.02, 0.05), "wood_dark")
    for x in (-0.5, 0.5):
        p.box((0.08, 0.06, 0.65), (x, -d / 2 - 0.02, 0.1), "wood_dark")
    # sandıklarda meyve
    for x, col in ((-0.6, "red"), (0.05, "leaf"), (0.65, "red")):
        p.box((0.55, 0.5, 0.18), (x, -0.15, 0.8), "wood_dark", top="wood")
        for dx in (-0.12, 0.12):
            for dy in (-0.1, 0.1):
                p.cyl(0.1, 0.12, (x + dx, -0.15 + dy, 0.95), col, 6, radius_top=0.06)
    # direkler
    for x in (-w / 2 + 0.06, w / 2 - 0.06):
        p.box((0.1, 0.1, 2.25), (x, -d / 2 + 0.06, 0), "wood_dark")
        p.box((0.1, 0.1, 2.55), (x, d / 2 - 0.06, 0), "wood_dark")
    # çizgili tente (arkadan öne eğik)
    slope = 0.38
    n = 7
    sw = (w + 0.3) / n
    L = (d + 0.6) / math.cos(slope)
    for i in range(n):
        x = -(w + 0.3) / 2 + sw * (i + 0.5)
        col = "blue" if i % 2 == 0 else "cream"
        tbox(p, (sw + 0.002, L, 0.08), (x, -0.15, 2.45), col, rx(slope))
        tbox(p, (sw + 0.002, 0.06, 0.24), (x, -d / 2 - 0.47, 2.2), col, None)
    return p.finish()


def cart_wheel():
    p = Part("M_CartWheel")
    spoked_wheel(p, 0.65, (0, 0, 0.65), n=12, spokes=8, rim="wood_dark", spoke="wood", width=0.14)
    return p.finish()


def hay_bale():
    p = Part("M_HayBale")
    p.box((1.3, 0.75, 0.7), (0, 0, 0), "straw", top="straw")
    for x in (-0.35, 0.35):
        p.box((0.08, 0.79, 0.74), (x, 0, -0.02), "wood_dark")
    p.box((1.32, 0.6, 0.04), (0, 0, 0.7), "straw_dark")
    return p.finish()


def weapon_rack():
    p = Part("M_WeaponRack")
    w = 1.5
    p.box((w + 0.1, 0.5, 0.1), (0, 0, 0), "wood_dark")
    for x in (-w / 2, w / 2):
        p.box((0.12, 0.12, 1.1), (x, 0, 0.1), "wood")
        p.box((0.14, 0.14, 0.08), (x, 0, 1.2), "wood_dark")
    for z in (0.35, 0.95):
        p.box((w, 0.1, 0.1), (0, 0, z), "wood")
    for x in (-0.55, -0.3, -0.05, 0.4, 0.6):
        p.box((0.05, 0.05, 1.45), (x, 0.06, 0.1), "wood_dark")
        p.cyl(0.06, 0.22, (x, 0.06, 1.55), "iron_light", 4, radius_top=0.0)
    # kalkan
    tbox(p, (0.55, 0.08, 0.6), (0.2, -0.16, 0.62), "blue", rx(-0.12))
    tbox(p, (0.42, 0.06, 0.18), (0.2, -0.13, 0.27), "blue", rx(-0.12))
    tbox(p, (0.62, 0.06, 0.06), (0.2, -0.15, 0.95), "gold", rx(-0.12))
    tbox(p, (0.08, 0.05, 0.42), (0.2, -0.21, 0.55), "gold", rx(-0.12))
    tbox(p, (0.3, 0.05, 0.08), (0.2, -0.2, 0.6), "gold", rx(-0.12))
    return p.finish()


def scarecrow():
    p = Part("M_Scarecrow")
    p.box((0.1, 0.1, 1.4), (0, 0, 0), "wood_dark")
    p.box((1.2, 0.09, 0.09), (0, 0, 1.25), "wood")
    # tunik
    p.box((0.44, 0.3, 0.6), (0, 0, 0.8), "blue")
    p.box((0.5, 0.32, 0.14), (0, 0, 0.82), "wood_dark")
    for x in (-1, 1):
        p.box((0.36, 0.26, 0.24), (x * 0.36, 0, 1.15), "blue")
        tbox(p, (0.14, 0.1, 0.1), (x * 0.6, 0, 1.24), "straw", rz(0.4 * x))
    p.box((0.18, 0.06, 0.06), (0.05, -0.16, 1.05), "straw_dark")
    for x in (-0.12, 0.12):
        p.box((0.1, 0.1, 0.15), (x, 0, 0.66), "straw")
    # kafa + şapka
    p.box((0.3, 0.28, 0.3), (0, 0, 1.4), "straw_dark")
    for x in (-0.07, 0.07):
        p.box((0.05, 0.02, 0.05), (x, -0.15, 1.56), "dark")
    p.box((0.14, 0.02, 0.03), (0, -0.15, 1.47), "dark")
    p.cyl(0.36, 0.05, (0, 0, 1.68), "straw", 10)
    p.cyl(0.17, 0.2, (0, 0, 1.72), "straw", 8, radius_top=0.14, top="straw")
    p.cyl(0.175, 0.05, (0, 0, 1.73), "red", 8)
    return p.finish()


def training_dummy():
    p = Part("M_TrainingDummy")
    p.box((0.7, 0.7, 0.12), (0, 0, 0), "wood_dark")
    p.box((0.12, 0.12, 0.7), (0, 0, 0.12), "wood")
    p.cyl(0.26, 0.7, (0, 0, 0.75), "plaster", 8, radius_top=0.3)
    p.cyl(0.28, 0.06, (0, 0, 0.7), "straw_dark", 8)
    p.cyl(0.3, 0.06, (0, 0, 1.38), "straw_dark", 8)
    # hedef halkaları (ön)
    tcyl(p, 0.2, 0.04, (0, -0.27, 1.08), "red", AX_Y, segments=10)
    tcyl(p, 0.13, 0.06, (0, -0.27, 1.08), "cream", AX_Y, segments=10)
    tcyl(p, 0.065, 0.08, (0, -0.27, 1.08), "red", AX_Y, segments=8)
    tcyl(p, 0.05, 0.8, (0, 0, 1.25), "wood", AX_X, segments=6)
    p.cyl(0.08, 0.1, (0, 0, 1.44), "wood", 6)
    p.cyl(0.17, 0.16, (0, 0, 1.52), "wood_light", 8, radius_top=0.2)
    p.cyl(0.2, 0.16, (0, 0, 1.68), "wood_light", 8, radius_top=0.1)
    return p.finish()


# ===================== MEDIUM (insandan büyük) =====================

def vegetable_cart():
    p = Part("M_VegetableCart")
    p.box((1.6, 1.0, 0.1), (0, 0, 0.55), "wood_dark")
    p.box((1.6, 0.08, 0.32), (0, -0.48, 0.65), "wood", top="wood_light")
    p.box((1.6, 0.08, 0.32), (0, 0.48, 0.65), "wood", top="wood_light")
    p.box((0.08, 1.0, 0.32), (-0.78, 0, 0.65), "wood", top="wood_light")
    p.box((0.08, 1.0, 0.32), (0.78, 0, 0.65), "wood", top="wood_light")
    for x in (-0.3, 0.3):
        p.box((0.08, 0.1, 0.34), (x, -0.53, 0.63), "wood_dark")
    p.box((0.1, 0.1, 0.55), (-0.7, -0.4, 0), "wood_dark")
    p.box((0.1, 0.1, 0.55), (-0.7, 0.4, 0), "wood_dark")
    for y in (-0.36, 0.36):
        tbox(p, (1.0, 0.07, 0.07), (1.2, y, 0.68), "wood", ry(0.18))
    p.box((0.06, 0.8, 0.06), (1.62, 0, 0.56), "wood_dark")
    for y in (-0.56, 0.56):
        simple_wheel(p, 0.36, (0.35, y, 0.36))
    # yük
    p.box((1.5, 0.9, 0.12), (0, 0, 0.65), "dirt")
    items = [(-0.5, -0.22, "fire"), (-0.15, 0.22, "fire"), (0.45, -0.2, "fire"),
             (-0.5, 0.2, "leaf"), (0.15, -0.2, "leaf"), (0.5, 0.22, "red"), (0.17, 0.2, "red"),
             (-0.17, -0.2, "red")]
    for x, y, c in items:
        r = 0.18 if c != "red" else 0.13
        p.cyl(r, r * 1.1, (x, y, 0.75), c, 7, radius_top=r * 0.6)
        if c == "fire":
            p.box((0.04, 0.04, 0.08), (x, y, 0.75 + r * 1.1), "leaf_dark")
    return p.finish()


def oak_tree():
    p = Part("M_OakTree")
    p.cyl(0.32, 0.25, (0, 0, 0), "wood_dark", 7, radius_top=0.22)
    p.cyl(0.22, 1.7, (0, 0, 0.25), "wood_dark", 7, radius_top=0.16)
    tbox(p, (0.12, 0.12, 0.8), (0.35, 0, 1.6), "wood_dark", ry(0.8))
    tbox(p, (0.12, 0.12, 0.7), (-0.3, 0.1, 1.55), "wood_dark", ry(-0.8))
    blob(p, 1.05, (0, 0, 2.0), "leaf", spin=0.2)
    blob(p, 0.75, (0.85, -0.1, 1.75), "leaf_dark", spin=0.9)
    blob(p, 0.75, (-0.85, 0.15, 1.8), "leaf_dark", spin=0.4)
    blob(p, 0.7, (0.1, -0.7, 1.9), "leaf", spin=1.3)
    blob(p, 0.65, (0.15, 0.6, 2.4), "leaf", spin=0.7)
    blob(p, 0.55, (-0.4, -0.3, 2.75), "grass", spin=0.1)
    p.box((0.4, 0.3, 0.2), (0.55, -0.45, 0), "stone", rot_z=0.5)
    p.box((0.22, 0.2, 0.12), (-0.5, -0.35, 0), "stone_light", rot_z=-0.3)
    return p.finish()


def haystack():
    p = Part("M_Haystack")
    tiers = [(1.25, 1.1, 0.6), (1.1, 0.85, 0.6), (0.85, 0.55, 0.55), (0.55, 0.15, 0.5)]
    z = 0.0
    for i, (r0, r1, h) in enumerate(tiers):
        p.cyl(r0, h, (0, 0, z), "straw", 10, radius_top=r1, rot=rz(0.3 * i))
        p.cyl(r0 + 0.04, 0.08, (0, 0, z), "straw_dark", 10, radius_top=r0 - 0.02, rot=rz(0.3 * i))
        z += h * 0.85
    for a in (0.4, -0.4):
        tbox(p, (0.06, 0.06, 0.6), (0.08 * math.sin(a), 0, z + 0.15), "wood_dark", ry(a))
    return p.finish()


def wagon():
    p = Part("M_Wagon")
    L, W = 2.6, 1.3
    p.box((L, W, 0.12), (0, 0, 0.6), "wood_dark")
    p.box((L, W, 0.4), (0, 0, 0.72), "wood", top="wood_light")
    for x in (-0.65, 0, 0.65):
        p.box((0.08, W + 0.04, 0.4), (x, 0, 0.72), "wood_dark")
    p.box((L + 0.04, W + 0.04, 0.06), (0, 0, 1.1), "wood_dark")
    # kanvas kemer (X boyunca)
    R = W / 2 + 0.05
    n = 6
    cz = 1.15
    chord = 2 * R * math.sin(PI / (2 * n)) * 1.06
    for i in range(n):
        a = PI * (i + 0.5) / n
        c = (0, R * math.cos(a), cz + R * math.sin(a) * 1.15)
        tbox(p, (L - 0.3, chord, 0.08), c, "plaster", rx(a + PI / 2))
        for x in (-0.6, 0.6):
            tbox(p, (0.1, chord, 0.1), (x, c[1], c[2]), "wood_light", rx(a + PI / 2))
    # ön/arka alın: koyu açıklık
    for sx in (-1, 1):
        p.box((0.04, W * 0.7, 0.55), (sx * (L / 2 - 0.2), 0, 1.15), "dark")
    # oklar + tekerler
    tbox(p, (1.2, 0.08, 0.08), (L / 2 + 0.55, 0, 0.55), "wood_dark", ry(0.15))
    for x in (-0.85, 0.85):
        for y in (-W / 2 - 0.08, W / 2 + 0.08):
            simple_wheel(p, 0.42, (x, y, 0.42))
    return p.finish()


def stone_fountain():
    p = Part("M_StoneFountain")
    p.cyl(1.35, 0.15, (0, 0, 0), "stone_dark", 8)
    p.cyl(1.25, 0.6, (0, 0, 0.15), "stone_light", 8, top="stone_light")
    p.cyl(1.05, 0.605, (0, 0, 0.15), "water", 8)
    p.cyl(0.25, 0.95, (0, 0, 0.6), "stone", 8)
    p.cyl(0.25, 0.2, (0, 0, 1.15), "stone_light", 8, radius_top=0.62)
    p.cyl(0.62, 0.14, (0, 0, 1.35), "stone_light", 8, top="stone_light")
    p.cyl(0.5, 0.145, (0, 0, 1.35), "water", 8)
    p.cyl(0.13, 0.45, (0, 0, 1.45), "stone", 8)
    p.cyl(0.13, 0.12, (0, 0, 1.9), "stone_light", 8, radius_top=0.3)
    p.cyl(0.3, 0.08, (0, 0, 2.02), "stone_light", 8, top="water")
    p.cyl(0.07, 0.2, (0, 0, 2.08), "water", 6, radius_top=0.02)
    # akan su
    for i in range(4):
        a = PI / 4 + i * PI / 2
        p.box((0.06, 0.06, 0.55), (0.57 * math.cos(a), 0.57 * math.sin(a), 0.85), "water")
    return p.finish()


def torch_pole():
    p = Part("M_TorchPole")
    p.box((0.4, 0.4, 0.15), (0, 0, 0), "stone")
    p.box((0.16, 0.16, 2.6), (0, 0, 0.15), "wood")
    p.box((0.2, 0.2, 0.1), (0, 0, 2.7), "wood_dark")
    p.box((0.7, 0.12, 0.12), (-0.3, 0, 2.45), "wood")
    tbox(p, (0.08, 0.08, 0.45), (-0.15, 0, 2.25), "wood_dark", ry(-0.8))
    # fener
    lx = -0.55
    p.box((0.03, 0.03, 0.12), (lx, 0, 2.33), "iron")
    p.box((0.26, 0.26, 0.05), (lx, 0, 1.9), "iron")
    p.box((0.2, 0.2, 0.3), (lx, 0, 1.95), "window")
    for dx in (-0.11, 0.11):
        for dy in (-0.11, 0.11):
            p.box((0.04, 0.04, 0.3), (lx + dx, dy, 1.95), "iron")
    p.cyl(0.2, 0.14, (lx, 0, 2.25), "iron", 4, radius_top=0.04, rot=rz(PI / 4))
    p.cyl(0.05, 0.12, (lx, 0, 1.97), "fire", 5, radius_top=0.0)
    return p.finish()


def archery_target():
    p = Part("M_ArcheryTarget")
    lean = 0.22
    for x in (-0.45, 0.45):
        tbox(p, (0.1, 0.1, 1.75), (x * 0.9, 0, 0.85), "wood", rx(-lean) @ ry(0.22 * (1 if x < 0 else -1)))
    tbox(p, (0.1, 0.1, 1.2), (0, 0.38, 0.55), "wood_dark", rx(0.6))
    c = Vector((0, -0.12, 1.08))
    rot = rx(-lean) @ AX_Y
    n = Vector((0, -math.cos(lean), math.sin(lean)))  # yaklaşık ön normal
    rings = [(0.62, "straw_dark", 0.12), (0.55, "cream", 0.14), (0.42, "red", 0.16),
             (0.29, "cream", 0.18), (0.16, "red", 0.2)]
    for r, col, h in rings:
        tcyl(p, r, h, c + n * (h / 2 - 0.06), col, rot, segments=12)
    return p.finish()


BUILDERS = {
    # tiny
    "bread_crust": bread_crust, "drumstick": drumstick, "apple_core": apple_core, "cheese_wedge": cheese_wedge,
    "wooden_mug": wooden_mug, "candle_stub": candle_stub, "small_bone": small_bone, "turnip": turnip,
    # little
    "chicken": chicken, "wooden_bucket": wooden_bucket, "stool": stool, "small_crate": small_crate,
    "flower_pot": flower_pot, "hay_bundle": hay_bundle,
    # small
    "barrel": barrel, "water_well": water_well, "market_stall": market_stall, "cart_wheel": cart_wheel,
    "hay_bale": hay_bale, "weapon_rack": weapon_rack, "scarecrow": scarecrow, "training_dummy": training_dummy,
    # medium
    "vegetable_cart": vegetable_cart, "oak_tree": oak_tree, "haystack": haystack, "wagon": wagon,
    "stone_fountain": stone_fountain, "torch_pole": torch_pole, "archery_target": archery_target,
}
