"""Ortaçağ kiti: köy çeşitleri (Bahar, 2026-11-21: referans köy haritaları — L planlı evler, farklı çatı renkleri,
avlu eşyaları). Binalar B_, eşyalar P_ (MODULE_PREFIX). Ön yüz −Y, taban merkezi orijinde, ölçü metre."""
import math

from build_medieval import Part, rz
from kit_props import AX_X, AX_Y, blob, rx, ry, tbox, tcyl

PI = math.pi


# ===================== binalar =====================

def _walls(p, w, d, h, loc, base="stone_dark", wall="plaster"):
    x, y, z = loc
    p.box((w + 0.3, d + 0.3, 0.3), (x, y, z), base)
    p.box((w, d, h), (x, y, z + 0.3), wall)


def house_l():
    """L planlı ev: ana gövde + öne çıkan kanat, kahve çatı."""
    p = Part("M_HouseL")
    _walls(p, 3.8, 2.6, 2.0, (-0.5, 0.5, 0))
    _walls(p, 2.3, 2.4, 2.0, (1.3, -1.0, 0))
    p.frame(0.75, 1.2, (-1.2, -0.8, 0.3), glass="wood")
    for x in (-2.0, 0.0):
        p.frame(0.5, 0.5, (x, -0.8, 1.15))
    p.frame(0.5, 0.5, (1.3, -2.2, 1.0))
    for x, y in ((-1.6, -0.82), (0.4, -0.82)):
        p.box((0.7, 0.26, 0.18), (x, y - 0.12, 0.95), "wood")
        p.box((0.6, 0.2, 0.15), (x, y - 0.12, 1.12), "leaf")
    p.roof(3.8, 2.6, 1.5, (-0.5, 0.5, 2.3), "roof_brown", overhang=0.3, gable="plaster", rows=4, edge="wood_dark")
    p.roof(2.4, 2.3, 1.35, (1.3, -1.0, 2.3), "roof_brown", overhang=0.3, gable="plaster", rows=4, edge="wood_dark",
           rot_z=PI / 2)
    p.box((0.5, 0.5, 1.2), (-1.6, 1.0, 3.0), "stone")
    p.box((0.62, 0.62, 0.12), (-1.6, 1.0, 4.2), "stone_dark")
    return p.finish()


def long_house():
    """Uzun ev: iki kapı, sıra pencere, bej çatı (referanstaki uzun ambar-evler)."""
    p = Part("M_LongHouse")
    _walls(p, 5.6, 2.6, 2.1, (0, 0, 0))
    for x in (-2.84, 0, 2.84):
        p.box((0.16, 0.12, 2.1), (x, -1.32, 0.3), "wood_dark")
    for x in (-1.5, 1.5):
        p.frame(0.75, 1.2, (x, -1.3, 0.3), glass="wood")
    for x in (-2.3, -0.6, 0.6, 2.3):
        p.frame(0.45, 0.45, (x, -1.3, 1.3))
    p.roof(5.6, 2.6, 1.5, (0, 0, 2.4), "roof_beige", overhang=0.3, gable="plaster", rows=4, edge="wood_light")
    for x in (-1.8, 1.9):
        p.box((0.45, 0.45, 1.0), (x, 0.6, 3.1), "stone")
        p.box((0.56, 0.56, 0.1), (x, 0.6, 4.1), "stone_dark")
    for x in (-2.9, 2.9):
        p.box((0.5, 0.4, 0.4), (x, -1.55, 0), "wood", top="wood_light")      # sandık
    return p.finish()


def cottage_slate():
    """Köylü evi, arduvaz (mavi-gri) çatılı çeşit."""
    p = Part("M_CottageSlate")
    _walls(p, 3.0, 2.4, 1.9, (0, 0, 0))
    p.frame(0.7, 1.2, (0, -1.2, 0.3), glass="wood")
    for x in (-0.95, 0.95):
        p.frame(0.5, 0.5, (x, -1.2, 1.05))
    p.roof(3.0, 2.4, 1.6, (0, 0, 2.2), "roof_slate", overhang=0.4, gable="plaster", rows=4, edge="stone_dark")
    p.box((0.5, 0.5, 1.3), (0.9, 0.5, 2.7), "stone")
    p.box((0.6, 0.6, 0.12), (0.9, 0.5, 4.0), "stone_dark")
    for x in (-1.4, 1.4):
        p.box((0.55, 0.45, 0.4), (x, -1.4, 0), "leaf")
    return p.finish()


def cottage_red():
    """Köylü evi, kırmızı kiremit çatılı çeşit (yarı ahşap cephe)."""
    p = Part("M_CottageRed")
    _walls(p, 3.2, 2.5, 2.0, (0, 0, 0))
    for x in (-1.62, 1.62):
        p.box((0.14, 0.12, 2.0), (x, -1.27, 0.3), "wood_dark")
    p.box((3.3, 0.12, 0.14), (0, -1.27, 1.25), "wood_dark")
    p.frame(0.7, 1.2, (-0.6, -1.25, 0.3), glass="wood")
    p.frame(0.55, 0.5, (0.8, -1.25, 0.9))
    p.roof(3.2, 2.5, 1.6, (0, 0, 2.3), "roof", overhang=0.32, gable="plaster", rows=5, edge="roof_dark")
    p.box((0.45, 0.45, 1.2), (-0.9, 0.5, 2.9), "stone")
    p.box((0.56, 0.56, 0.1), (-0.9, 0.5, 4.1), "stone_dark")
    p.box((0.9, 0.35, 0.3), (0.9, -1.45, 0), "wood", top="dirt")             # pencere önü saksı
    for x in (0.65, 0.9, 1.15):
        p.box((0.14, 0.14, 0.14), (x, -1.45, 0.3), "red" if x != 0.9 else "white")
    return p.finish()


# ===================== avlu eşyaları =====================

def crate_stack():
    p = Part("M_CrateStack")
    for x, y, z, s, r in ((-0.33, 0, 0, 0.62, 0.0), (0.33, 0.05, 0, 0.6, 0.15), (-0.05, 0.0, 0.62, 0.56, -0.2),
                          (0.0, 0.62, 0, 0.55, 0.3)):
        p.box((s, s, s), (x, y, z), "wood", top="wood_light", rot_z=r)
        p.box((s + 0.02, 0.06, 0.07), (x, y - s / 2, z + s * 0.45), "wood_dark", rot_z=r)
    return p.finish()


def woodpile():
    p = Part("M_Woodpile")
    for row, n in ((0, 4), (1, 3), (2, 2)):
        for k in range(n):
            x = (k - (n - 1) / 2) * 0.32
            tcyl(p, 0.15, 1.3, (x, 0, 0.15 + row * 0.27), "wood", AX_Y, segments=7, top="wood_light")
    p.box((0.08, 0.08, 0.9), (-0.8, 0, 0), "wood_dark")
    p.box((0.08, 0.08, 0.9), (0.8, 0, 0), "wood_dark")
    tbox(p, (0.5, 0.06, 0.06), (0.95, -0.4, 0.05), "iron_light", rz(0.6))      # balta sapı yerine kıymık
    return p.finish()


def bench():
    p = Part("M_Bench")
    p.box((1.5, 0.45, 0.08), (0, 0, 0.42), "wood", top="wood_light")
    p.box((1.5, 0.08, 0.4), (0, 0.22, 0.5), "wood")
    for x in (-0.6, 0.6):
        p.box((0.1, 0.4, 0.42), (x, 0, 0), "wood_dark")
    return p.finish()


def flower_bed():
    p = Part("M_FlowerBed")
    p.box((1.6, 0.7, 0.25), (0, 0, 0), "wood_dark", top="dirt")
    cols = ("red", "white", "gold", "blue", "white", "red")
    for k, c in enumerate(cols):
        x = -0.62 + k * 0.25
        p.box((0.06, 0.06, 0.2), (x, -0.1 + (k % 2) * 0.2, 0.25), "leaf_dark")
        p.box((0.16, 0.16, 0.1), (x, -0.1 + (k % 2) * 0.2, 0.45), c, rot_z=0.4 * k)
        p.box((0.2, 0.12, 0.05), (x + 0.05, -0.1 + (k % 2) * 0.2, 0.3), "leaf", rot_z=0.7)
    return p.finish()


def laundry_line():
    p = Part("M_LaundryLine")
    for x in (-1.1, 1.1):
        p.box((0.1, 0.1, 1.7), (x, 0, 0), "wood_dark")
    p.box((2.2, 0.03, 0.03), (0, 0, 1.6), "cream")
    for x, w, h, c in ((-0.65, 0.45, 0.6, "white"), (-0.05, 0.5, 0.75, "blue"), (0.55, 0.42, 0.55, "red")):
        p.box((w, 0.04, h), (x, 0, 1.6 - h), c)
    return p.finish()


def signpost():
    p = Part("M_Signpost")
    p.box((0.12, 0.12, 1.8), (0, 0, 0), "wood_dark")
    tbox(p, (0.85, 0.06, 0.24), (0.3, 0, 1.55), "wood_light", rz(0.15))
    tbox(p, (0.75, 0.06, 0.22), (-0.25, 0, 1.2), "wood", rz(-0.2) @ rz(PI))
    p.box((0.22, 0.22, 0.12), (0, 0, 1.8), "wood_dark")
    return p.finish()


def hedge():
    p = Part("M_Hedge")
    p.box((1.8, 0.6, 0.6), (0, 0, 0), "leaf_dark", top="leaf")
    for x in (-0.6, 0.0, 0.6):
        blob(p, 0.38, (x, 0, 0.45), "leaf", spin=x)
    return p.finish()


def market_tent():
    """Renkli tenteli pazar tezgâhı (meydan kenarı): dört direk + çizgili tente + mal."""
    p = Part("M_MarketTent")
    for x in (-0.9, 0.9):
        for y in (-0.6, 0.6):
            p.box((0.08, 0.08, 1.9), (x, y, 0), "wood_dark")
    p.box((1.9, 1.0, 0.7), (0, 0.1, 0), "wood", top="wood_light")
    for k in range(6):
        c = "red" if k % 2 == 0 else "cream"
        tbox(p, (0.34, 1.5, 0.06), (-0.85 + k * 0.34, 0, 2.0), c, rx(0.25))
    for x, c in ((-0.6, "red"), (-0.2, "gold"), (0.2, "leaf"), (0.6, "fire")):
        blob(p, 0.16, (x, 0.0, 0.7), c, seg=6)
    return p.finish()


# ===================== yol taşları (3D, Bahar 2026-11-21: "taşlar tek boyutlu duruyor") =====================
# Alçak, düzensiz çokgen taş: taban biraz geniş, üstü hafif kubbeli. Yerleşim build_castle_layout (karışım haritasının
# taş bölgesi), Unity'de bölge bölge tek mesh'te birleşir (CastleSceneBuilder). Yenmez, çarpışmasız (D_).
import random as _random

import bmesh as _bmesh
from mathutils import Vector as _V


def _path_stone(seed):
    rnd = _random.Random(seed)
    p = Part("M_PathStone%d" % seed)
    n = rnd.randint(9, 12)
    rx, ry = rnd.uniform(0.6, 0.85), rnd.uniform(0.45, 0.7)
    # düzensiz kenar: köşe yarıçapları geniş aralıkta, komşular arası yumuşak
    raw = [rnd.uniform(0.7, 1.0) for _ in range(n)]
    radii = [(raw[i - 1] + 2 * raw[i] + raw[(i + 1) % n]) / 4 for i in range(n)]
    h = rnd.uniform(0.12, 0.17)
    tilt = (rnd.uniform(-0.03, 0.03), rnd.uniform(-0.03, 0.03))   # üst yüz hafif eğik: hepsi aynı ışığı almasın
    bm = p.bm
    rings = []
    for z, k in ((0.0, 1.0), (h * 0.65, 0.96), (h, 0.84)):
        ring = []
        for i in range(n):
            a = i * 2 * math.pi / n + rnd.uniform(-0.08, 0.08)
            x, y = math.cos(a) * rx * radii[i] * k, math.sin(a) * ry * radii[i] * k
            ring.append(bm.verts.new(_V((x, y, z + (x * tilt[0] + y * tilt[1] if z > 0 else 0.0)))))
        rings.append(ring)
    sides = []
    for r0, r1 in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            sides.append(bm.faces.new([r0[i], r0[j], r1[j], r1[i]]))
    top = bm.faces.new(rings[-1])
    p._paint(sides, "stone_dark")
    p._paint([top], "stone")
    return p.finish()


def path_stone1(): return _path_stone(1)
def path_stone2(): return _path_stone(2)
def path_stone3(): return _path_stone(3)
def path_stone4(): return _path_stone(4)
def path_stone5(): return _path_stone(5)


BUILDERS = {"house_l": house_l, "long_house": long_house, "cottage_slate": cottage_slate, "cottage_red": cottage_red,
            "crate_stack": crate_stack, "woodpile": woodpile, "bench": bench, "flower_bed": flower_bed,
            "laundry_line": laundry_line, "signpost": signpost, "hedge": hedge, "market_tent": market_tent,
            "path_stone1": path_stone1, "path_stone2": path_stone2, "path_stone3": path_stone3,
            "path_stone4": path_stone4, "path_stone5": path_stone5}
# Hangileri bina (B_), hangileri eşya (P_): export_name modüle göre önek verir; köy modülü karışık olduğundan adla.
BUILDING_KEYS = {"house_l", "long_house", "cottage_slate", "cottage_red"}
