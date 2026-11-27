"""Kral heykelinin Blender kaynağı (krallık-haritası dalından korundu). Çıktı: Art/City/Medieval/B_MedKingStatue.fbx,
renkleri kendi paletinden (KingStatuePalette.png / Mat_KingStatue). Bu daldaki build_medieval paletinde heykel renkleri
yok; yeniden üretmek için aşağıdaki STATUE_COLORS paletine eklenmeli ve modül MODULES'a ("kit_castle": "B_") girmeli."""
from build_medieval import Part
from kit_props import ry, tbox

STATUE_COLORS = [
    ("bronze", (0.325, 0.455, 0.424)),
    ("bronze_dark", (0.278, 0.408, 0.38)),
    ("bronze_light", (0.404, 0.518, 0.471)),
    ("brass", (0.827, 0.639, 0.298)),
    ("plinth", (0.718, 0.678, 0.588)),
    ("plinth_light", (0.835, 0.776, 0.651)),
    ("plinth_dark", (0.655, 0.616, 0.537)),
    ("plinth_top", (0.882, 0.812, 0.686)),
]


# ---------- kral heykeli ----------

def king_statue():
    """Taçlı bronz kral (Emberwatch referansı): uzun pelerin, bir eli altın asada; kademeli kumtaşı kaide + pirinç
    plaket. Figür kutulardan (voxel), yüz −Y'ye (kapıya, oyuncuya) bakar. Ölçü referansın STATUE_K katı."""
    p = Part("M_KingStatue")
    K = STATUE_K

    def b(x, y, z, w, h, d, color, tilt=0.0):
        # referans: y yukarı, +z ön, (x, y, z) kutu merkezi; tilt ön eksen etrafında
        tbox(p, (w * K, d * K, h * K), (x * K, -z * K, y * K), color, ry(-tilt) if tilt else None)

    for y, w, h, c in ((0.14, 3.85, 0.28, "plinth"), (0.45, 3.26, 0.36, "plinth_light"),
                       (1.02, 2.26, 0.83, "plinth_dark"), (1.53, 2.56, 0.21, "plinth_top")):
        b(0, y, 0, w, h, w * 0.78, c)
    b(0, 1.09, 0.9, 0.95, 0.37, 0.08, "brass")                               # plaket
    for x in (-0.29, 0.29):
        b(x, 1.93, 0.06, 0.47, 0.58, 0.69, "bronze")                         # bacaklar
    b(0, 2.68, 0, 1.22, 1.28, 0.8, "bronze")                                 # gövde
    b(0, 2.64, -0.42, 1.5, 1.78, 0.23, "bronze_dark")                        # pelerin
    b(0, 3.43, 0.03, 1.25, 0.49, 0.83, "bronze_light")                       # yaka
    b(0, 3.98, 0.12, 0.7, 0.79, 0.64, "bronze")                              # baş
    b(0, 3.8, 0.46, 0.47, 0.42, 0.12, "bronze_dark")                         # sakal
    b(0, 4.38, 0.1, 0.82, 0.21, 0.76, "brass")                               # taç
    for sx in (-1, 1):
        for sz in (-1, 1):
            b(sx * 0.32, 4.57, 0.11 + sz * 0.26, 0.16, 0.28, 0.16, "brass")  # taç uçları
    b(-0.77, 3.07, 0, 0.34, 0.91, 0.43, "bronze", -0.16)                     # sol kol (aşağıda)
    b(0.77, 3.3, 0.21, 0.38, 0.45, 0.73, "bronze")                           # sağ kol (asaya uzanır)
    b(0.84, 2.99, 0.52, 0.11, 2.03, 0.12, "brass")                           # asa
    b(0.84, 4.11, 0.52, 0.28, 0.28, 0.28, "brass")                           # asa topuzu
    return p.finish()


STATUE_K = 1.9          # referans biriminden kit birimine: ~9 birim boy (oyunda ~6), avlunun odağı
