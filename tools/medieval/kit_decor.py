"""Ortaçağ kiti: zemin süsleri (yenmez, çarpışmasız; dışa aktarım öneki D_). Boş zeminleri doldurur.
Taban merkezi orijinde. Küçük ve ucuz (her biri ~20–80 üçgen); statik birleştirilir."""
import math
import random

from build_medieval import Part, rz


def _blade(p, x, y, h, lean, angle, color):
    """İnce, eğik çimen yaprağı (üçgen prizma)."""
    p.cyl(0.05, h, (x, y, 0), color, segments=3, radius_top=0.0,
          rot=rz(angle) @ __import__("mathutils").Matrix.Rotation(lean, 3, "X"))


def grass_tuft():
    p = Part("M_GrassTuft")
    rnd = random.Random(1)
    for i in range(6):
        a = i / 6 * math.tau + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(0.02, 0.09)
        _blade(p, math.cos(a) * r, math.sin(a) * r, rnd.uniform(0.22, 0.34), rnd.uniform(0.2, 0.45), a,
               rnd.choice(("leaf", "leaf_dark", "grass")))
    return p.finish()


def _flowers(name, color, seed):
    p = Part(name)
    rnd = random.Random(seed)
    for i in range(4):
        x, y = rnd.uniform(-0.18, 0.18), rnd.uniform(-0.18, 0.18)
        h = rnd.uniform(0.12, 0.22)
        p.box((0.025, 0.025, h), (x, y, 0), "leaf_dark")
        p.box((0.1, 0.1, 0.04), (x, y, h), color, rot_z=rnd.uniform(0, 1))
        p.box((0.045, 0.045, 0.05), (x, y, h + 0.005), "gold" if color != "gold" else "white")
    for i in range(3):
        a = rnd.uniform(0, math.tau)
        _blade(p, math.cos(a) * 0.08, math.sin(a) * 0.08, 0.16, 0.4, a, "leaf")
    return p.finish()


def flowers_white():
    return _flowers("M_FlowersWhite", "white", 2)


def flowers_yellow():
    return _flowers("M_FlowersYellow", "gold", 3)


def flowers_red():
    return _flowers("M_FlowersRed", "red", 4)


def pebbles():
    p = Part("M_Pebbles")
    rnd = random.Random(5)
    for i in range(4):
        s = rnd.uniform(0.07, 0.15)
        p.cyl(s, s * 0.45, (rnd.uniform(-0.2, 0.2), rnd.uniform(-0.2, 0.2), 0), rnd.choice(("stone", "stone_light", "stone_dark")),
              segments=5, radius_top=s * 0.7, rot=rz(rnd.uniform(0, 1)))
    return p.finish()


def bush_low():
    p = Part("M_BushLow")
    rnd = random.Random(6)
    for i in range(3):
        a = i / 3 * math.tau
        r = rnd.uniform(0.22, 0.3)
        x, y = math.cos(a) * 0.18, math.sin(a) * 0.18
        p.cyl(r * 0.8, r * 0.5, (x, y, 0), "leaf_dark", segments=7, radius_top=r)
        p.cyl(r, r * 0.45, (x, y, r * 0.5), "leaf", segments=7, radius_top=r * 0.45)
    return p.finish()


BUILDERS = {"grass_tuft": grass_tuft, "flowers_white": flowers_white, "flowers_yellow": flowers_yellow,
            "flowers_red": flowers_red, "pebbles": pebbles, "bush_low": bush_low}
