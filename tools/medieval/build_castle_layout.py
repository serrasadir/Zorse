"""Kuşatılmış Kale haritasının yerleşimi (Unity birimi, oyun ölçeğinde). Blender gerekmez: python build_castle_layout.py
Çıktı: Assets/_Project/Art/City/Medieval/castle_layout.json → Tools/Blob.io/Kale Sahnesini Kur okur.
Blender'da önizleme: tools/medieval/preview_layout.py ('CastleLayout' sahnesi).

Düzen (Bahar'ın kuşbakışı krokisi, 2026-11-20): TEK YOL, güneyden kuzeye 4 bölge, her geçişte işaretli kapı:
  Köy (başlangıç) → çit kapısı S (sağ) → Tarla + pazar → taş kapı M (sol) → Kasaba → kale kapısı L (orta) → Burç + ejderha meydanı
Yollar yumuşak eğri (Catmull-Rom): Unity'de şerit mesh (meta.paths). Yanlarda sur + hendek, dışında ağaçlar.
Yön: modelin ön yüzü −Z (yaw 0) = güneye, oyuncunun geldiği yöne.
"""
import json
import math
import os
import random

import ground_splat

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Medieval", "castle_layout.json"))
SPLAT = "Assets/_Project/Art/Ground/Castle_Splat.png"   # zemin karışım haritası (ground_splat.py)

SCALE = 0.7                 # build_medieval.GAME_SCALE
SEG = 3.0 * SCALE           # duvar parçası boyu
HALF = 56.0                 # harita sınırı
SIDE = 24.0                 # oynanan genişlik yarısı
START = (0.0, -50.0)
MOAT = (SIDE + 1.6, SIDE + 4.6)   # hendek şeridi (x aralığı, iki yanda)

# Bölge sınırları (güneyden kuzeye): (z, duvar öneki, kapı mesh'i, kapı genişliği, kapı x, kapı ölçeği)
LINES = [
    (-24.0, "B_MedFence", "B_MedFenceGate", 4.28, 9.0, 1.0),     # genişlikler Blender'dan ölçüldü (oyun birimi, ölçeksiz)
    (6.0, "B_MedStoneWall", "B_MedGateClosed", 2.73, -9.0, 1.4),
    (32.0, "B_MedCastleWall", "B_MedMainGate", 6.455, 0.0, 1.0),
]
KEEP = (0.0, 51.5)
# Öneri B (Bahar seçti, 2026-11-21): her bölgenin ortasında yuvarlak meydan, binalar çevresinde halka halinde meydana bakar.
# (x, z, yarıçap, zemin dokusu)
PLAZAS = [(0.0, -50.5, 3.0, "Cobble"), (0.0, -9.0, 6.0, "Dirt"), (0.0, 19.0, 6.0, "Flagstone"), (0.0, 43.0, 5.5, "Cobble")]
ARENA = PLAZAS[3][:3]       # ejderha meydanı

rnd = random.Random(20261121)
items = []
taken = []  # (x, z, yarıçap)


def put(mesh, x, z, yaw=0.0, s=1.0, y=0.0):
    items.append({"mesh": mesh, "x": round(x, 3), "y": round(y, 3), "z": round(z, 3), "yaw": round(yaw % 360, 2), "s": s})


def free(x, z, r):
    return all((x - tx) ** 2 + (z - tz) ** 2 >= (r + tr) ** 2 for tx, tz, tr in taken)


def take(x, z, r):
    taken.append((x, z, r))


def place(mesh, x, z, yaw=0.0, r=1.0, s=1.0):
    put(mesh, x, z, yaw, s=s)
    take(x, z, r)


# ---------- yollar: kontrol noktaları → yumuşak eğri ----------
def catmull(points, step=0.5, loop=False):
    pts = list(points)
    if loop:
        ext = [pts[-1]] + pts + [pts[0], pts[1]]
    else:
        ext = [pts[0]] + pts + [pts[-1]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        n = max(2, int(math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / step))
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * (2 * p1[j] + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    if not loop:
        out.append(pts[-1])
    return [(round(x, 3), round(z, 3)) for x, z in out]


# (doku, yarı genişlik, kontrol noktaları, halka mı)
# Köy (Bahar'ın referans köy haritası, 2026-11-21): ortada büyük yapıyı dolanan oval taş yol, ondan ayrılan toprak
# patikalar; patikaların iki yanında avlulu ev kümeleri. Ana yol köyde iki parça: başlangıç → oval, oval → S kapısı.
VILLAGE_LOOP = (0.0, -40.5, 6.8, 4.8)        # oval yol: merkez x, z, yarıçap x, z
PATH_DEFS = [  # kapı → meydan → kapı (meydanlar şeridin üstünde çizilir)
    ("Cobble", 2.0, [(0.0, -57.0), (0.0, -51.0), (0.0, VILLAGE_LOOP[1] - VILLAGE_LOOP[3])], False),   # başlangıç → oval
    ("Dirt", 2.4, [(0.0, VILLAGE_LOOP[1] + VILLAGE_LOOP[3]), (4.5, -31.0), (8.6, -27.5),
                   (9.0, -24.0), (8.5, -18.5),                      # S kapısı (sağ)
                   (0.0, -9.0), (-6.5, -1.5),
                   (-9.0, 6.0), (-7.5, 11.5),                       # M kapısı (sol)
                   (0.0, 19.0), (0.0, 26.0),
                   (0.0, 32.0), (0.0, ARENA[1] - ARENA[2] + 0.5)], False),   # L kapısı (orta) → ejderha meydanı
]
PATHS = [{"tex": tex, "hw": hw, "pts": catmull(pts, loop=loop), "loop": loop} for tex, hw, pts, loop in PATH_DEFS]
_lx, _lz, _rx, _rz = VILLAGE_LOOP
PATHS.append({"tex": "Cobble", "hw": 1.5, "loop": True,
              "pts": [(round(_lx + _rx * math.cos(a), 3), round(_lz + _rz * math.sin(a), 3))
                      for a in [k * 2 * math.pi / 48 for k in range(48)]]})
VILLAGE_LANES = [  # toprak patikalar (ovaldan ve ana yoldan ayrılır)
    [(-6.6, -40.8), (-14.0, -41.5), (-22.8, -40.5)],
    [(6.6, -41.2), (15.0, -42.5), (22.8, -44.0)],
    [(0.0, -51.0), (-7.5, -52.8), (-15.5, -51.5), (-22.8, -48.5)],
    [(0.0, -51.0), (8.0, -53.2), (16.0, -53.6), (22.8, -51.0)],
    [(-4.2, -36.6), (-10.5, -31.0), (-17.5, -28.6), (-22.8, -27.5)],
    [(5.8, -38.0), (14.5, -36.0), (22.8, -35.5)],
]
for lane in VILLAGE_LANES:
    PATHS.append({"tex": "Dirt", "hw": 1.0, "loop": False, "pts": catmull(lane)})
SEGS = []
for p in PATHS:
    pts = p["pts"] + ([p["pts"][0]] if p["loop"] else [])
    for a, b in zip(pts, pts[1:]):
        SEGS.append((a[0], a[1], b[0], b[1], p["hw"]))
ROAD_HW = 2.4


def dist_to_roads(x, z):
    best = 1e9
    for x0, z0, x1, z1, hw in SEGS:
        dx, dz = x1 - x0, z1 - z0
        L2 = dx * dx + dz * dz or 1e-9
        t = max(0.0, min(1.0, ((x - x0) * dx + (z - z0) * dz) / L2))
        best = min(best, math.hypot(x - (x0 + dx * t), z - (z0 + dz * t)) - (hw - ROAD_HW))
    for px, pz, pr, _ in PLAZAS:
        if math.hypot(x - px, z - pz) < pr + 0.5:
            best = min(best, 0.0)
    return best


# ---------- bölge duvarları + kapılar + fare delikleri ----------
for z, prefix, gate, gate_w, gate_x, gate_s in LINES:
    put(gate, gate_x, z, 0, s=gate_s)
    take(gate_x, z, gate_w * gate_s / 2 + 0.5)
    half_gap = gate_w * gate_s / 2 - 0.1                     # parçalar kapıya hafif biner: arada boşluk kalmaz
    hole_x = gate_x + (-4.3 if gate_x > 0 else 4.3)        # kapının hemen yanında kestirme (krokideki gibi)
    for direction in (-1, 1):
        x = gate_x + direction * half_gap
        while abs(x) < SIDE + 0.6:
            cx = x + direction * SEG / 2
            put(prefix + ("Hole" if abs(cx - hole_x) < SEG / 2 else ""), cx, z, 0)
            take(cx, z, 1.2)
            x += direction * SEG
    if prefix != "B_MedFence":                               # taş ve kale suru: iki uçta kule
        for sx in (-1, 1):
            place("B_MedCornerTower", sx * (SIDE + 0.6), z, 0, r=2.0)
    if prefix == "B_MedCastleWall":
        for sx in (-1, 1):
            place("P_MedTorchPole", gate_x + sx * 4.2, z - 1.6, 0, r=0.5)

# ---------- dış sur: yanlar, güney (başlangıç kapısıyla) ve kuzey ----------
for sx in (-1, 1):
    zz = -HALF + SEG / 2
    while zz < HALF:
        put("B_MedCastleWall", sx * (SIDE + 0.6), zz, 90 if sx > 0 else 270)
        zz += SEG
for zz in (-HALF, HALF):
    x = -SIDE - 0.6 + SEG / 2
    while x < SIDE + 0.6:
        if not (zz < 0 and abs(x) < 3.5):            # başlangıçta kemerli boşluk
            put("B_MedCastleWall", x, zz, 0 if zz > 0 else 180)
        x += SEG
    for sx in (-1, 1):
        put("B_MedCornerTower", sx * (SIDE + 0.6), zz, 0)
for sx in (-1, 1):
    put("B_MedCornerTower", sx * 3.6, -HALF, 0, s=0.7)   # başlangıç kapısının kuleleri

# ---------- hendeğin dışı: ağaç sırası (görünmez duvarın dışında, sadece görüntü) ----------
for sx in (-1, 1):
    zz = -HALF + 1.5
    while zz < HALF - 1:
        put("P_MedOakTree", sx * (MOAT[1] + rnd.uniform(1.6, 2.6)), zz + rnd.uniform(-0.6, 0.6), rnd.uniform(0, 360))
        put("P_MedOakTree", sx * (MOAT[1] + rnd.uniform(4.0, 6.0)), zz + 1.5 + rnd.uniform(-0.6, 0.6), rnd.uniform(0, 360))
        zz += 3.0


def scatter(meshes, count, z0, z1, r, road_margin=1.2, yaw_any=True, x0=-(SIDE - 1.5), x1=SIDE - 1.5, tries=6000):
    placed = 0
    for _ in range(tries):
        if placed >= count:
            break
        x, z = rnd.uniform(x0, x1), rnd.uniform(z0, z1)
        if dist_to_roads(x, z) < ROAD_HW + road_margin + r or not free(x, z, r):
            continue
        if math.hypot(x - START[0], z - START[1]) < 4:
            continue
        put(rnd.choice(meshes), x, z, rnd.uniform(0, 360) if yaw_any else rnd.choice((0, 90, 180, 270)))
        take(x, z, r)
        placed += 1


def facing_road(x, z):
    best, yaw = 1e9, 0
    for a in (0, 90, 180, 270):  # ön yüz en yakın yola
        fx, fz = -math.sin(math.radians(a)), -math.cos(math.radians(a))
        d = dist_to_roads(x + fx * 2.5, z + fz * 2.5)
        if d < best:
            best, yaw = d, a
    return yaw


def houses(meshes, count, z0, z1, r, tries=8000):
    """Binalar: yoldan uzak, birbirine değmeden; ön yüz yola."""
    placed = 0
    for _ in range(tries):
        if placed >= count:
            break
        x, z = rnd.uniform(-SIDE + r + 0.4, SIDE - r - 0.4), rnd.uniform(z0, z1)
        if dist_to_roads(x, z) < ROAD_HW + r + 0.3 or not free(x, z, r):
            continue
        place(rnd.choice(meshes), x, z, facing_road(x, z), r=r)
        placed += 1


def fence_pen(x0, z0, x1, z1, gap_side=None):
    """Çit çevrili alan (bahçe, ağıl). Parçalar kenarı boşluksuz kaplar (hafif biner); köşelerde direk.
    gap_side: 'S'/'N'/'E'/'W' tarafında tek giriş boşluğu."""
    n = math.ceil((x1 - x0) / SEG)
    for i in range(n):
        cx = x0 + (i + 0.5) * (x1 - x0) / n
        for zz, side in ((z0, "S"), (z1, "N")):
            if gap_side == side and i == n // 2:
                continue
            put("B_MedFence", cx, zz, 0)
    m = math.ceil((z1 - z0) / SEG)
    for i in range(m):
        cz = z0 + (i + 0.5) * (z1 - z0) / m
        for xx, side in ((x0, "W"), (x1, "E")):
            if gap_side == side and i == m // 2:
                continue
            put("B_MedFence", xx, cz, 90)
    for cx, cz in ((x0, z0), (x0, z1), (x1, z0), (x1, z1)):
        put("B_MedFenceCorner", cx, cz, 0)
    take((x0 + x1) / 2, (z0 + z1) / 2, max(x1 - x0, z1 - z0) / 2 + 0.3)  # içine bina girmesin


def ring(cx, cz, radius, slots, meshes, r, start=0.0, special=None, rz=None):
    """Meydanın çevresine halka (rz verilirse elips: bölge duvarlarına çarpmasın): ön yüz meydana bakar;
    yol geçen yuvalar boş kalır. special: {yuva: mesh}."""
    rz = radius if rz is None else rz
    for k in range(slots):
        a = start + k * 2 * math.pi / slots
        x, z = cx + math.cos(a) * radius, cz + math.sin(a) * rz
        if abs(x) > SIDE - r - 0.3 or dist_to_roads(x, z) < ROAD_HW + r + 0.2 or not free(x, z, r):
            continue
        yaw = math.degrees(math.atan2(-(cx - x), -(cz - z)))   # ön yüz (−Z) meydana
        mesh = special.get(k) if special and k in special else meshes[k % len(meshes)]
        place(mesh, x, z, yaw, r=r)


def grove(cx, cz, n, spread, r=1.3):
    """Ağaç kümesi (köşeler boş kalmasın)."""
    for _ in range(n * 6):
        if n <= 0:
            break
        x, z = cx + rnd.uniform(-spread, spread), cz + rnd.uniform(-spread, spread)
        if abs(x) > SIDE - 1.2 or dist_to_roads(x, z) < ROAD_HW + r or not free(x, z, r):
            continue
        place("P_MedOakTree", x, z, rnd.uniform(0, 360), r=r)
        n -= 1


# ---------- 1. köy (güney, başlangıç): oval yolun ortasında taverna, patikalarda avlulu ev kümeleri ----------
VILLAGE_HOUSES = ["B_MedCottage", "B_MedCottageRed", "B_MedHouseL", "B_MedCottageSlate", "B_MedLongHouse",
                  "B_MedCottage", "B_MedBarn", "B_MedCottageRed"]
YARD_FRONT = ["P_MedBarrel", "P_MedCrateStack", "P_MedFlowerBed", "P_MedBench", "P_MedWoodenBucket", "P_MedHayBale"]
YARD_BACK = ["P_MedWoodpile", "P_MedLaundryLine", "P_MedFlowerBed", "P_MedCrateStack", "P_MedOakTree", "P_MedHedge"]


def yaw_to(x, z, tx, tz):
    return math.degrees(math.atan2(-(tx - x), -(tz - z)))   # ön yüz (−Z) hedefe


def yard(x, z, yaw):
    """Evin yanına ön eşya, arkasına avlu eşyası (sandık, odun, çamaşır, çiçek tarhı)."""
    a = math.radians(yaw)
    fx, fz = -math.sin(a), -math.cos(a)                       # ön
    sx, sz = -fz, fx                                          # yan
    side = rnd.choice((-1, 1))
    for mesh, ox, oz, r in ((rnd.choice(YARD_FRONT), side * 3.3 * sx + 0.9 * fx, side * 3.3 * sz + 0.9 * fz, 0.7),
                            (rnd.choice(YARD_BACK), -3.5 * fx + rnd.uniform(-1.2, 1.2) * sx, -3.5 * fz + rnd.uniform(-1.2, 1.2) * sz, 0.8)):
        px, pz = x + ox, z + oz
        if abs(px) < SIDE - 1.0 and -55.2 < pz < -25.2 and dist_to_roads(px, pz) > ROAD_HW + 0.2 and free(px, pz, r):
            place(mesh, px, pz, yaw + rnd.choice((0, 90, 180, 270)) if mesh != "P_MedOakTree" else rnd.uniform(0, 360), r=r)


def lane_houses(pts, hw, spacing=4.5, depth=2.6, r=2.0):
    """Patika boyunca iki yanda yola bakan evler + avluları."""
    pts = catmull(pts)
    acc = spacing * 0.6
    for (ax, az), (bx, bz) in zip(pts, pts[1:]):
        acc += math.hypot(bx - ax, bz - az)
        if acc < spacing:
            continue
        acc = 0.0
        L = math.hypot(bx - ax, bz - az) or 1.0
        nx, nz = -(bz - az) / L, (bx - ax) / L
        for side in (1, -1):
            x, z = bx + nx * (hw + depth) * side, bz + nz * (hw + depth) * side
            if abs(x) > SIDE - r - 0.2 or not (-55.5 + r < z < -24.5 - r):
                continue
            if dist_to_roads(x, z) < ROAD_HW + r - 0.2 or not free(x, z, r):
                continue
            yaw = yaw_to(x, z, bx, bz)
            place(rnd.choice(VILLAGE_HOUSES), x, z, yaw, r=r)
            yard(x, z, yaw)


lx, lz, lrx, lrz = VILLAGE_LOOP
place("B_MedTavern", lx, lz + 0.4, 0, r=2.6)                    # ovalin ortasında büyük yapı
for tx, tz in ((-3.9, -39.6), (3.9, -41.4)):
    place("P_MedOakTree", tx, tz, rnd.uniform(0, 360), r=1.1)
place("P_MedFlowerBed", 2.6, -43.4, 0, r=0.8)
place("P_MedBench", -2.8, -43.4, 0, r=0.8)
sx_, sz_ = PLAZAS[0][:2]
place("P_MedWaterWell", sx_ + 3.9, sz_ + 0.4, 0, r=1.0)          # güney meydanında kuyu ve tezgâh
place("P_MedMarketTent", sx_ - 4.1, sz_ + 0.6, 90, r=1.2)
for lane in VILLAGE_LANES:
    lane_houses(lane, 1.0)
# oval yolun dışına da ev halkası
for k in range(18):
    a = k * 2 * math.pi / 18 + 0.12
    x, z = lx + (lrx + 4.4) * math.cos(a), lz + (lrz + 4.0) * math.sin(a)
    if abs(x) > SIDE - 2.5 or not (-53.0 < z < -27.0) or dist_to_roads(x, z) < ROAD_HW + 1.8 or not free(x, z, 2.0):
        continue
    yaw = yaw_to(x, z, lx, lz)
    place(rnd.choice(VILLAGE_HOUSES), x, z, yaw, r=2.0)
    yard(x, z, yaw)
for lane in VILLAGE_LANES:                                     # patika başlarında tabela
    x, z = lane[0]
    ox, oz = (lane[1][0] - x), (lane[1][1] - z)
    L = math.hypot(ox, oz) or 1.0
    tx, tz = x + ox / L * 2.6 + oz / L * 1.9, z + oz / L * 2.6 - ox / L * 1.9
    if free(tx, tz, 0.5) and dist_to_roads(tx, tz) > ROAD_HW - 0.4:
        place("P_MedSignpost", tx, tz, rnd.uniform(0, 360), r=0.5)
grove(-20.5, -54.0, 3, 2.5)
grove(20.5, -54.5, 3, 2.5)
grove(-21.0, -33.5, 3, 3.0)
grove(20.5, -26.8, 2, 2.0)
scatter(["P_MedChicken", "P_MedChicken", "P_MedWoodenBucket", "P_MedHayBundle", "P_MedSmallCrate", "P_MedStool",
         "P_MedFlowerPot"], 36, -55, -26, 0.45, road_margin=-0.6)

# ---------- 2. tarla + pazar: tezgâhlı meydan, köşelerde tarlalar ----------
fx, fz = PLAZAS[1][:2]
ring(fx, fz, 8.0, 10, ["P_MedMarketStall", "P_MedMarketStall", "P_MedVegetableCart"], 1.2, start=math.pi / 10)
place("P_MedWindmill", -17.0, -4.0, 20, r=3.2)
for i in range(6):                                            # ekin tarlası (sol alt); yarısı hasat edilmiş
    for j in range(7):
        if rnd.random() < 0.5:
            continue
        put("P_MedTurnip" if i % 2 else "P_MedHayBundle", -21.6 + j * 1.25, -22.2 + i * 1.3, rnd.uniform(0, 360))
take(-17.8, -19.0, 4.5)
fence_pen(12.5, -22.0, 21.5, -14.0, gap_side="W")             # meyve bahçesi (sağ alt)
for i in range(3):
    for j in range(2):
        put("P_MedOakTree", 14.5 + i * 3.0, -20.0 + j * 3.8, rnd.uniform(0, 360))
for x, z in ((15.5, 1.5), (19.5, -2.5)):
    place("B_MedBarn", x, z, facing_road(x, z), r=2.8)
for x, z in ((-20.0, -12.0), (-21.0, 2.5), (13.0, -9.5)):
    place("P_MedHaystack", x, z, rnd.uniform(0, 360), r=1.1)
scatter(["P_MedHayBale", "P_MedBarrel", "P_MedSmallCrate", "P_MedScarecrow"], 14, -23, 5, 0.8)
scatter(["P_MedChicken", "P_MedWoodenBucket", "P_MedTurnip", "P_MedStool", "P_MedFlowerPot", "P_MedCartWheel"], 36, -23, 5, 0.45, road_margin=0.2)

# ---------- 3. kasaba: şapelli meydan, çevresinde sıra evler ----------
tx, tz = PLAZAS[2][:2]
ring(tx, tz, 11.0, 12, ["B_MedTownhouse", "B_MedNarrowTownhouse", "B_MedTownhouse", "B_MedBakery"], 2.1,
     start=math.pi / 12, special={1: "B_MedChapel", 4: "B_MedTavern", 10: "B_MedBlacksmith"}, rz=8.3)
for sx in (-1, 1):                                            # yanlarda meydana bakan ev sırası
    for z in (9.5, 14.5, 19.5, 24.5, 29.0):
        x = sx * 20.5
        if free(x, z, 2.0):
            place("B_MedNarrowTownhouse" if z in (9.5, 29.0) else "B_MedTownhouse", x, z, 90 if sx < 0 else 270, r=2.0)
for k in range(3):
    place("P_MedTrainingDummy", -4.0 + k * 1.8, 29.0, 0, r=0.8)
for a in range(0, 360, 60):                                   # meydan kenarında fenerler
    ax, az = tx + 6.8 * math.cos(math.radians(a + 30)), tz + 6.8 * math.sin(math.radians(a + 30))
    if dist_to_roads(ax, az) > ROAD_HW + 0.6 or math.hypot(ax - tx, az - tz) > 6.5:
        place("P_MedTorchPole", ax, az, 0, r=0.4)
# Kasaba evlerinin arka bahçeleri (Bahar, 2026-11-21): evin arkasında yuvarlak çimenlik, içinde yenebilir çiçek + saksı.
GARDENS = []
for it in list(items):
    m = it["mesh"]
    if not m.startswith("B_Med") or any(w in m for w in ("Wall", "Fence", "Gate", "Tower", "Chapel")) or not (6.0 < it["z"] < 32.0):
        continue
    yaw = math.radians(it["yaw"])
    bx, bz = math.sin(yaw), math.cos(yaw)                     # arka yön (ön yüz −Z'nin tersi)
    gx, gz = it["x"] + bx * 3.6, it["z"] + bz * 3.6
    if abs(gx) > SIDE - 1.6 or not (7.6 < gz < 30.4) or dist_to_roads(gx, gz) < ROAD_HW + 1.6 or not free(gx, gz, 1.3):
        continue
    GARDENS.append((gx, gz, 1.7))
    take(gx, gz, 1.5)
    put("P_MedFlowerPot", gx + rnd.uniform(-0.6, 0.6), gz + rnd.uniform(-0.6, 0.6), rnd.uniform(0, 360))
    for _ in range(3):
        f = rnd.choice(["D_MedFlowersWhite", "D_MedFlowersYellow", "D_MedFlowersRed"])
        ang, rad = rnd.uniform(0, 2 * math.pi), rnd.uniform(0.6, 1.2)
        put(f, gx + math.cos(ang) * rad, gz + math.sin(ang) * rad, rnd.uniform(0, 360), s=2.2)

scatter(["P_MedBarrel", "P_MedWoodenBucket", "P_MedSmallCrate", "P_MedFlowerPot", "P_MedStool", "P_MedHayBundle",
         "P_MedChicken", "P_MedWoodenMug"], 50, 7.5, 31, 0.45, road_margin=0.0)

# ---------- 4. burç + ejderha meydanı ----------
place("B_MedCastleKeep", KEEP[0], KEEP[1], 0, r=5.0)
take(ARENA[0], ARENA[1], ARENA[2])
for a in range(0, 360, 90):                                   # meydanın köşelerinde meşale
    ax, az = ARENA[0] + (ARENA[2] + 0.9) * math.cos(math.radians(a + 45)), ARENA[1] + (ARENA[2] + 0.9) * math.sin(math.radians(a + 45))
    place("P_MedTorchPole", ax, az, 0, r=0.4)
for x in (-16.5, 16.5):
    place("B_MedBarracks", x, 47.5, 90 if x < 0 else 270, r=3.2)
for k in range(3):
    place("P_MedArcheryTarget", -19.5 + k * 2.6, 37.0, 180, r=1.0)
for x in (12.0, 14.0, 16.0):
    place("P_MedWeaponRack", x, 37.0, 0, r=0.9)
scatter(["P_MedBarrel", "P_MedSmallCrate", "P_MedWoodenBucket", "P_MedHayBale"], 18, 34, 54, 0.6, road_margin=0.4)


# ---------- zemin süsleri (D_: yenmez, çarpışmasız) ----------
def decor(meshes, count, z0, z1, x_lim=SIDE - 0.6, road_min=None, road_max=None, tries=20000):
    placed = 0
    for _ in range(tries):
        if placed >= count:
            break
        x, z = rnd.uniform(-x_lim, x_lim), rnd.uniform(z0, z1)
        d = dist_to_roads(x, z)
        if road_min is not None and d < road_min:
            continue
        if road_max is not None and d > road_max:
            continue
        if not free(x, z, 0.35):
            continue
        mesh = rnd.choice(meshes)
        # Çiçekler yenir: belli olsun diye büyük, kümeler halinde (Bahar: "aşırı küçükler")
        put(mesh, x, z, rnd.uniform(0, 360), s=2.2 if mesh.startswith("D_MedFlowers") else 1.0)
        placed += 1


GRASS = ["D_MedGrassTuft"]
FLOWERS = ["D_MedFlowersWhite", "D_MedFlowersWhite", "D_MedFlowersYellow", "D_MedFlowersRed"]
decor(GRASS, 90, -HALF + 1, -25.5, road_min=ROAD_HW + 0.3)                       # köy çimeni
decor(FLOWERS, 55, -HALF + 1, -25.5, road_min=ROAD_HW + 0.8)
decor(GRASS, 70, -23.0, 5.0, road_min=ROAD_HW + 0.3)                             # tarla çimeni
decor(FLOWERS, 35, -23.0, 5.0, road_min=ROAD_HW + 0.8)
# Çakıl yok (Bahar: yerde yiyecek gibi duruyordu, zemin kalabalıktı); yol kenarında seyrek ot.
decor(GRASS, 70, -HALF + 1, 5.0, road_min=ROAD_HW + 0.1, road_max=ROAD_HW + 0.7)  # yol kenarı
decor(GRASS, 25, 5.0, HALF - 1, road_min=ROAD_HW + 0.1, road_max=ROAD_HW + 0.6)
decor(GRASS, 25, 7.5, 31.0, road_min=ROAD_HW + 0.5)    # kasaba: duvar ve ev dipleri
decor(GRASS, 15, 33.5, HALF - 1, road_min=ROAD_HW + 0.4)

# ---------- meydan kenarı: yarı gömülü taşlar + kenarı aşan ot (geçiş yumuşak görünsün) ----------
for px, pz, pr, _ in PLAZAS:
    steps = int(2 * math.pi * pr / 0.8)
    for k in range(steps):
        if rnd.random() < 0.35:
            continue
        a = k * 2 * math.pi / steps + rnd.uniform(-0.1, 0.1)
        rr = pr + rnd.uniform(-0.5, 0.7)
        x, z = px + math.cos(a) * rr, pz + math.sin(a) * rr
        if not free(x, z, 0.3):
            continue
        if rnd.random() < 0.6:
            continue
        put("D_MedGrassTuft", x, z, rnd.uniform(0, 360), s=round(rnd.uniform(1.0, 1.4), 2))

# ---------- zemin: tek karışan yüzey (karışım haritası) + hendek suyu ----------
GROUND_RECT = (-HALF - 10, -HALF - 6, HALF + 10, HALF + 6)
cobble_at = ground_splat.build(
    os.path.normpath(os.path.join(HERE, "..", "..", SPLAT)), GROUND_RECT,
    base_rects=[("Cobble", -SIDE, 6.0, SIDE, 32.0, False),            # kasaba (kenarları surda)
                ("Flagstone", -SIDE, 32.0, SIDE, HALF, False),        # burç avlusu
                ("Dirt", -22.8, -23.2, -12.6, -14.6, True)],          # ekin tarlası
    # toprak önce, taş sonra: taş yolun üstüne toprak patika binmesin
    paths=[(p["tex"], p["hw"], p["pts"] + (p["pts"][:1] if p["loop"] else [])) for p in sorted(PATHS, key=lambda q: q["tex"] != "Dirt")],
    discs=[(tex, x, z, r) for x, z, r, tex in PLAZAS] + [("Grass", x, z, r) for x, z, r in GARDENS])
# 3D yol taşları (Bahar: "taşlar tek boyutlu"): karışım haritasının taş bölgesine tek tek, geçişte seyrekleşerek.
# Unity'de bölge bölge tek mesh'te birleşir (D_MedPathStone*, CastleSceneBuilder.CombinePathStones).
STONE_STEP = 0.82
stone_rng = random.Random(4242)
stones = 0
zs = -HALF + 0.5
while zs < HALF - 0.5:
    xs = -SIDE + 0.4 + (STONE_STEP * 0.5 if int((zs + HALF) / STONE_STEP) % 2 else 0.0)
    while xs < SIDE - 0.4:
        x, z = xs + stone_rng.uniform(-0.22, 0.22), zs + stone_rng.uniform(-0.22, 0.22)
        w = cobble_at(x, z)
        if w > 0.12 and stone_rng.random() < min(1.0, w * 1.6 - 0.1) and free(x, z, 0.3):
            # üst üste binen taşlar aynı yükseklikte kesişip çizgi yapmasın: her biri biraz farklı yükseklikte
            put("D_MedPathStone%d" % stone_rng.randint(1, 5), x, z, stone_rng.uniform(0, 360),
                s=round(stone_rng.uniform(0.85, 1.05) if w > 0.6 else stone_rng.uniform(0.6, 0.9), 2),
                y=round(stone_rng.uniform(0.0, 0.03), 3))
            stones += 1
        xs += STONE_STEP
    zs += STONE_STEP * 0.87
print("yol taşı:", stones)

GROUND = []
for sx in (-1, 1):                                                                  # hendek
    GROUND.append({"tex": "Water", "x0": min(sx * MOAT[0], sx * MOAT[1]), "z0": -HALF - 4,
                   "x1": max(sx * MOAT[0], sx * MOAT[1]), "z1": HALF + 4})
DISCS = []

meta = {"ground": GROUND, "paths": [{"tex": p["tex"], "hw": p["hw"], "loop": p["loop"],
                                     "pts": [c for xz in p["pts"] for c in xz]} for p in PATHS],
        "discs": DISCS, "splat": {"path": SPLAT, "x0": GROUND_RECT[0], "z0": GROUND_RECT[1], "x1": GROUND_RECT[2], "z1": GROUND_RECT[3]},
        "zLines": [l[0] for l in LINES], "side": SIDE, "half": HALF,
        "start": list(START), "keep": list(KEEP)}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump({"meta": meta, "items": items}, f, ensure_ascii=False, indent=0)
counts = {}
for it in items:
    counts[it["mesh"]] = counts.get(it["mesh"], 0) + 1
print("yazıldı:", OUT, "obje:", len(items))
print(sorted(counts.items(), key=lambda kv: -kv[1])[:16])
