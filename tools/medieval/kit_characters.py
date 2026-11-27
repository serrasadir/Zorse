"""Ortaçağ kiti: karakterler (köylüler + düşmanlar). Ön yüz −Y, ayak z=0, insan ~1.7 m (chibi: büyük kafa).
Oyun mesh'i otomatik böler: bacaklar z=0..kalça ayrı kutular (arada boşluk), kollar gövde yanında, el kalçanın üstünde."""
from build_medieval import Part, rz

import math

from mathutils import Matrix

HIP = 0.52
TORSO_H = 0.52
NECK = HIP + TORSO_H          # 1.04
HEAD = (0.46, 0.40, 0.44)     # kafa kutusu
HEAD_Z = NECK + 0.02


def rx(a):
    return Matrix.Rotation(a, 3, "X")


def ry(a):
    return Matrix.Rotation(a, 3, "Y")


def rod(p, a, b, r, color, segments=5):
    """XZ düzleminde a'dan b'ye çubuk (yay kolu vb.)."""
    dx, dz = b[0] - a[0], b[2] - a[2]
    ln = math.hypot(dx, dz)
    p.cyl(r, ln, a, color, segments=segments, rot=ry(math.atan2(dx, dz)))


# ---------- ortak gövde ----------

def legs(p, pants, shoes, hip=HIP, w=0.17, d=0.2, x=0.115):
    for sx in (-1, 1):
        p.box((w, d, hip - 0.1), (sx * x, 0, 0.1), pants)
        p.box((w + 0.02, d + 0.08, 0.1), (sx * x, -0.04, 0), shoes)


def torso(p, color, w=0.5, d=0.34, hip=HIP, h=TORSO_H, belt="wood_dark", buckle="gold"):
    p.box((w, d, h), (0, 0, hip), color)
    if belt:
        p.box((w + 0.02, d + 0.02, 0.07), (0, 0, hip + 0.06), belt)
        if buckle:
            p.box((0.09, 0.03, 0.09), (0, -d / 2 - 0.01, hip + 0.05), buckle)


def arms(p, sleeve, hand="skin", tw=0.5, top=NECK - 0.02, length=0.3, side=(-1, 1)):
    """Kol: gövdenin yanında, omuzdan aşağı. El alt ucu ~top-length-0.1 (kalçanın epey üstünde)."""
    x = tw / 2 + 0.095
    for sx in side:
        p.box((0.14, 0.17, length), (sx * x, 0, top - length), sleeve)
        p.box((0.13, 0.15, 0.1), (sx * x, 0, top - length - 0.1), hand)
    return x, top - length - 0.05          # el merkezi (x, z)


def head(p, skin="skin", z=HEAD_Z, eyes_z=None, y_eye=None):
    w, d, h = HEAD
    p.box((w, d, h), (0, 0, z), skin)
    ez = z + 0.14 if eyes_z is None else eyes_z
    for sx in (-1, 1):
        p.box((0.06, 0.03, 0.09), (sx * 0.095, -d / 2 - 0.005, ez), "dark")
    return z + h


def hair(p, color, z=HEAD_Z, fringe=True, cap=True):
    w, d, h = HEAD
    if cap:
        p.box((w + 0.04, d + 0.04, 0.12), (0, 0.0, z + h - 0.06), color)   # tepe
    p.box((w + 0.04, 0.1, h - 0.1), (0, d / 2 - 0.03, z + 0.06), color)     # ense
    for sx in (-1, 1):
        p.box((0.06, d * 0.7, 0.24), (sx * (w / 2 + 0.01), 0.04, z + 0.18), color)
    if fringe:
        p.box((w * 0.8, 0.06, 0.07), (0.04, -d / 2 - 0.01, z + h - 0.11), color)


def fleur(p, x, y, z, s=1.0, color="gold", side=0):
    """Zambak rozeti. side=0: ön yüzde (−Y); side=±1: ±X yan yüzde (Y ekseninde uzanır). (x,y,z) = alt orta."""
    def b(w, h, du, dz):
        if side:
            p.box((0.03, w, h), (x, y + du, z + dz), color)
        else:
            p.box((w, 0.03, h), (x + du, y, z + dz), color)
    b(0.06 * s, 0.24 * s, 0, 0)
    b(0.1 * s, 0.06 * s, 0, 0.14 * s)
    b(0.2 * s, 0.04 * s, 0, 0.06 * s)
    for d in (-1, 1):
        b(0.05 * s, 0.1 * s, d * 0.09 * s, 0.1 * s)
        b(0.04 * s, 0.05 * s, d * 0.06 * s, 0.0)


def straw_hat(p, z, r=0.37, crown=0.21):
    p.cyl(r, 0.05, (0, 0, z), "straw", segments=8, top="straw")
    p.cyl(crown, 0.16, (0, 0, z + 0.05), "straw", segments=8, radius_top=crown * 0.85)
    p.cyl(crown + 0.01, 0.04, (0, 0, z + 0.06), "straw_dark", segments=8)


# ---------- köylüler ----------

def male_peasant():
    p = Part("M_PeasantMale")
    legs(p, "wood_dark", "wood")
    torso(p, "cream")
    p.box((0.52, 0.36, 0.38), (0, 0.0, HIP + 0.13), "wood_dark")       # yelek (arka + yan)
    p.box((0.12, 0.02, 0.36), (0, -0.19, HIP + 0.15), "cream")          # yelek açıklığı
    arms(p, "cream")
    top = head(p)
    hair(p, "wood_dark", cap=False)
    straw_hat(p, top - 0.06)
    return p.finish()


def female_peasant():
    p = Part("M_PeasantFemale")
    # Uzun etek bacak kutularında: iki mavi panel + önlük şeridi (yürürken bacakla sallanır).
    for sx in (-1, 1):
        p.box((0.23, 0.3, HIP - 0.1), (sx * 0.125, 0, 0.1), "blue")
        p.box((0.12, 0.03, HIP - 0.24), (sx * 0.075, -0.16, 0.24), "cream")
        p.box((0.17, 0.24, 0.1), (sx * 0.12, -0.04, 0), "wood_dark")
    torso(p, "cream", w=0.46, d=0.32, belt=None)
    p.box((0.48, 0.34, 0.26), (0, 0, HIP), "wood_dark")                 # korse
    p.box((0.3, 0.03, 0.22), (0, -0.18, HIP + 0.02), "cream")           # önlük üstü
    arms(p, "cream", tw=0.46)
    top = head(p)
    w, d, h = HEAD
    p.box((0.3, 0.04, 0.08), (0.0, -d / 2 - 0.01, HEAD_Z + h - 0.13), "wood")   # kâkül
    # başörtüsü: tepe + yanlar + ense düğümü
    p.box((w + 0.08, d + 0.08, 0.14), (0, 0.01, top - 0.08), "cream")
    for sx in (-1, 1):
        p.box((0.07, d + 0.04, 0.34), (sx * (w / 2 + 0.03), 0.03, HEAD_Z + 0.04), "cream")
    p.box((w + 0.08, 0.08, 0.38), (0, d / 2 + 0.02, HEAD_Z + 0.04), "cream")
    p.box((0.14, 0.1, 0.16), (0, d / 2 + 0.08, HEAD_Z + 0.02), "cream")
    return p.finish()


def merchant():
    p = Part("M_Merchant")
    legs(p, "wood_dark", "dark", x=0.13, w=0.19)
    tw = 0.64
    torso(p, "cream", w=tw, d=0.46)
    p.box((tw + 0.03, 0.47, 0.42), (0, 0.0, HIP + 0.1), "roof")         # kırmızı yelek
    p.box((0.1, 0.03, 0.4), (0, -0.25, HIP + 0.1), "gold")             # düğme şeridi
    p.box((tw + 0.05, 0.49, 0.07), (0, 0, HIP + 0.05), "wood_dark")
    p.box((0.1, 0.03, 0.1), (0, -0.26, HIP + 0.04), "gold")
    arms(p, "cream", tw=tw)
    top = head(p)
    hair(p, "wood_dark", fringe=False)
    p.box((0.3, 0.05, 0.07), (0, -HEAD[1] / 2 - 0.02, HEAD_Z + 0.07), "wood_dark")   # bıyık
    for sx in (-1, 1):
        p.box((0.08, 0.05, 0.06), (sx * 0.17, -HEAD[1] / 2 - 0.02, HEAD_Z + 0.04), "wood_dark")
    p.box((0.1, 0.06, 0.08), (0, -HEAD[1] / 2 - 0.03, HEAD_Z + 0.11), "skin")        # burun
    # kadife bere + tüy
    p.cyl(0.3, 0.12, (0, 0.01, top - 0.08), "roof_dark", segments=8, radius_top=0.33)
    p.cyl(0.33, 0.08, (0, 0.01, top + 0.04), "roof_dark", segments=8, radius_top=0.25)
    p.box((0.06, 0.04, 0.26), (0.18, -0.08, top + 0.02), "gold", rot_z=0.3)
    p.box((0.05, 0.04, 0.12), (0.22, -0.06, top + 0.24), "gold", rot_z=0.3)
    # sırtta çuval
    p.box((0.5, 0.36, 0.5), (0.02, 0.4, HIP + 0.05), "dirt")
    p.box((0.32, 0.26, 0.14), (0.02, 0.4, HIP + 0.55), "dirt")
    p.box((0.12, 0.12, 0.1), (0.02, 0.4, HIP + 0.69), "wood_dark")
    return p.finish()


def farmer():
    p = Part("M_Farmer")
    legs(p, "leaf_dark", "wood_dark")
    torso(p, "cream", belt=None)
    p.box((0.52, 0.36, 0.3), (0, 0, HIP), "leaf_dark")                 # tulum gövdesi
    p.box((0.32, 0.03, 0.18), (0, -0.19, HIP + 0.28), "leaf_dark")     # göğüs cebi
    for sx in (-1, 1):
        p.box((0.07, 0.38, 0.06), (sx * 0.16, 0, HIP + 0.44), "leaf_dark")  # askı
        p.box((0.05, 0.03, 0.05), (sx * 0.13, -0.2, HIP + 0.4), "gold")
    hx, hz = arms(p, "cream")
    top = head(p)
    hair(p, "wood_dark", fringe=False, cap=False)
    d = HEAD[1]
    p.box((0.44, 0.08, 0.18), (0, -d / 2 - 0.03, HEAD_Z - 0.04), "wood_dark")   # sakal
    p.box((0.14, 0.06, 0.06), (0, -d / 2 - 0.05, HEAD_Z + 0.06), "skin")        # ağız boşluğu
    p.box((0.3, 0.06, 0.06), (0, -d / 2 - 0.04, HEAD_Z + 0.1), "wood_dark")     # bıyık
    straw_hat(p, top - 0.06)
    # dirgen: sağ elde (−X), dik
    x = -hx
    p.cyl(0.03, 1.55, (x, -0.11, 0.18), "wood", segments=6)
    p.box((0.3, 0.05, 0.05), (x, -0.11, 1.72), "iron")
    for dx in (-0.13, 0, 0.13):
        p.box((0.04, 0.04, 0.24), (x + dx, -0.11, 1.76), "iron_light")
    return p.finish()


# Ek köylüler (Serra): sadece kutu, yumuşatmasız dışa aktarılır (--hard; az yüzey).
def minstrel():
    """Ozan: bordo tüylü şapka, sarı tunik, yeşil yaka/kol ağzı, yeşil tayt, çizme, önünde ud."""
    p = Part("M_Minstrel")
    for sx in (-1, 1):
        p.box((0.17, 0.2, HIP - 0.1), (sx * 0.115, 0, 0.1), "leaf_dark")
        p.box((0.19, 0.24, 0.28), (sx * 0.115, 0, 0.0), "wood_dark")          # çizme
        p.box((0.19, 0.08, 0.1), (sx * 0.115, -0.15, 0.0), "wood_dark")       # çizme burnu
        p.box((0.21, 0.24, 0.14), (sx * 0.12, 0, HIP - 0.14), "gold")         # tunik eteği
    torso(p, "gold", buckle="wood_dark")
    p.box((0.32, 0.03, 0.08), (0, -0.18, NECK - 0.08), "leaf_dark")           # yaka
    p.box((0.05, 0.03, 0.2), (0, -0.18, NECK - 0.27), "leaf_dark")            # yaka yırtmacı
    x, _ = arms(p, "gold")
    for sx in (-1, 1):
        p.box((0.16, 0.19, 0.09), (sx * x, 0, NECK - 0.11), "leaf_dark")       # omuz
        p.box((0.16, 0.19, 0.07), (sx * x, 0, NECK - 0.34), "leaf_dark")       # kol ağzı
    # ud: gövde önünde, sapı sağ üste basamaklı
    p.box((0.26, 0.08, 0.26), (-0.07, -0.21, HIP - 0.02), "wood_dark")
    p.box((0.08, 0.03, 0.08), (-0.07, -0.255, HIP + 0.08), "dark")
    for i in range(3):
        p.box((0.07, 0.05, 0.11), (0.06 + i * 0.06, -0.22, HIP + 0.2 + i * 0.1), "wood_light")
    p.box((0.09, 0.06, 0.08), (0.23, -0.22, HIP + 0.5), "wood_dark")
    top = head(p)
    hair(p, "wood_dark")
    p.box((0.54, 0.5, 0.1), (0, 0, top - 0.1), "maroon")                       # şapka siperi
    p.box((0.42, 0.38, 0.2), (0, 0.01, top), "maroon")
    p.box((0.32, 0.28, 0.05), (0, 0.01, top + 0.2), "maroon")
    for i, (y, w) in enumerate(((0.06, 0.14), (0.12, 0.12), (0.18, 0.1))):    # tüy: arkaya kalkan basamaklar
        p.box((0.05, w, 0.15), (0.2, y, top + 0.08 + i * 0.13), "roof_dark")
    return p.finish()


def _dress(p, dress, apron):
    """Uzun etek bacak kutularında (yürürken bacakla sallanır) + önlük şeridi + ayakkabı."""
    for sx in (-1, 1):
        p.box((0.23, 0.3, HIP - 0.1), (sx * 0.125, 0, 0.1), dress)
        if apron:
            p.box((0.12, 0.03, HIP - 0.24), (sx * 0.075, -0.16, 0.24), apron)
        p.box((0.17, 0.24, 0.1), (sx * 0.12, -0.04, 0), "wood_dark")


def peasant_hat():
    """Hasır şapkalı köylü kadın: kırmızı elbise, krem bluz, önlük, örgü saç, kurdeleli geniş hasır şapka."""
    p = Part("M_PeasantHat")
    _dress(p, "roof", "cream")
    torso(p, "cream", w=0.46, d=0.32, belt=None)
    p.box((0.48, 0.34, 0.28), (0, 0, HIP), "roof")                            # korse
    for i in range(3):
        p.box((0.1, 0.02, 0.025), (0, -0.18, HIP + 0.06 + i * 0.08), "dark")   # bağcık
    arms(p, "cream", tw=0.46)
    top = head(p)
    hair(p, "wood_light", cap=False)
    p.box((0.12, 0.1, 0.46), (0, HEAD[1] / 2 + 0.03, HEAD_Z - 0.34), "wood_light")   # örgü
    p.box((0.14, 0.11, 0.06), (0, HEAD[1] / 2 + 0.03, HEAD_Z - 0.3), "red")
    p.box((0.8, 0.76, 0.05), (0, 0, top - 0.1), "straw")                       # geniş siper
    p.box((0.38, 0.36, 0.17), (0, 0, top - 0.05), "straw")
    p.box((0.39, 0.37, 0.05), (0, 0, top - 0.04), "red")                        # kurdele
    return p.finish()


def peasant_basket():
    """Sepetli köylü kadın: yeşil elbise, beyaz önlük, bordo korse ve başlık, önünde elma sepeti."""
    p = Part("M_PeasantBasket")
    _dress(p, "leaf_dark", None)
    for sx in (-1, 1):
        p.box((0.2, 0.03, HIP - 0.16), (sx * 0.1, -0.16, 0.16), "white")       # önlük
    torso(p, "white", w=0.46, d=0.32, belt=None)
    p.box((0.48, 0.34, 0.3), (0, 0, HIP), "maroon")                           # korse
    arms(p, "white", tw=0.46)
    top = head(p)
    w, d, h = HEAD
    hair(p, "dark", cap=False)
    p.box((w + 0.06, d + 0.06, 0.12), (0, 0.01, top - 0.06), "white")          # beyaz başlık
    p.box((w + 0.06, 0.08, 0.26), (0, d / 2 + 0.02, HEAD_Z + 0.12), "white")
    p.box((0.36, 0.32, 0.08), (0, 0.03, top + 0.04), "maroon")                 # üstünde bordo kep
    # sepet: gövde önünde, iki elle tutuluyor gibi
    p.box((0.32, 0.22, 0.16), (0, -0.3, HIP - 0.04), "straw_dark")
    p.box((0.34, 0.24, 0.04), (0, -0.3, HIP + 0.1), "straw")
    for sx in (-1, 0, 1):
        p.box((0.08, 0.08, 0.07), (sx * 0.09, -0.3, HIP + 0.12), "red")         # elmalar
    for sx in (-1, 1):
        p.box((0.03, 0.03, 0.2), (sx * 0.14, -0.3, HIP + 0.12), "wood")          # sap
    p.box((0.31, 0.03, 0.03), (0, -0.3, HIP + 0.31), "wood")
    return p.finish()


# ---------- düşmanlar ----------

def militia():
    p = Part("M_Militia")
    legs(p, "wood_dark", "wood")
    torso(p, "cream")
    p.box((0.52, 0.36, 0.38), (0, 0, HIP + 0.13), "wood")              # deri yelek
    p.box((0.08, 0.38, 0.5), (0.1, 0, HIP + 0.02), "wood_dark", rot_z=0)  # kayış
    hx, hz = arms(p, "cream")
    top = head(p)
    hair(p, "wood", fringe=False)
    w, d, h = HEAD
    p.box((w + 0.06, d + 0.06, 0.1), (0, 0, HEAD_Z + h - 0.16), "red")   # kafa bandı
    p.box((0.1, 0.1, 0.16), (0.08, d / 2 + 0.06, HEAD_Z + h - 0.24), "red")
    p.box((w + 0.04, d + 0.04, 0.1), (0, 0, top - 0.06), "wood")         # saç tepesi
    # sopa: sağ elde, dik, kalın uç yukarıda
    x = -hx
    p.cyl(0.045, 0.4, (x, -0.1, hz - 0.1), "wood_light", segments=6, radius_top=0.07)
    p.cyl(0.07, 0.4, (x, -0.1, hz + 0.3), "wood_light", segments=6, radius_top=0.12)
    p.cyl(0.12, 0.07, (x, -0.1, hz + 0.7), "wood", segments=6, radius_top=0.07)
    return p.finish()


def town_guard():
    p = Part("M_TownGuard")
    legs(p, "wood_dark", "wood")
    torso(p, "blue", belt=None)
    p.box((0.42, 0.04, 0.5), (0, -0.18, HIP - 0.04), "blue")           # tabar ön
    p.box((0.46, 0.05, 0.04), (0, -0.19, HIP - 0.06), "gold")
    fleur(p, 0, -0.205, HIP + 0.12, s=1.0)
    p.box((0.54, 0.38, 0.07), (0, 0, HIP + NECK - HIP - 0.1), "gold")  # yaka
    hx, hz = arms(p, "blue")
    top = head(p)
    hair(p, "wood", fringe=False, cap=False)
    # kettle miğfer: geniş kenar + kubbe
    p.cyl(0.4, 0.05, (0, 0, top - 0.1), "iron_light", segments=8, radius_top=0.36)
    p.cyl(0.32, 0.18, (0, 0, top - 0.05), "iron_light", segments=8, radius_top=0.25)
    p.cyl(0.25, 0.07, (0, 0, top + 0.13), "iron_light", segments=8, radius_top=0.1)
    # mızrak: sağ elde dik
    x = -hx
    p.cyl(0.03, 1.75, (x, -0.11, 0.08), "wood", segments=6)
    p.cyl(0.06, 0.05, (x, -0.11, 1.83), "iron", segments=6)
    p.cyl(0.07, 0.24, (x, -0.11, 1.88), "iron_light", segments=4, radius_top=0.0)
    return p.finish()


def shield_knight():
    p = Part("M_ShieldKnight")
    legs(p, "iron_light", "iron", w=0.18, x=0.12)
    for sx in (-1, 1):
        p.box((0.2, 0.22, 0.12), (sx * 0.12, -0.01, 0.28), "iron")      # dizlik
    tw = 0.56
    torso(p, "iron_light", w=tw, d=0.38, belt="wood_dark", buckle="gold")
    p.box((0.44, 0.04, 0.26), (0, -0.2, HIP - 0.05), "blue")          # etek tabar
    hx, hz = arms(p, "iron_light", hand="iron", tw=tw)
    for sx in (-1, 1):
        p.box((0.2, 0.22, 0.14), (sx * hx, 0, NECK - 0.12), "iron")    # omuzluk
    # tam kapalı miğfer (kafa yerine)
    w, d, h = HEAD
    p.box((w, d, h + 0.04), (0, 0, HEAD_Z), "iron_light")
    p.box((w + 0.02, d + 0.02, 0.08), (0, 0, HEAD_Z + h - 0.02), "iron_light")
    p.box((0.32, 0.03, 0.06), (0, -d / 2 - 0.005, HEAD_Z + 0.22), "dark")   # vizör yarığı
    for dx in (-0.08, 0, 0.08):
        p.box((0.03, 0.03, 0.12), (dx, -d / 2 - 0.005, HEAD_Z + 0.06), "dark")
    p.box((0.04, d + 0.04, 0.06), (0, 0, HEAD_Z + h + 0.02), "iron")   # tepe sırtı
    # büyük mavi kalkan: sol kol (+X) tarafında, gövdenin önünde, bacaklara inmeden
    sx = hx - 0.08
    y = -0.3
    p.box((0.52, 0.06, 0.62), (sx, y, HIP + 0.02), "gold")
    p.box((0.44, 0.04, 0.54), (sx, y - 0.04, HIP + 0.06), "blue")
    fleur(p, sx, y - 0.07, HIP + 0.2, s=1.2)
    p.box((0.1, 0.24, 0.1), (sx, y + 0.13, HIP + 0.3), "wood_dark")    # kola bağlayan kayış
    return p.finish()


def archer():
    p = Part("M_Archer")
    legs(p, "wood_dark", "wood_dark")
    torso(p, "wood_light")
    p.box((0.08, 0.38, 0.5), (-0.1, 0, HIP + 0.02), "wood_dark")       # sadak kayışı
    hx, hz = arms(p, "wood_light", hand="skin")
    # pelerin (sırt) + kapüşon
    p.box((0.56, 0.08, 0.5), (0, 0.2, HIP + 0.04), "leaf_dark")
    p.box((0.62, 0.38, 0.12), (0, 0, NECK - 0.1), "leaf")              # omuz pelerini
    top = head(p)
    w, d, h = HEAD
    p.box((w + 0.1, d + 0.06, 0.16), (0, 0.02, top - 0.08), "leaf")
    for sx in (-1, 1):
        p.box((0.07, d + 0.04, h - 0.06), (sx * (w / 2 + 0.035), 0.03, HEAD_Z), "leaf")
    p.box((w + 0.1, 0.1, h + 0.04), (0, d / 2 + 0.04, HEAD_Z), "leaf")
    p.box((0.22, 0.2, 0.14), (0, d / 2 - 0.02, top + 0.02), "leaf")     # sivri uç (arkaya)
    p.box((0.12, 0.14, 0.1), (0, d / 2 + 0.06, top + 0.1), "leaf")
    p.box((w * 0.75, 0.05, 0.07), (0, -d / 2 - 0.01, HEAD_Z + h - 0.13), "wood")   # kâkül
    # sadak sırtta + oklar
    p.box((0.16, 0.14, 0.5), (-0.12, 0.32, HIP + 0.2), "wood")
    for dx in (-0.04, 0.03):
        p.box((0.03, 0.03, 0.16), (-0.12 + dx, 0.32, HIP + 0.7), "wood_light")
        p.box((0.06, 0.02, 0.08), (-0.12 + dx, 0.32, HIP + 0.84), "cream")
    # uzun yay: sol elde (+X), dik, eğri dışa
    x = hx + 0.03
    y = -0.12
    p.box((0.06, 0.06, 0.2), (x, y, hz - 0.1), "wood_dark")            # kabza
    pts = [(0.0, 0.1), (0.1, 0.38), (0.14, 0.62), (0.1, 0.82)]
    for sz in (1, -1):
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            rod(p, (x + x0, y, hz + sz * z0), (x + x1, y, hz + sz * z1), 0.03, "wood")
    p.box((0.015, 0.015, 1.64), (x + 0.1, y, hz - 0.82), "cream")      # kiriş
    return p.finish()


def mounted_knight():
    p = Part("M_MountedKnight")
    # --- at (tıknaz), ön −Y ---
    for sx in (-1, 1):
        for sy in (-0.42, 0.42):
            p.box((0.18, 0.2, 0.62), (sx * 0.2, sy, 0.1), "wood_light")
            p.box((0.2, 0.22, 0.1), (sx * 0.2, sy, 0), "cream")         # toynak üstü beyaz
    p.box((0.62, 1.3, 0.55), (0, 0, 0.66), "wood_light")                       # gövde
    # örtü (caparison): mavi, altın kenar, zambak
    p.box((0.68, 1.1, 0.42), (0, 0.05, 0.62), "blue")
    for sx in (-1, 1):
        p.box((0.02, 1.12, 0.05), (sx * 0.345, 0.05, 0.6), "gold")
        fleur(p, sx * 0.35, 0.05, 0.72, s=0.9, side=sx)
    p.box((0.66, 0.02, 0.05), (0, -0.505, 0.6), "gold")
    p.box((0.4, 0.5, 0.1), (0, 0.05, 1.21), "wood_dark")                 # eyer
    # boyun + kafa
    p.box((0.36, 0.4, 0.5), (0, -0.66, 0.92), "wood_light")
    p.box((0.34, 0.56, 0.32), (0, -0.86, 1.3), "wood_light")
    p.box((0.3, 0.2, 0.22), (0, -1.06, 1.28), "cream")                  # burun
    for sx in (-1, 1):
        p.box((0.05, 0.08, 0.07), (sx * 0.16, -0.98, 1.44), "dark")     # göz
        p.box((0.08, 0.08, 0.14), (sx * 0.1, -0.72, 1.62), "wood_light")  # kulak
    p.box((0.12, 0.5, 0.18), (0, -0.58, 1.3), "wood")              # yele
    p.box((0.12, 0.1, 0.1), (0, -1.0, 1.62), "wood")               # perçem
    p.box((0.38, 0.06, 0.1), (0, -0.88, 1.34), "blue")                  # yular
    # kuyruk
    p.box((0.14, 0.14, 0.4), (0, 0.68, 0.72), "wood_dark")
    p.box((0.12, 0.18, 0.2), (0, 0.72, 0.56), "wood_dark")
    # --- binici ---
    base = 1.3
    for sx in (-1, 1):
        p.box((0.16, 0.38, 0.14), (sx * 0.27, -0.08, base - 0.02), "iron_light")   # uyluk
        p.box((0.15, 0.18, 0.42), (sx * 0.38, -0.2, base - 0.4), "iron_light")     # baldır
        p.box((0.16, 0.26, 0.08), (sx * 0.38, -0.24, base - 0.48), "iron")
    tw = 0.5
    p.box((tw, 0.34, 0.5), (0, 0.05, base), "iron_light")
    p.box((0.42, 0.04, 0.42), (0, -0.14, base + 0.02), "blue")
    fleur(p, 0, -0.165, base + 0.12, s=0.9)
    p.box((0.52, 0.36, 0.06), (0, 0.05, base + 0.04), "wood_dark")
    for sx in (-1, 1):
        p.box((0.2, 0.24, 0.14), (sx * 0.34, 0.05, base + 0.4), "iron")         # omuzluk
        p.box((0.14, 0.16, 0.3), (sx * 0.34, 0.05, base + 0.12), "iron_light")
        p.box((0.13, 0.15, 0.1), (sx * 0.34, -0.02, base + 0.04), "iron")
    hz = base + 0.52
    w, d, h = 0.42, 0.38, 0.42
    p.box((w, d, h), (0, 0.05, hz), "iron_light")
    p.box((0.3, 0.03, 0.06), (0, 0.05 - d / 2 - 0.005, hz + 0.22), "dark")
    for dx in (-0.07, 0.07):
        p.box((0.03, 0.03, 0.12), (dx, 0.05 - d / 2 - 0.005, hz + 0.06), "dark")
    p.box((0.08, 0.2, 0.18), (0, 0.08, hz + h), "blue")                     # tüy
    p.box((0.08, 0.2, 0.14), (0, 0.22, hz + h + 0.06), "blue")
    # mızrak: sağ elde (−X), ileri (−Y) uzanır
    lx, lz = -0.34, base + 0.1
    p.cyl(0.045, 2.4, (lx, 0.45, lz), "wood_light", segments=6, rot=rx(math.radians(90)))
    p.cyl(0.13, 0.22, (lx, -0.05, lz), "blue", segments=6, radius_top=0.05, rot=rx(math.radians(90)))
    p.cyl(0.06, 0.3, (lx, -1.95, lz), "iron_light", segments=6, radius_top=0.0, rot=rx(math.radians(90)))
    return p.finish()


def hunting_hound():
    p = Part("M_HuntingHound")
    # gövde 0.75 uzun, omuz ~0.48
    for sx in (-1, 1):
        for sy, col in ((-0.24, "cream"), (0.24, "wood")):
            p.box((0.1, 0.11, 0.26), (sx * 0.1, sy, 0.0), col)
    p.box((0.3, 0.72, 0.24), (0, 0, 0.24), "wood")
    p.box((0.24, 0.3, 0.06), (0, -0.16, 0.22), "cream")                  # göğüs/karın
    p.box((0.26, 0.2, 0.26), (0, -0.33, 0.22), "cream")                  # göğüs önü
    p.box((0.32, 0.2, 0.06), (0, -0.28, 0.46), "blue")                   # tasma
    p.box((0.06, 0.03, 0.06), (0, -0.39, 0.42), "gold")
    # kafa
    p.box((0.3, 0.28, 0.28), (0, -0.38, 0.5), "wood")
    p.box((0.08, 0.2, 0.2), (0, -0.44, 0.55), "cream")                  # alın şeridi
    p.box((0.18, 0.18, 0.13), (0, -0.56, 0.5), "cream")                  # ağız
    p.box((0.08, 0.04, 0.06), (0, -0.66, 0.58), "dark")                  # burun
    for sx in (-1, 1):
        p.box((0.04, 0.03, 0.05), (sx * 0.08, -0.53, 0.66), "dark")      # göz
        p.box((0.06, 0.12, 0.22), (sx * 0.17, -0.36, 0.48), "wood_dark")  # sarkık kulak
    # kuyruk (yukarı kalkık)
    p.box((0.06, 0.06, 0.12), (0, 0.38, 0.44), "wood")
    p.box((0.06, 0.06, 0.12), (0, 0.42, 0.54), "cream")
    return p.finish()


BUILDERS = {
    "peasant_male": male_peasant,
    "peasant_female": female_peasant,
    "merchant": merchant,
    "farmer": farmer,
    "minstrel": minstrel,
    "peasant_hat": peasant_hat,
    "peasant_basket": peasant_basket,
    "militia": militia,
    "town_guard": town_guard,
    "shield_knight": shield_knight,
    "archer": archer,
    "mounted_knight": mounted_knight,
    "hunting_hound": hunting_hound,
}
