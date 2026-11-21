"""Kale zemininin karışım haritası (Bahar, 2026-11-21: "kusursuz daire, keskin geçiş kötü görünüyor").
Tek zemin, her noktada çimen/toprak/taş/kaldırım oranı. Kenarlar gürültüyle dalgalı, geçişler yumuşak; taşlar shader'da
yükseklikle karışır (kenarda tek tük taş çimene taşar). build_castle_layout.py çağırır.

Kanallar (doğrusal, sRGB değil): R toprak, G arnavut kaldırımı, B kaldırım taşı (flagstone), A gölge
(geniş açık-koyu lekeler × döşeli alanın kenarındaki koyuluk). Çimen = 1 − R − G − B.
"""
import math

import numpy as np
from PIL import Image

PX = 0.125            # birim / piksel
SOFT = 0.22           # geçiş yumuşaklığı (birim)
SOFT_DIRT = 0.9       # toprak yol kenarı: daha geniş
EDGE_NOISE = 1.3      # kenar dalgalanması (birim)
CH = {"Grass": None, "Dirt": 0, "Cobble": 1, "Flagstone": 2}


def _noise(w, h, cell_px, rng):
    """Değer gürültüsü: kaba rastgele ızgara, bikübik büyütülür (0..1)."""
    gw, gh = max(2, int(w / cell_px) + 3), max(2, int(h / cell_px) + 3)
    g = (rng.random((gh, gw)) * 255).astype(np.uint8)
    im = Image.fromarray(g).resize((int(gw * cell_px), int(gh * cell_px)), Image.BICUBIC)
    return np.asarray(im, dtype=np.float32)[:h, :w] / 255.0


def _blur(a, r_px):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    from PIL import ImageFilter
    return np.asarray(im.filter(ImageFilter.GaussianBlur(r_px)), dtype=np.float32) / 255.0


def build(out_path, rect, base_rects, paths, discs, seed=20261121):
    """rect: (x0, z0, x1, z1) haritanın zemin dikdörtgeni. base_rects: [(tex, x0, z0, x1, z1, dalgalı mı)].
    paths: [(tex, yarı genişlik, [(x, z), ...])]. discs: [(tex, x, z, r)] (sırayla üst üste boyanır)."""
    x0, z0, x1, z1 = rect
    w, h = int(round((x1 - x0) / PX)), int(round((z1 - z0) / PX))
    rng = np.random.default_rng(seed)
    xs = x0 + (np.arange(w) + 0.5) * PX
    zs = z1 - (np.arange(h) + 0.5) * PX               # görüntü satırı 0 = kuzey
    X, Z = np.meshgrid(xs, zs)
    # kenar gürültüsü: iki oktav (geniş dalga + küçük kırıntı), −1..1
    n = (_noise(w, h, 4.0 / PX, rng) * 0.65 + _noise(w, h, 1.2 / PX, rng) * 0.35) * 2 - 1
    W = np.zeros((h, w, 3), np.float32)               # toprak, arnavut, kaldırım (çimen kalan)

    fine = _noise(w, h, 0.5 / PX, rng) * 2 - 1                 # ince kırıntı: toprak kenarı çimene dağılsın

    def paint(tex, sdf):
        if tex == "Dirt":   # toprak→çimen geniş ve dağınık geçiş (Bahar: "gerçekçi değil, yumuşak olsun")
            soft = SOFT_DIRT
            sdf = sdf + fine * 0.45
        else:
            soft = SOFT
        a = 1.0 - np.clip((sdf + n * EDGE_NOISE + soft) / (2 * soft), 0, 1)
        a = a * a * (3 - 2 * a)
        target = np.zeros(3, np.float32)
        if CH[tex] is not None:
            target[CH[tex]] = 1.0
        W[:] = W * (1 - a[..., None]) + target * a[..., None]

    for tex, rx0, rz0, rx1, rz1, wavy in base_rects:
        dx = np.maximum(rx0 - X, X - rx1)
        dz = np.maximum(rz0 - Z, Z - rz1)
        sdf = np.maximum(dx, dz)
        if not wavy:
            a = (sdf <= 0).astype(np.float32)
            target = np.zeros(3, np.float32)
            if CH[tex] is not None:
                target[CH[tex]] = 1.0
            W[:] = W * (1 - a[..., None]) + target * a[..., None]
        else:
            paint(tex, sdf)
    for tex, hw, pts in paths:
        d = np.full(X.shape, 1e9, np.float32)
        for (ax, az), (bx, bz) in zip(pts, pts[1:]):
            vx, vz = bx - ax, bz - az
            L2 = vx * vx + vz * vz or 1e-9
            t = np.clip(((X - ax) * vx + (Z - az) * vz) / L2, 0, 1)
            d = np.minimum(d, np.hypot(X - (ax + vx * t), Z - (az + vz * t)))
        paint(tex, d - hw)
    for tex, cx, cz, r in discs:
        paint(tex, np.hypot(X - cx, Z - cz) - r)

    paved = W.sum(axis=2)
    macro = _noise(w, h, 18.0 / PX, rng) * 0.65 + _noise(w, h, 6.0 / PX, rng) * 0.35
    shade = 0.8 + 0.2 * np.clip((macro - 0.15) / 0.7, 0, 1)
    rim = np.clip(paved - _blur(paved, 0.9 / PX), 0, 1)    # döşeli alanın kenarı (içeride): hafif çukur görünür
    shade *= 1.0 - 0.9 * rim
    rgba = np.concatenate([W, shade[..., None]], axis=2)
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(out_path)
    cobble = W[..., 1].copy()

    def cobble_at(x, z):
        """Taş (arnavut) ağırlığı 0..1: 3D yol taşlarının yerleşimi için."""
        i = int((x - x0) / PX)
        j = int((z1 - z) / PX)
        if i < 0 or j < 0 or i >= w or j >= h:
            return 0.0
        return float(cobble[j, i])
    return cobble_at
