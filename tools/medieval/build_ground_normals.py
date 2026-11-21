"""Zemin dokularından normal haritası (Bahar, 2026-11-21: "zemin çok 2D duruyor").
Parlaklık = yükseklik (taş/toprak tümseği açık, derz koyu); dikişsiz (kenarlar sarılır). Çıktı: Med_<Ad>_N.png.
Çalıştırma: python build_ground_normals.py  (PIL + numpy)"""
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Textures"))
# doku → (kabartma gücü, yükseklik yumuşatma yarıçapı px)
# Sadece GroundSplat'ın okuduğu normaller (çimen ve toprak normalsiz)
SOURCES = {"Cobble": (4.5, 2), "Flagstone": (4.0, 2)}

for name, (strength, blur) in SOURCES.items():
    src = os.path.join(TEX, f"Med_{name}.png")
    im = Image.open(src).convert("L")
    # dikişsiz bulanıklaştırma: 3x3 döşe, bulanıklaştır, ortayı kes
    w, h = im.size
    big = Image.new("L", (w * 3, h * 3))
    for i in range(3):
        for j in range(3):
            big.paste(im, (i * w, j * h))
    big = big.filter(ImageFilter.GaussianBlur(blur)).crop((w, h, 2 * w, 2 * h))
    hgt = np.asarray(big, dtype=np.float32) / 255.0
    dx = (np.roll(hgt, -1, axis=1) - np.roll(hgt, 1, axis=1)) * 0.5
    dy = (np.roll(hgt, -1, axis=0) - np.roll(hgt, 1, axis=0)) * 0.5
    nx, ny, nz = -dx * strength * 8, dy * strength * 8, np.ones_like(hgt)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    rgb = np.stack([nx / l, ny / l, nz / l], axis=-1) * 0.5 + 0.5
    out = os.path.join(TEX, f"Med_{name}_N.png")
    Image.fromarray((rgb * 255).clip(0, 255).astype(np.uint8)).save(out)
    print("yazıldı", out)
