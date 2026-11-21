"""Bahar'ın çimen dokusunu (ChatGPT, 2026-11-21) zemin döşemesine çevirir: taş patikayı siler (çevredeki çimenle
doldurur), kenarları dikişsiz yapar, 512'ye indirir → Assets/_Project/Art/City/Textures/Med_Grass.png.
Çalıştırma: python prepare_grass_texture.py <kaynak.jpg>"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Textures", "Med_Grass.png"))


def stone_mask(a):
    """Taşlar: düşük doygunluk (grimsi) bölgeler; genişletilip yumuşatılır."""
    f = a.astype(np.float32) / 255.0
    mx, mn = f.max(axis=2), f.min(axis=2)
    sat = (mx - mn) / np.maximum(mx, 1e-4)
    m = ((sat < 0.54) & (mx > 0.4)).astype(np.uint8) * 255   # taşlar çimenden belirgin daha az doygun (≈0.47 vs 0.6+)
    im = Image.fromarray(m).filter(ImageFilter.MedianFilter(7)).filter(ImageFilter.MaxFilter(15))
    return np.asarray(im) > 0


def fill_from_offsets(a, mask):
    """Maskeli pikselleri aynı görüntünün kaydırılmış (maskesiz) bir yerinden kopyalar: gerçek çimen dokusu."""
    out = a.copy()
    todo = mask.copy()
    h, w = mask.shape
    rng = np.random.default_rng(7)
    for _ in range(40):
        if not todo.any():
            break
        dy, dx = int(rng.integers(h // 5, h - h // 5)), int(rng.integers(w // 5, w - w // 5))
        src = np.roll(np.roll(a, dy, axis=0), dx, axis=1)
        src_m = np.roll(np.roll(mask, dy, axis=0), dx, axis=1)
        ok = todo & ~src_m
        out[ok] = src[ok]
        todo &= ~ok
    # dolgu kenarı: yumuşak geçiş
    soft = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6)), np.float32) / 255.0
    return (a * (1 - soft[..., None]) + out * soft[..., None]).astype(np.uint8)


def make_seamless(a):
    """Yarım kaydırılmış kopyayla çapraz geçiş: kenarlarda kopya, ortada asıl — döşenince dikiş görünmez."""
    h, w, _ = a.shape
    shifted = np.roll(np.roll(a, h // 2, axis=0), w // 2, axis=1).astype(np.float32)
    y = np.abs(np.linspace(-1, 1, h))[:, None]
    x = np.abs(np.linspace(-1, 1, w))[None, :]
    edge = np.clip(np.maximum(x, y) * 1.6 - 0.6, 0, 1)        # merkezde 0, kenarda 1
    edge = edge * edge * (3 - 2 * edge)
    return (a.astype(np.float32) * (1 - edge[..., None]) + shifted * edge[..., None]).astype(np.uint8)


def main(src):
    a = np.asarray(Image.open(src).convert("RGB"))
    mask = stone_mask(a)
    print("taş pikseli: %.1f%%" % (mask.mean() * 100))
    a = fill_from_offsets(a, mask)
    a = make_seamless(a)
    Image.fromarray(a).resize((512, 512), Image.LANCZOS).save(OUT)
    print("yazıldı", OUT)


if __name__ == "__main__":
    main(sys.argv[1])
