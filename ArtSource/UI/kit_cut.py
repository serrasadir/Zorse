# ChatGPT sprite sheet'lerini parçalara ayırır: saydamlık (yoksa koyu konturdan dışarıyı silerek) → bileşenler →
# kırpılmış PNG'ler. Önce --label ile numaralı önizleme, sonra isim listesiyle kaydet.
import sys, os, json
from collections import deque
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

DL = r'C:\Users\nurba\Downloads'
SCR = r'C:\Users\nurba\AppData\Local\Temp\claude\202106cdd7988fde\5777037a-20fb-4370-8eac-308717bc509f\scratchpad\kit'
os.makedirs(SCR, exist_ok=True)
SHEETS = {
    'S1': ('Parlak Renkli Oyun Butonları Sprite Seti.png', 0),
    'S2': ('Parlak Çizgi Film Oyun Arayüzü Öğeleri.png', 0),
    'S2b': ('Parlak Altın ve Kırmızı Kurdele Arayüz Seti.png', 0),
    'S3': ('Parlak Karikatür Oyun Arayüzü İkonları.png', 0),
    'S3b': ('Kalın Çizgili Beyaz Arayüz İkonları.png', 0),
    'S4a': ('Renkli Aksiyon İkonları Sticker Seti.png', (4, 2)),
    'S4b': ('Parlak Chibi Yetenek İkonları Gridi.png', (3, 3)),
}

def cut_fake_bg(rgb):
    a = np.asarray(rgb).astype(int); H, W, _ = a.shape
    mx, mn = a.max(2), a.min(2)
    bg = (mn > 200) & ((mx - mn) < 14)
    seen = np.zeros((H, W), bool); q = deque()
    for x in range(W):
        for y in (0, H - 1):
            if bg[y, x] and not seen[y, x]: seen[y, x] = True; q.append((y, x))
    for y in range(H):
        for x in (0, W - 1):
            if bg[y, x] and not seen[y, x]: seen[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and not seen[ny, nx] and bg[ny, nx]:
                seen[ny, nx] = True; q.append((ny, nx))
    alpha = np.where(seen, 0, 255).astype(np.uint8)
    # kenar yumuşatma: alfa maskesini hafif bulanıklaştır, sonra sadece kontur kenarında etkili olsun
    am = Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(0.7))
    out = rgb.convert('RGBA'); out.putalpha(am)
    return out

def load(key):
    fn, _ = SHEETS[key]
    im = Image.open(os.path.join(DL, fn))
    if im.mode != 'RGBA':
        im = cut_fake_bg(im.convert('RGB'))
    im = im.convert('RGBA')
    a = np.asarray(im).copy()
    al = a[..., 3]
    al[al >= 245] = 255           # %99 opak → tam opak
    al[al <= 8] = 0
    a[al == 0, :3] = 0            # görünmez pikseldeki renk bilgisini sil (parıltı artığı)
    return Image.fromarray(a, 'RGBA')

def components(im, dil):
    if isinstance(dil, tuple):  # ızgara: eşit hücreler
        cols, rows = dil; out = []
        for r in range(rows):
            for c in range(cols):
                out.append([im.width * c // cols, im.height * r // rows, im.width * (c + 1) // cols, im.height * (r + 1) // rows])
        return out
    S = 2
    small = im.split()[3].resize((im.width // S, im.height // S), Image.BOX)
    m = np.asarray(small) > 160
    if dil > 1:
        k = max(3, (dil // S) * 2 + 1)
        m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(k))) > 0
    H, W = m.shape; lab = np.zeros((H, W), int); boxes = []; n = 0
    for y in range(H):
        for x in range(W):
            if m[y, x] and not lab[y, x]:
                n += 1; q = deque([(y, x)]); lab[y, x] = n; x0 = x1 = x; y0 = y1 = y; cnt = 0
                while q:
                    cy, cx = q.popleft(); cnt += 1
                    x0 = min(x0, cx); x1 = max(x1, cx); y0 = min(y0, cy); y1 = max(y1, cy)
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < H and 0 <= nx < W and m[ny, nx] and not lab[ny, nx]:
                                lab[ny, nx] = n; q.append((ny, nx))
                if cnt > 30:
                    boxes.append([x0 * S, y0 * S, (x1 + 1) * S, (y1 + 1) * S])
    # satır/sütun sırası: y merkezine göre satır grupla
    boxes.sort(key=lambda b: (b[1] + b[3]) / 2)
    rows, cur = [], []
    for b in boxes:
        if cur and (b[1] + b[3]) / 2 - (cur[-1][1] + cur[-1][3]) / 2 > 60:
            rows.append(cur); cur = []
        cur.append(b)
    if cur: rows.append(cur)
    out = []
    for r in rows: out += sorted(r, key=lambda b: b[0])
    return out

def crop(im, b, pad=6):
    x0, y0, x1, y1 = b
    c = im.crop((max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad)))
    bb = c.split()[3].point(lambda v: 255 if v > 8 else 0).getbbox()
    if bb: c = c.crop((max(0, bb[0] - 2), max(0, bb[1] - 2), min(c.width, bb[2] + 2), min(c.height, bb[3] + 2)))
    return c

if __name__ == '__main__':
    mode = sys.argv[1]
    if mode == 'label':
        for key in SHEETS:
            im = load(key); bs = components(im, SHEETS[key][1])
            prev = Image.new('RGBA', im.size, (60, 66, 96, 255)); prev.alpha_composite(im)
            d = ImageDraw.Draw(prev)
            for i, b in enumerate(bs):
                d.rectangle(b, outline=(0, 255, 180, 255), width=3)
                d.text((b[0] + 6, b[1] + 4), str(i), fill=(255, 255, 0, 255), font=ImageFont.truetype('arial.ttf', 34))
            prev.convert('RGB').resize((im.width // 2, im.height // 2)).save(os.path.join(SCR, f'label_{key}.png'))
            print(key, len(bs))
    else:
        names = json.load(open(sys.argv[2], encoding='utf-8'))
        dest = sys.argv[3]
        for key, lst in names.items():
            im = load(key); bs = components(im, SHEETS[key][1])
            assert len(bs) == len(lst), (key, len(bs), len(lst))
            merged = []
            for b, n in zip(bs, lst):
                if n == '+':  # öncekiyle birleştir (iki parçaya bölünmüş ikon)
                    pb, pn = merged[-1]; merged[-1] = ([min(pb[0], b[0]), min(pb[1], b[1]), max(pb[2], b[2]), max(pb[3], b[3])], pn)
                else: merged.append((b, n))
            for b, n in merged:
                if not n: continue
                sub, name = n.split('/')
                os.makedirs(os.path.join(dest, sub), exist_ok=True)
                crop(im, b, 0 if isinstance(SHEETS[key][1], tuple) else 6).save(os.path.join(dest, sub, name + '.png'))
        print('kaydedildi')

# Izgaralı ikon sayfaları: her bağlı parça ağırlık merkezinin düştüğü hücreye ait; hücre = kendi parçalarının birleşimi
# (komşu ikonun taşan kıvılcımı karışmaz, kendi taşan alevi kesilmez).
def grid_icons(im, cols, rows):
    S = 2
    a = np.asarray(im.split()[3].resize((im.width // S, im.height // S), Image.BOX)) > 20
    H, W = a.shape; lab = np.zeros((H, W), int); comps = []; n = 0
    for y in range(H):
        for x in range(W):
            if a[y, x] and not lab[y, x]:
                n += 1; q = deque([(y, x)]); lab[y, x] = n; pts = []
                while q:
                    cy, cx = q.popleft(); pts.append((cy, cx))
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < H and 0 <= nx < W and a[ny, nx] and not lab[ny, nx]:
                                lab[ny, nx] = n; q.append((ny, nx))
                comps.append(pts)
    cells = [[] for _ in range(cols * rows)]
    for idx, pts in enumerate(comps, 1):
        ys = [p[0] for p in pts]; xs = [p[1] for p in pts]
        cy, cx = sum(ys) / len(ys), sum(xs) / len(xs)
        c = min(cols - 1, int(cx * S * cols / im.width)); r = min(rows - 1, int(cy * S * rows / im.height))
        cells[r * cols + c].append(idx)
    out = []
    arr = np.asarray(im).copy()
    for ids in cells:
        m = np.isin(lab, ids)
        mfull = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((im.width, im.height), Image.NEAREST).filter(ImageFilter.MaxFilter(3))) > 0
        sub = arr.copy(); sub[~mfull] = 0
        img = Image.fromarray(sub, 'RGBA'); bb = img.split()[3].getbbox()
        out.append(img.crop((max(0, bb[0] - 2), max(0, bb[1] - 2), min(im.width, bb[2] + 2), min(im.height, bb[3] + 2))))
    return out

if __name__ == '__main__' and sys.argv[1] == 'skills':
    names = json.load(open(sys.argv[2], encoding='utf-8')); dest = sys.argv[3]
    for key, (c, r) in (('S4a', (4, 2)), ('S4b', (3, 3))):
        for img, n in zip(grid_icons(load(key), c, r), names[key]):
            sub, name = n.split('/'); img.save(os.path.join(dest, sub, name + '.png'))
    print('ikonlar tamam')
