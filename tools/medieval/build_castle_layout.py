"""Kuşatılmış Kale haritasının yerleşimi (Unity birimi, oyun ölçeğinde). Blender gerekmez: python build_castle_layout.py
Çıktı: Assets/_Project/Art/City/Medieval/castle_layout.json → Tools/Blob.io/Kale Sahnesini Kur okur.

Düzen (Bahar, 2026-11-20): TEK YOL. Güneyden kuzeye bölgeler, her geçişte işaretli kapı:
  Köy (başlangıç) → çit kapısı (Little) → Kasaba (dar sokaklar) → taş kapı (Medium) → Kale avlusu → kale kapısı (Large) → Burç
Kapılar zikzak (sağ, sol, orta): yol düz koridor olmasın. Oyun alanı x ∈ ±SIDE; yanlarda ağaç sırası + görünmez duvar.
Yön: modelin ön yüzü −Z (yaw 0) = güneye, oyuncunun geldiği yöne.
"""
import json
import math
import os
import random

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "City", "Medieval", "castle_layout.json"))

SCALE = 0.7                 # build_medieval.GAME_SCALE
SEG = 3.0 * SCALE           # duvar parçası boyu
HALF = 56.0                 # harita sınırı (kare)
SIDE = 24.0                 # oynanan genişlik yarısı
START = (0.0, -50.0)

# Bölge sınırları (güneyden kuzeye): (z, duvar öneki, kapı mesh'i, kapı genişliği, kapı x, kapı ölçeği)
LINES = [
    (-24.0, "B_MedFence", "B_MedFenceGate", 4.28, 9.0, 1.0),     # genişlikler Blender'dan ölçüldü (oyun birimi, ölçeksiz)
    (6.0, "B_MedStoneWall", "B_MedGateClosed", 2.73, -9.0, 1.4),
    (32.0, "B_MedCastleWall", "B_MedMainGate", 6.455, 0.0, 1.0),
]
KEEP = (0.0, 45.0)

rnd = random.Random(20261120)
items = []
taken = []  # (x, z, yarıçap)


def put(mesh, x, z, yaw=0.0, s=1.0, y=0.0):
    items.append({"mesh": mesh, "x": round(x, 3), "y": round(y, 3), "z": round(z, 3), "yaw": round(yaw % 360, 2), "s": s})


def free(x, z, r):
    return all((x - tx) ** 2 + (z - tz) ** 2 >= (r + tr) ** 2 for tx, tz, tr in taken)


def take(x, z, r):
    taken.append((x, z, r))


# ---------- yollar (merkez çizgileri; çevresi boş kalır) ----------
ROADS = [  # (x0, z0, x1, z1, yarı genişlik)
    (0.0, -56.0, 0.0, -36.0, 2.4),            # köy: başlangıçtan kuzeye
    (0.0, -36.0, 9.0, -30.0, 2.4),            # çit kapısına kıvrılır
    (9.0, -30.0, 9.0, -24.0, 2.4),
    (9.0, -24.0, 9.0, -11.0, 2.6),            # kasaba: kapıdan kuzeye ana cadde
    (9.0, -11.0, -9.0, -11.0, 2.6),           # batıya dönen cadde
    (-9.0, -11.0, -9.0, 6.0, 2.6),            # taş kapıya
    (-9.0, 6.0, -9.0, 14.0, 3.0),             # avlu: kapıdan içeri
    (-9.0, 14.0, 0.0, 24.0, 3.0),
    (0.0, 24.0, 0.0, 32.0, 3.0),              # kale kapısına
    (0.0, 32.0, 0.0, 40.0, 3.0),              # burca
]
ROAD_HW = 2.6


def dist_to_roads(x, z):
    best = 1e9
    for x0, z0, x1, z1, _ in ROADS:
        dx, dz = x1 - x0, z1 - z0
        L2 = dx * dx + dz * dz or 1e-9
        t = max(0.0, min(1.0, ((x - x0) * dx + (z - z0) * dz) / L2))
        best = min(best, math.hypot(x - (x0 + dx * t), z - (z0 + dz * t)))
    return best


# ---------- bölge duvarları + kapılar + fare delikleri ----------
for z, prefix, gate, gate_w, gate_x, gate_s in LINES:
    put(gate, gate_x, z, 0, s=gate_s)
    take(gate_x, z, gate_w * gate_s / 2 + 0.5)
    half_gap = gate_w * gate_s / 2 - 0.1                     # parçalar kapıya hafif biner: arada boşluk kalmaz
    hole_x = gate_x + (-6.3 if gate_x > 0 else 6.3)        # kapının yanında kestirme
    # Kapının iki kenarından dışarı doğru dizilir; son parça oyun alanının dışına taşar (yan duvarla arada boşluk yok).
    for direction in (-1, 1):
        x = gate_x + direction * half_gap
        while abs(x) < SIDE + SEG:
            cx = x + direction * SEG / 2
            put(prefix + ("Hole" if abs(cx - hole_x) < SEG / 2 else ""), cx, z, 0)
            take(cx, z, 1.2)
            x += direction * SEG
    if prefix == "B_MedCastleWall":
        for sx in (-1, 1):
            put("B_MedCornerTower", sx * (SIDE - 0.5), z, 0)

# ---------- yanlar: ağaç sırası (görünmez duvarın dışında, sadece görüntü) ----------
for sx in (-1, 1):
    zz = -HALF + 1.5
    while zz < HALF - 1:
        put("P_MedOakTree", sx * (SIDE + rnd.uniform(2.0, 3.2)), zz + rnd.uniform(-0.6, 0.6), rnd.uniform(0, 360))
        put("P_MedOakTree", sx * (SIDE + rnd.uniform(4.5, 6.5)), zz + 1.5 + rnd.uniform(-0.6, 0.6), rnd.uniform(0, 360))
        zz += 3.0


def scatter(meshes, count, z0, z1, r, road_margin=1.2, yaw_any=True, x_lim=SIDE - 1.5, tries=5000):
    placed = 0
    for _ in range(tries):
        if placed >= count:
            break
        x, z = rnd.uniform(-x_lim, x_lim), rnd.uniform(z0, z1)
        if dist_to_roads(x, z) < ROAD_HW + road_margin + r or not free(x, z, r):
            continue
        if math.hypot(x - START[0], z - START[1]) < 5:
            continue
        put(rnd.choice(meshes), x, z, rnd.uniform(0, 360) if yaw_any else rnd.choice((0, 90, 180, 270)))
        take(x, z, r)
        placed += 1


# ---------- köy (güney) ----------
put("P_MedWindmill", -15.0, -44.0, 20); take(-15.0, -44.0, 3.2)
put("B_MedChapel", 15.0, -46.0, 270); take(15.0, -46.0, 3.4)
put("P_MedWaterWell", -4.5, -40.0, 0); take(-4.5, -40.0, 1.0)
scatter(["B_MedCottage", "B_MedCottage", "B_MedBarn"], 7, -54, -27, 2.8, yaw_any=False)
scatter(["P_MedHaystack", "P_MedHayBale", "P_MedHayBale", "P_MedScarecrow", "P_MedWagon", "P_MedVegetableCart", "P_MedBarrel"], 40, -55, -26, 0.9)
scatter(["P_MedOakTree"], 16, -55, -26, 1.3)
scatter(["P_MedChicken", "P_MedChicken", "P_MedWoodenBucket", "P_MedHayBundle", "P_MedSmallCrate", "P_MedStool", "P_MedFlowerPot"], 60, -55, -26, 0.45, road_margin=0.2)

# ---------- kasaba (dar sokaklar) ----------
TOWN = ["B_MedTownhouse", "B_MedTownhouse", "B_MedNarrowTownhouse", "B_MedNarrowTownhouse", "B_MedTavern",
        "B_MedBakery", "B_MedBlacksmith"]
put("P_MedStoneFountain", 0.0, -5.0, 0); take(0.0, -5.0, 3.6)       # meydan
for mx, mz in ((-3.2, -2.2), (3.2, -2.2), (0.0, -1.0)):
    put("P_MedMarketStall", mx, mz, 0); take(mx, mz, 1.1)
STEP = 3.4
z = -24 + 2.2
while z < 6 - 2.0:
    x = -SIDE + 2.0
    while x < SIDE - 1.8:
        jx, jz = x + rnd.uniform(-0.2, 0.2), z + rnd.uniform(-0.2, 0.2)
        if dist_to_roads(jx, jz) > ROAD_HW + 1.2 and free(jx, jz, 1.45) and rnd.random() > 0.12:  # %12 boş: ara sokak
            best, yaw = 1e9, 0
            for a in (0, 90, 180, 270):  # ön yüz en yakın caddeye
                fx, fz = -math.sin(math.radians(a)), -math.cos(math.radians(a))
                dd = dist_to_roads(jx + fx * 2.0, jz + fz * 2.0)
                if dd < best:
                    best, yaw = dd, a
            put(rnd.choice(TOWN), jx, jz, yaw)
            take(jx, jz, 1.45)
        x += STEP
    z += STEP
scatter(["P_MedBarrel", "P_MedWoodenBucket", "P_MedSmallCrate", "P_MedFlowerPot", "P_MedTorchPole", "P_MedStool",
         "P_MedHayBundle", "P_MedChicken", "P_MedVegetableCart"], 75, -23, 5, 0.45, road_margin=-1.6)

# ---------- kale avlusu ----------
for mesh, x, z, yaw in (("B_MedStables", 12.0, 12.0, 270), ("B_MedBarracks", 12.0, 24.0, 270),
                        ("B_MedBarracks", -16.0, 26.0, 90)):
    put(mesh, x, z, yaw); take(x, z, 3.2)
for i in range(4):
    put("P_MedTrainingDummy", -18 + i * 1.8, 10.0, 0); take(-18 + i * 1.8, 10.0, 0.8)
for i in range(4):
    put("P_MedArcheryTarget", 4 + i * 2.4, 29.0, 180); take(4 + i * 2.4, 29.0, 1.0)
put("P_MedWeaponRack", -4.0, 18.0, 0); take(-4.0, 18.0, 1.0)
scatter(["P_MedBarrel", "P_MedHayBale", "P_MedSmallCrate", "P_MedTorchPole", "P_MedWagon", "P_MedWoodenBucket",
         "P_MedStool", "P_MedHayBundle", "P_MedTrainingDummy"], 55, 7.5, 31, 0.6, road_margin=0.4)

# ---------- burç (kuzey) ----------
put("B_MedCastleKeep", KEEP[0], KEEP[1], 0); take(KEEP[0], KEEP[1], 5.0)
for sx in (-1, 1):
    put("P_MedTorchPole", sx * 4.0, 38.5, 0)
scatter(["P_MedBarrel", "P_MedTorchPole", "P_MedSmallCrate", "P_MedWoodenBucket", "P_MedHayBale"], 28, 34, 54, 0.6, road_margin=0.4)

# ---------- zemin: dokulu düzlemler (üst üste, sonraki üstte) ----------
GROUND = [
    {"tex": "Grass", "x0": -HALF, "z0": -HALF, "x1": HALF, "z1": HALF},
    {"tex": "Cobble", "x0": -SIDE, "z0": -24.0, "x1": SIDE, "z1": 6.0},
    {"tex": "Flagstone", "x0": -SIDE, "z0": 6.0, "x1": SIDE, "z1": HALF},
]
for x0, z0, x1, z1, hw in ROADS[:3]:  # köy toprak yolu: düz kısım tek kutu, çapraz kısım adım adım küçük kutular
    if x0 == x1 or z0 == z1:
        GROUND.append({"tex": "Dirt", "x0": min(x0, x1) - hw, "z0": min(z0, z1) - hw, "x1": max(x0, x1) + hw, "z1": min(max(z0, z1) + hw, LINES[0][0])})  # çitin ötesine taşmasın
        continue
    n = int(math.hypot(x1 - x0, z1 - z0) / 1.2) + 1
    for k in range(n + 1):
        cx, cz = x0 + (x1 - x0) * k / n, z0 + (z1 - z0) * k / n
        GROUND.append({"tex": "Dirt", "x0": cx - hw, "z0": cz - hw, "x1": cx + hw, "z1": cz + hw})

meta = {"ground": GROUND, "zLines": [l[0] for l in LINES], "side": SIDE, "half": HALF, "start": list(START), "keep": list(KEEP)}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump({"meta": meta, "items": items}, f, ensure_ascii=False, indent=0)
counts = {}
for it in items:
    counts[it["mesh"]] = counts.get(it["mesh"], 0) + 1
print("yazıldı:", OUT, "obje:", len(items))
print(sorted(counts.items(), key=lambda kv: -kv[1])[:14])
