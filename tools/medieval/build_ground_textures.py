"""Kale haritası zemin dokuları (tekrar eden, düz renkli low-poly görünüm): python build_ground_textures.py
Çıktı: Assets/_Project/Art/City/Textures/Med_{Grass,Dirt,Cobble,Flagstone}.png (Repeat + mipmap: CityKitImportSettings).
Renkler MedievalPalette ile aynı (build_medieval.PALETTE). Bir doku = 4×4 birim zemin (CastleSceneBuilder.GroundTile)."""
import os
import random

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Textures"))
S = 256


def c(rgb):
    return tuple(int(v * 255) for v in rgb)


COL = {
    "stone": c((0.62, 0.60, 0.57)), "stone_dark": c((0.45, 0.44, 0.43)), "stone_light": c((0.78, 0.76, 0.72)),
    "moss": c((0.47, 0.62, 0.25)), "grass": c((0.52, 0.74, 0.30)), "leaf": c((0.42, 0.66, 0.24)),
    "leaf_dark": c((0.30, 0.50, 0.18)), "dirt": c((0.68, 0.48, 0.30)), "wood": c((0.66, 0.42, 0.22)),
    "wood_light": c((0.80, 0.58, 0.33)), "white": c((0.97, 0.97, 0.95)), "gold": c((0.98, 0.76, 0.20)),
}


def wrap_rect(d, x, y, w, h, fill):
    """Kenarı aşan dikdörtgeni karşı kenara sarar (dikişsiz tekrar)."""
    for ox in (-S, 0, S):
        for oy in (-S, 0, S):
            d.rectangle([x + ox, y + oy, x + w + ox, y + h + oy], fill=fill)


def grass():
    im = Image.new("RGB", (S, S), COL["grass"])
    d = ImageDraw.Draw(im)
    r = random.Random(1)
    for _ in range(14):
        wrap_rect(d, r.randrange(S), r.randrange(S), r.randint(18, 40), r.randint(12, 28), COL["leaf"])
    for _ in range(26):
        wrap_rect(d, r.randrange(S), r.randrange(S), r.randint(4, 8), r.randint(2, 4), COL["leaf_dark"])
    for _ in range(4):  # küçük çiçek
        x, y = r.randrange(S), r.randrange(S)
        wrap_rect(d, x, y, 6, 6, COL["white"])
        wrap_rect(d, x + 2, y + 2, 2, 2, COL["gold"])
    return im


def dirt():
    im = Image.new("RGB", (S, S), COL["dirt"])
    d = ImageDraw.Draw(im)
    r = random.Random(2)
    for _ in range(16):
        wrap_rect(d, r.randrange(S), r.randrange(S), r.randint(16, 34), r.randint(8, 18), COL["wood_light"])
    for _ in range(10):
        wrap_rect(d, r.randrange(S), r.randrange(S), r.randint(10, 22), r.randint(6, 12), COL["wood"])
    for _ in range(18):
        wrap_rect(d, r.randrange(S), r.randrange(S), r.randint(3, 6), r.randint(3, 5), COL["stone"])
    return im


def cobble():
    im = Image.new("RGB", (S, S), COL["stone_dark"])
    d = ImageDraw.Draw(im)
    r = random.Random(3)
    n = 8
    cell = S // n
    for j in range(n):
        off = (cell // 2) if j % 2 else 0
        for i in range(n + 1):
            x = i * cell + off + r.randint(-2, 2)
            y = j * cell + r.randint(-2, 2)
            col = r.choice((COL["stone"], COL["stone"], COL["stone_light"]))
            wrap_rect(d, x + 3, y + 3, cell - 7, cell - 7, col)
    return im


def flagstone():
    im = Image.new("RGB", (S, S), COL["stone_dark"])
    d = ImageDraw.Draw(im)
    r = random.Random(4)
    y = 0
    rows = [64, 64, 64, 64]
    for h in rows:
        x = r.randint(0, 40)
        while x < S + 40:
            w = r.randint(50, 90)
            col = r.choice((COL["stone_light"], COL["stone_light"], COL["stone"]))
            wrap_rect(d, x + 3, y + 3, w - 6, h - 6, col)
            x += w
        y += h
    for _ in range(5):
        wrap_rect(d, r.randrange(S), r.randrange(S), 8, 4, COL["moss"])
    return im


os.makedirs(OUT, exist_ok=True)
for name, fn in (("Grass", grass), ("Dirt", dirt), ("Cobble", cobble), ("Flagstone", flagstone)):
    path = os.path.join(OUT, "Med_%s.png" % name)
    fn().save(path)
    print("yazıldı:", path)
