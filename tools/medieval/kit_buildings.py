"""Ortaçağ kiti: binalar (Sheet 1). Ön yüz −Y. Taban merkezi orijinde. Ölçü metre (insan ~1.7)."""
import math

from mathutils import Matrix

from build_medieval import Part, rz


def cottage():
    p = Part("M_Cottage")
    p.box((3.3, 2.7, 0.3), (0, 0, 0), "stone_dark")
    p.box((3.0, 2.4, 1.9), (0, 0, 0.3), "plaster")
    p.frame(0.7, 1.2, (0, -1.2, 0.3), glass="wood")
    for x in (-0.95, 0.95):
        p.frame(0.5, 0.5, (x, -1.2, 1.05))
    p.roof(3.0, 2.4, 1.6, (0, 0, 2.2), "straw", overhang=0.4, gable="plaster", rows=4, edge="straw_dark")
    p.box((0.5, 0.5, 1.3), (0.9, 0.5, 2.7), "stone")
    p.box((0.6, 0.6, 0.12), (0.9, 0.5, 4.0), "stone_dark")
    for x in (-1.4, 1.4):
        p.box((0.55, 0.45, 0.4), (x, -1.4, 0), "leaf")
    return p.finish()


def townhouse():
    p = Part("M_Townhouse")
    p.box((3.0, 2.8, 0.3), (0, 0, 0), "stone")
    p.box((2.9, 2.7, 1.6), (0, 0, 0.3), "plaster")
    p.box((3.2, 3.0, 1.6), (0, 0, 1.95), "plaster")
    p.box((3.35, 3.15, 0.18), (0, 0, 1.85), "wood_dark")
    for x in (-1.52, 0, 1.52):
        p.box((0.16, 0.12, 1.6), (x, -1.52, 1.95), "wood_dark")
    for x in (-1.37, 1.37):
        p.box((0.16, 0.12, 1.55), (x, -1.37, 0.3), "wood_dark")
    p.frame(0.75, 1.15, (0.7, -1.37, 0.3), glass="wood")
    p.frame(0.55, 0.55, (-0.75, -1.37, 0.85))
    for x in (-0.76, 0.76):
        p.frame(0.55, 0.6, (x, -1.52, 2.4))
        p.box((0.8, 0.28, 0.2), (x, -1.66, 2.2), "wood")
        p.box((0.65, 0.22, 0.16), (x, -1.66, 2.4), "leaf")
    p.roof(3.2, 3.0, 1.8, (0, 0, 3.55), "roof", overhang=0.28, gable="plaster", rows=5, edge="roof_dark")
    p.box((0.5, 0.5, 1.5), (0.95, 0.6, 4.1), "stone")
    p.box((0.62, 0.62, 0.12), (0.95, 0.6, 5.6), "stone_dark")
    return p.finish()


def barn():
    p = Part("M_Barn")
    p.box((4.2, 3.4, 0.25), (0, 0, 0), "stone_dark")
    p.box((4.0, 3.2, 2.4), (0, 0, 0.25), "roof_dark")                      # kırmızı ahşap
    for x in (-1.9, 1.9):
        p.box((0.18, 0.12, 2.4), (x, -1.62, 0.25), "wood_dark")
    p.box((1.8, 0.12, 1.9), (0, -1.62, 0.25), "wood")                       # büyük kapı
    p.box((0.12, 0.16, 1.9), (0, -1.66, 0.25), "wood_dark")
    p.box((1.9, 0.16, 0.14), (0, -1.66, 2.1), "wood_dark")
    p.box((1.9, 0.16, 0.14), (0, -1.66, 1.1), "wood_dark")
    p.frame(0.5, 0.45, (0, -1.6, 2.75), glass="dark")
    p.roof(3.2, 4.0, 1.9, (0, 0, 2.65), "straw", overhang=0.35, gable="roof_dark", rows=5, edge="straw_dark", rot_z=math.pi / 2)
    for x in (-2.3, 2.3):
        p.box((0.7, 0.55, 0.5), (x, -1.4, 0), "straw")                       # saman balyası
    return p.finish()


def narrow_townhouse():
    p = Part("M_NarrowTownhouse")
    p.box((1.9, 2.4, 0.3), (0, 0, 0), "stone")
    p.box((1.8, 2.3, 5.4), (0, 0, 0.3), "plaster")
    for z in (1.95, 3.7):
        p.box((2.0, 2.5, 0.16), (0, 0, z), "wood_dark")
    for x in (-0.86, 0.86):
        p.box((0.15, 0.12, 5.4), (x, -1.17, 0.3), "wood_dark")
    p.frame(0.6, 1.1, (0, -1.17, 0.3), glass="wood")
    for z in (2.4, 4.1):
        p.frame(0.55, 0.65, (0, -1.17, z))
    p.roof(1.8, 2.3, 1.7, (0, 0, 5.7), "roof", overhang=0.25, gable="plaster", rows=4, edge="roof_dark")
    p.box((0.42, 0.42, 1.2), (0.4, 0.7, 6.3), "stone")
    return p.finish()


def tavern():
    p = Part("M_Tavern")
    p.box((4.4, 3.2, 0.3), (0, 0, 0), "stone")
    p.box((4.2, 3.0, 1.7), (0, 0, 0.3), "plaster")
    p.box((4.4, 3.2, 1.5), (0, 0, 2.05), "plaster")
    p.box((4.55, 3.35, 0.18), (0, 0, 1.95), "wood_dark")
    for x in (-2.1, -0.7, 0.7, 2.1):
        p.box((0.16, 0.12, 1.7), (x, -1.52, 0.3), "wood_dark")
        p.box((0.16, 0.12, 1.5), (x * 1.04, -1.62, 2.05), "wood_dark")
    p.frame(0.9, 1.25, (0, -1.52, 0.3), glass="wood")
    for x in (-1.4, 1.4):
        p.frame(0.6, 0.55, (x, -1.52, 0.9))
    for x in (-1.4, 0, 1.4):
        p.frame(0.5, 0.5, (x, -1.62, 2.5))
    p.roof(4.4, 3.2, 1.9, (0, 0, 3.55), "roof", overhang=0.3, gable="plaster", rows=5, edge="roof_dark")
    p.box((0.5, 0.5, 1.5), (1.4, 0.7, 4.3), "stone")
    p.box((0.9, 0.1, 0.1), (2.55, -1.6, 2.6), "wood_dark")                  # tabela kolu
    p.box((0.62, 0.12, 0.62), (2.75, -1.6, 1.95), "blue")                   # tabela
    p.box((0.3, 0.14, 0.34), (2.75, -1.68, 2.08), "gold")                   # bira bardağı
    for x, y in ((-2.0, -2.0), (-1.4, -2.05), (2.0, -2.05)):
        p.cyl(0.32, 0.75, (x, y, 0), "wood", segments=8, top="wood_light")
        p.cyl(0.35, 0.08, (x, y, 0.15), "iron", segments=8)
    return p.finish()


def blacksmith():
    p = Part("M_Blacksmith")
    p.box((4.2, 3.2, 0.2), (0, 0, 0), "stone_dark")
    p.box((4.0, 0.3, 2.3), (0, 1.45, 0.2), "plaster")                        # arka duvar
    p.box((0.3, 3.0, 2.3), (-1.85, 0, 0.2), "plaster")                       # yan duvar
    for x in (-1.85, 0.1, 1.85):
        p.box((0.22, 0.22, 2.3), (x, -1.45, 0.2), "wood_dark")               # açık ön direkleri
    p.box((4.2, 0.22, 0.22), (0, -1.45, 2.3), "wood_dark")
    p.box((1.3, 1.1, 1.2), (1.1, 0.75, 0.2), "stone")                        # ocak
    p.box((0.8, 0.12, 0.55), (1.1, 0.18, 0.45), "fire")
    p.box((0.75, 0.75, 3.6), (1.1, 1.0, 1.4), "stone")                       # baca
    p.box((0.85, 0.85, 0.15), (1.1, 1.0, 5.0), "stone_dark")
    p.box((0.5, 0.3, 0.45), (-0.6, -0.4, 0.2), "wood_dark")                  # örs kütüğü
    p.box((0.7, 0.32, 0.25), (-0.6, -0.4, 0.65), "iron")                     # örs
    p.box((0.3, 0.25, 0.12), (-0.25, -0.4, 0.78), "iron")
    for x in (-1.4, -1.0, -0.6):                                            # duvardaki aletler
        p.box((0.08, 0.06, 0.6), (x, 1.28, 1.2), "iron_light")
    p.roof(4.2, 3.2, 1.6, (0, 0, 2.5), "roof", overhang=0.3, gable="plaster", rows=4, edge="roof_dark")
    p.cyl(0.32, 0.8, (-2.3, -1.6, 0), "wood", segments=8, top="water")      # su fıçısı
    return p.finish()


def bakery():
    p = Part("M_Bakery")
    p.box((3.4, 3.0, 0.3), (0, 0, 0), "stone")
    p.box((3.2, 2.8, 1.7), (0, 0, 0.3), "plaster")
    p.box((3.4, 3.0, 1.4), (0, 0, 2.05), "plaster")
    p.box((3.55, 3.15, 0.16), (0, 0, 1.95), "wood_dark")
    for x in (-1.62, 1.62):
        p.box((0.15, 0.12, 1.4), (x, -1.52, 2.05), "wood_dark")
    p.box((2.0, 0.12, 1.0), (-0.3, -1.42, 0.55), "dark")                     # vitrin
    p.box((2.0, 0.6, 0.18), (-0.3, -1.65, 0.45), "wood")                     # tezgâh
    for x in (-1.0, -0.55, -0.1, 0.35):                                      # ekmekler
        p.cyl(0.18, 0.14, (x, -1.65, 0.63), "straw_dark", segments=6)
    for i in range(5):                                                       # çizgili tente
        p.box((0.44, 0.9, 0.08), (-1.18 + i * 0.44, -1.85, 1.6), "blue" if i % 2 == 0 else "cream")
    p.frame(0.6, 1.1, (1.1, -1.42, 0.3), glass="wood")
    p.frame(0.55, 0.5, (0, -1.52, 2.45))
    p.roof(3.4, 3.0, 1.7, (0, 0, 3.45), "roof", overhang=0.28, gable="plaster", rows=4, edge="roof_dark")
    p.box((0.5, 0.5, 1.4), (-0.9, 0.6, 3.9), "stone")
    p.box((0.9, 0.1, 0.1), (2.05, -1.6, 2.5), "wood_dark")
    p.cyl(0.32, 0.1, (2.3, -1.55, 2.0), "gold", segments=8, rot=Matrix.Rotation(math.pi / 2, 3, "X"))
    p.cyl(0.2, 0.12, (2.3, -1.62, 2.0), "straw_dark", segments=6, rot=Matrix.Rotation(math.pi / 2, 3, "X"))
    return p.finish()


def chapel():
    p = Part("M_Chapel")
    p.box((3.2, 4.6, 0.3), (0, 0.4, 0), "stone_dark")
    p.box((3.0, 4.4, 2.6), (0, 0.4, 0.3), "stone_light")                     # nef
    p.roof(4.4, 3.0, 1.8, (0, 0.4, 2.9), "blue", overhang=0.25, gable="stone_light", rows=4, edge="iron", rot_z=math.pi / 2)
    p.box((1.6, 1.6, 4.6), (0, -1.4, 0.3), "stone")                          # çan kulesi
    p.box((1.0, 0.12, 0.9), (0, -2.22, 3.6), "dark")                         # çan açıklığı
    p.cyl(0.3, 0.5, (0, -2.05, 3.65), "gold", segments=8, radius_top=0.12)   # çan
    p.cyl(1.2, 1.4, (0, -1.4, 4.9), "blue", segments=4, radius_top=0.0, rot=rz(math.pi / 4))
    p.box((0.12, 0.12, 0.9), (0, -1.4, 6.2), "gold")                         # haç
    p.box((0.55, 0.12, 0.12), (0, -1.4, 6.75), "gold")
    p.frame(0.8, 1.3, (0, -2.2, 0.3), glass="wood")
    for y in (-0.2, 1.2):
        p.box((0.12, 0.45, 0.8), (-1.52, y, 1.3), "window")
    return p.finish()


def stables():
    p = Part("M_Stables")
    p.box((5.0, 2.8, 0.2), (0, 0, 0), "stone_dark")
    p.box((5.0, 0.3, 2.2), (0, 1.25, 0.2), "plaster")
    for x in (-2.4, 2.4):
        p.box((0.3, 2.8, 2.2), (x, 0, 0.2), "plaster")
    for x in (-2.4, -0.8, 0.8, 2.4):
        p.box((0.2, 0.2, 2.2), (x, -1.3, 0.2), "wood_dark")
    p.box((5.0, 0.2, 0.2), (0, -1.3, 2.2), "wood_dark")
    for x in (-1.6, 0.0, 1.6):                                               # bölme kapıları + at başı
        p.box((1.4, 0.12, 0.9), (x, -1.3, 0.2), "wood")
        p.box((0.5, 0.7, 0.55), (x, -1.0, 1.15), "wood_light")
        p.box((0.36, 0.45, 0.3), (x, -1.45, 1.3), "wood_light")
        p.box((0.1, 0.05, 0.1), (x - 0.12, -1.69, 1.45), "dark")
        p.box((0.1, 0.05, 0.1), (x + 0.12, -1.69, 1.45), "dark")
    p.roof(5.0, 2.8, 1.5, (0, 0, 2.4), "roof", overhang=0.3, gable="plaster", rows=4, edge="roof_dark")
    p.box((0.7, 0.5, 0.45), (2.9, -1.4, 0), "straw")
    return p.finish()


def barracks():
    p = Part("M_Barracks")
    p.box((5.4, 3.2, 0.25), (0, 0, 0), "stone_dark")
    p.box((5.2, 3.0, 2.6), (0, 0, 0.25), "stone")
    p.roof(4.6, 2.4, 1.3, (0, 0, 2.85), "roof", overhang=0.1, gable="plaster", rows=4, edge="roof_dark")
    for i in range(7):                                                       # ön mazgal dişleri
        p.box((0.5, 0.4, 0.45), (-2.4 + i * 0.8, -1.3, 2.85), "stone")
        p.box((0.5, 0.4, 0.45), (-2.4 + i * 0.8, 1.3, 2.85), "stone")
    for x in (-1.4, 1.4):
        p.frame(0.8, 1.3, (x, -1.5, 0.25), glass="wood")
    p.box((0.9, 0.1, 1.2), (0, -1.55, 1.1), "blue")                          # sancak
    p.box((0.5, 0.12, 0.5), (0, -1.6, 1.5), "gold")
    for x in (-2.3, 2.3):
        p.box((0.45, 0.1, 0.9), (x, -1.55, 1.2), "blue")
        p.box((0.2, 0.12, 0.2), (x, -1.6, 1.55), "gold")
    return p.finish()


def castle_keep():
    p = Part("M_CastleKeep")
    p.box((5.6, 5.6, 0.4), (0, 0, 0), "stone_dark")
    p.box((5.2, 5.2, 6.2), (0, 0, 0.4), "stone")
    p.box((5.6, 5.6, 0.3), (0, 0, 6.6), "stone_light")
    for i in range(5):                                                       # mazgallar
        t = -2.2 + i * 1.1
        for loc in ((t, -2.5, 6.9), (t, 2.5, 6.9), (-2.5, t, 6.9), (2.5, t, 6.9)):
            p.box((0.6, 0.6, 0.6), loc, "stone")
    p.box((1.5, 0.15, 2.0), (0, -2.65, 0.4), "wood_dark")                   # kapı
    p.box((1.3, 0.18, 1.8), (0, -2.68, 0.4), "iron")                         # parmaklık
    for x in (-0.4, 0.0, 0.4):
        p.box((0.08, 0.2, 1.8), (x, -2.72, 0.4), "iron_light")
    p.box((1.3, 0.12, 2.0), (0, -2.66, 3.0), "blue")                         # sancak
    p.box((0.6, 0.14, 0.5), (0, -2.7, 3.9), "gold")
    for x in (-1.6, 1.6):
        for z in (2.6, 4.6):
            p.box((0.25, 0.1, 0.7), (x, -2.62, z), "dark")                   # ok delikleri
    p.box((0.1, 0.1, 2.0), (1.8, 1.8, 6.9), "wood_dark")                     # bayrak direği
    p.box((1.0, 0.08, 0.6), (2.3, 1.8, 8.2), "blue")
    return p.finish()


BUILDERS = {"cottage": cottage, "townhouse": townhouse, "barn": barn, "narrow_townhouse": narrow_townhouse,
            "tavern": tavern, "blacksmith": blacksmith, "bakery": bakery, "chapel": chapel,
            "stables": stables, "barracks": barracks, "castle_keep": castle_keep}
