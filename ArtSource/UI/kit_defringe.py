# Sahte (boyanmış) dama tahtası arka planlı sheet'lerden kalan açık gri kırıntıları temizler:
# saydam alana komşu, açık ve renksiz pikselleri tekrar tekrar siler (koyu konturda durur).
import glob, os, sys
import numpy as np
from PIL import Image

KIT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', '_Project', 'Art', 'UI', 'Kit')
GROUPS = ['Panels', 'Slots', 'IconButtons', 'Icons']
EXTRA = ['Banners/plate_red', 'Bars/bar_fill_red', 'Bars/bar_fill_blue', 'Bars/bar_fill_yellow']

def clean(path, passes=6):
    im = np.asarray(Image.open(path).convert('RGBA')).copy()
    rgb = im[..., :3].astype(int); a = im[..., 3]
    light = (rgb.min(2) > 185) & ((rgb.max(2) - rgb.min(2)) < 22)
    removed = 0
    for _ in range(passes):
        t = a == 0
        near = np.zeros_like(t)
        near[1:, :] |= t[:-1, :]; near[:-1, :] |= t[1:, :]; near[:, 1:] |= t[:, :-1]; near[:, :-1] |= t[:, 1:]
        kill = near & light & (a > 0)
        if not kill.any(): break
        a[kill] = 0; removed += int(kill.sum())
    im[..., 3] = a; im[a == 0, :3] = 0
    Image.fromarray(im, 'RGBA').save(path)
    return removed

files = [p for g in GROUPS for p in glob.glob(os.path.join(KIT, g, '*.png'))] + [os.path.join(KIT, e + '.png') for e in EXTRA]
for p in files:
    n = clean(p)
    if n: print(os.path.relpath(p, KIT), n)
