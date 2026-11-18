"""Bekçi / güvenlik görevlisi (H_Guard): ilk dakikaların zayıf düşmanı. Copsuz, yumrukla durdurmaya çalışır.

zorse-blender deposuna taşınacak. build_police_variants.py ile aynı kurucu (M), palet ve ölçü: polis boyu,
yüz +Y, pivot taban ortası z=0. Renkler polisin mavisinden ve robot köpeğin grisinden ayrılsın diye
bej gömlek, koyu kahve pantolon/kasket, turuncu-sarı reflektörlü yelek.

Çalıştırma: blender -b --factory-startup --python build_guard.py -- [--export]
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_police_variants as bpv  # noqa: E402

bpv.PAL.update({
    "tan": (0, 3),        # e8c98f gömlek
    "darkbrown": (1, 1),  # 6e3a2b pantolon, kasket
    "brown": (1, 5),      # 8a5a3c kemer
    "orange": (1, 0),     # e59a2b yelek kenarı
})


def guard():
    m = bpv.M()
    shirt, pants, vest = "tan", "darkbrown", "yellow"
    # bacaklar + ayakkabı
    for sx in (-1, 1):
        x0, x1 = (0.03, 0.16) if sx > 0 else (-0.16, -0.03)
        m.box(x0, -0.08, 0.0, x1, 0.12, 0.07, "black")
        m.box(x0, -0.07, 0.07, x1, 0.07, 0.50, pants)
    # kemer + gövde (gömlek) + reflektörlü yelek
    m.box(-0.20, -0.12, 0.48, 0.20, 0.12, 0.55, "brown")
    m.box(-0.20, -0.11, 0.55, 0.20, 0.11, 0.90, shirt)
    m.box(-0.21, -0.12, 0.56, 0.21, 0.12, 0.86, vest)
    m.box(-0.215, -0.125, 0.66, 0.215, 0.125, 0.70, "lightgrey")  # reflektör şerit
    m.box(-0.215, -0.125, 0.56, 0.215, 0.125, 0.58, "orange")     # yelek alt kenarı
    m.box(-0.06, 0.12, 0.74, 0.06, 0.13, 0.82, "white")           # yaka kartı
    # el feneri (belde, sol) + düdük ipi
    m.box(-0.24, -0.02, 0.44, -0.20, 0.06, 0.56, "nearblack")
    m.box(0.07, 0.12, 0.70, 0.09, 0.13, 0.86, "red")
    # kollar yanda, sarkık; iri yumruklar (ten)
    for sx in (-1, 1):
        x0, x1 = (0.21, 0.30) if sx > 0 else (-0.30, -0.21)
        m.box(x0, -0.06, 0.60, x1, 0.06, 0.88, shirt)
        m.box(x0 - 0.005, -0.065, 0.80, x1 + 0.005, 0.065, 0.89, vest)  # omuz
        m.box(x0 - 0.01, -0.07, 0.49, x1 + 0.01, 0.07, 0.60, "skin")    # yumruk
    # kafa + kasket (siperlik önde)
    z0 = 0.90
    m.box(-0.16, -0.14, z0, 0.16, 0.16, z0 + 0.27, "skin")
    m.box(-0.17, -0.15, z0 + 0.20, 0.17, 0.15, z0 + 0.33, "darkbrown")
    m.box(-0.15, 0.15, z0 + 0.20, 0.15, 0.27, z0 + 0.23, "darkbrown")
    m.box(-0.04, 0.15, z0 + 0.25, 0.04, 0.16, z0 + 0.30, "yellow")  # kasket arması
    m.box(-0.10, 0.16, z0 + 0.06, 0.10, 0.17, z0 + 0.09, "darkbrown")  # bıyık
    return m.build("H_Guard")


def main(do_export):
    old = bpy.data.objects.get("H_Guard")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    obj = guard()
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(obj.name, "üçgen", tris, "boyut", tuple(round(d, 2) for d in obj.dimensions))
    if do_export:
        bpv.export(obj)
    return obj


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    main("--export" in argv)
