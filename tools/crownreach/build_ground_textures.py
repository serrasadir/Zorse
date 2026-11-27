"""Crownreach zemin dokuları: stilize, düşük kontrastlı çim ve toprak (low-poly objelerle aynı dil).

Düz lekeler (posterize değer gürültüsü), seyrek ot tutamı / çakıl; iz, ayak izi, çapraz desen yok. Dokular kenardan
kenara döşenir (periyodik gürültü). Çıktı: Assets/_Project/Art/Crownreach/Ground_Grass.png, Ground_Dirt.png
(GroundSplat: çim 4, toprak 7 birimde bir tekrar).

Çalıştırma: python3 tools/crownreach/build_ground_textures.py   (PIL + numpy)
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

SIZE = 512
OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Art/Crownreach"
GRASS = np.array([106, 138, 56], np.float32)
DIRT = np.array([204, 177, 140], np.float32)


def periodic_noise(cells, rng):
    """Kenardan kenara döşenen değer gürültüsü (0..1): rastgele ızgara 3×3 döşenip bikübik büyütülür, ortası alınır."""
    g = (rng.random((cells, cells)) * 255).astype(np.uint8)
    big = Image.fromarray(np.tile(g, (3, 3))).resize((SIZE * 3, SIZE * 3), Image.BICUBIC)
    return np.asarray(big, np.float32)[SIZE:2 * SIZE, SIZE:2 * SIZE] / 255.0


def bands(n, levels, soft):
    """Gürültüyü birkaç düz tona indirir (low-poly leke), kenarları hafif yumuşatılır."""
    q = np.floor(np.clip(n, 0, 0.999) * levels) / (levels - 1) - 0.5
    im = Image.fromarray(((q + 0.5) * 255).astype(np.uint8))
    tiled = Image.fromarray(np.tile(np.asarray(im), (3, 3))).filter(ImageFilter.GaussianBlur(soft))
    return np.asarray(tiled, np.float32)[SIZE:2 * SIZE, SIZE:2 * SIZE] / 255.0 - 0.5


def stamp(img, x, y, mask, color, alpha):
    """Küçük maskeyi kenardan taşarsa karşı kenara sararak boyar."""
    h, w = mask.shape
    ys = (np.arange(h) + y) % SIZE
    xs = (np.arange(w) + x) % SIZE
    region = img[np.ix_(ys, xs)]
    a = mask[..., None] * alpha
    img[np.ix_(ys, xs)] = region * (1 - a) + color * a


def grass(rng):
    macro = bands(periodic_noise(5, rng) * 0.7 + periodic_noise(11, rng) * 0.3, 4, 3)
    fine = periodic_noise(48, rng) - 0.5
    shade = 1 + macro * 0.07 + fine * 0.025
    img = GRASS[None, None] * shade[..., None]
    img[..., 0] += macro * 8             # açık lekeler biraz sarıya
    blade = np.zeros((7, 5), np.float32)  # ot tutamı: üç ince yaprak
    for (x0, x1) in ((0, 2), (2, 2), (4, 2)):
        for y in range(7):
            t = y / 6
            xx = round(x0 + (x1 - x0) * t)
            if y >= (2 if x0 != 2 else 0):
                blade[y, xx] = 1.0
    for _ in range(420):
        x, y = rng.integers(0, SIZE, 2)
        dark = rng.random() < 0.55
        color = GRASS * (0.86 if dark else 1.13)
        stamp(img, x, y, blade, color, 0.55)
    return img


def dirt(rng):
    macro = bands(periodic_noise(4, rng) * 0.65 + periodic_noise(9, rng) * 0.35, 4, 4)
    fine = periodic_noise(64, rng) - 0.5
    img = DIRT[None, None] * (1 + macro * 0.08 + fine * 0.02)[..., None]
    yy, xx = np.mgrid[-4:5, -4:5]
    for _ in range(70):                   # seyrek çakıl: üstü açık, altı koyu küçük elips
        x, y = rng.integers(0, SIZE, 2)
        rx, ry = rng.uniform(1.6, 3.6), rng.uniform(1.2, 2.6)
        body = ((xx / rx) ** 2 + (yy / ry) ** 2 <= 1).astype(np.float32)
        shadow = ((xx / rx) ** 2 + ((yy - 1) / ry) ** 2 <= 1).astype(np.float32) * (1 - body)
        stamp(img, x, y, shadow, DIRT * 0.8, 0.5)
        stamp(img, x, y, body, DIRT * rng.uniform(0.9, 1.12), 0.8)
    return img


def main():
    rng = np.random.default_rng(20261127)
    for name, fn in (("Ground_Grass", grass), ("Ground_Dirt", dirt)):
        img = np.clip(fn(rng), 0, 255).astype(np.uint8)
        Image.fromarray(img, "RGB").save(OUT / f"{name}.png")
        print(name, img.reshape(-1, 3).mean(0).round())


if __name__ == "__main__":
    main()
