# Kit sprite'larının 9-slice kenar payları (piksel: sol, alt, sağ, üst) + boyut → Assets/_Project/Art/UI/Kit/kit_borders.json.
# Unity tarafı: Tools → Blob.io → Arayüz Kitini Kur bu dosyayı okur.
import glob, json, os
from PIL import Image

KIT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', '_Project', 'Art', 'UI', 'Kit')
B = {}
for p in sorted(glob.glob(os.path.join(KIT, '*', '*.png'))):
    cat = os.path.basename(os.path.dirname(p)); n = os.path.basename(p)[:-4]; k = cat + '/' + n
    w, h = Image.open(p).size
    if cat == 'Buttons' and n.startswith('btn'): b = (int(h * 0.42), int(h * 0.48), int(h * 0.42), int(h * 0.40))
    elif cat in ('Pills', 'Bars'): b = (h // 2, 0, h // 2, 0)
    elif cat == 'Badges' and n != 'badge_lock': b = (int(h * 0.45), 0, int(h * 0.45), 0)
    elif cat == 'Panels' and n == 'modal_purple': b = (70, 70, 70, int(h * 0.22))
    elif cat == 'Panels': b = (min(int(h * 0.42), 70), min(int(h * 0.45), 70), min(int(h * 0.42), 70), min(int(h * 0.42), 70))
    elif cat == 'Banners' and n == 'plate_red': b = (int(h * 0.75), 0, int(h * 0.75), 0)
    elif cat == 'Banners' and n.startswith('ribbon_') and 'curved' not in n: b = (int(w * 0.108), 0, int(w * 0.108), 0)  # kuyruk + bant köşesi (~185 px)
    else: b = (0, 0, 0, 0)
    B[k] = list(b) + [w, h]
json.dump(B, open(os.path.join(KIT, 'kit_borders.json'), 'w'), indent=0)
for k, v in B.items():
    if v[0]: print(k, v)
