#!/usr/bin/env python3
"""Left No Crumbs denge tablosu: oyundaki gerçek değerleri asset/sahne/koddan okur, Markdown tablo basar.

Kullanım (repo kökünden):
    python3 -I .claude/skills/denge/denge_tablosu.py [analytics.json yolu]

analytics.json verilmezse Editor'un kaydı okunur (~/Library/Application Support/Zorse Studio/Left No Crumbs/).
Telefondaki kayıt DEV panelindeki özet satırlarında görünür.
"""
import json
import os
import re
import sys

ROOT = os.getcwd()
SCENE = "Assets/_Project/Scenes/GameScene.unity"


def read(path):
    with open(os.path.join(ROOT, path), encoding="utf-8", errors="ignore") as f:
        return f.read()


def yaml_num(text, key, default=None):
    m = re.search(rf"^\s*{re.escape(key)}: ([-\d.]+)\s*$", text, re.M)
    return float(m.group(1)) if m else default


def code_const(path, pattern, default=None):
    m = re.search(pattern, read(path))
    return m.groups() if m else default


def main():
    scene = read(SCENE)

    # --- Market (MetaProgression, sahnedeki değerler kodu ezer)
    meta = {k: yaml_num(scene, k) for k in [
        "_miknatoCost", "_speedBaseCost", "_speedCostPerLevel", "_maxHealthBaseCost", "_maxHealthCostPerLevel",
        "_massGainBaseCost", "_massGainCostPerLevel", "_coinGainBaseCost", "_coinGainCostPerLevel",
        "_xpMultiplierCost", "_speedBonusPerLevel", "_maxHealthBonusPerLevel", "_massGainBonusPerLevel",
        "_coinGainBonusPerLevel", "_xpMultiplierBonus"]}
    max_level = int(code_const("Assets/_Project/Scripts/Systems/Meta/MetaProgression.cs",
                               r"MaxStatLevel = (\d+)", ("0",))[0])

    # --- Coin kaynakları
    police_coin = int(code_const("Assets/_Project/Scripts/Entities/Enemies/EnemyBase.cs", r"PoliceCoin = (\d+)", ("1",))[0])
    elite_lo, elite_hi = map(int, code_const("Assets/_Project/Scripts/Entities/Enemies/EnemyBase.cs",
                                             r"IsElite \? Random\.Range\((\d+), (\d+)\)", ("5", "11")))
    elite_hi -= 1  # Random.Range(int, int) üst sınır hariç
    chest = int(yaml_num(read("Assets/_Project/Prefabs/Chest.prefab"), "_coinAmount", 0))
    boss = read("Assets/_Project/ScriptableObjects/Enemies/BossData_Miniboss.asset")
    bmin = [int(x) for x in re.findall(r"BonusCoinMin: (\d+)", boss)]
    bmax = [int(x) for x in re.findall(r"BonusCoinMax: (\d+)", boss)]

    # --- XP eğrisi (BlobGrowth, sahne)
    xb, xg, xq = (yaml_num(scene, k) for k in ("_baseXPThreshold", "_xpGrowthPerLevel", "_xpQuadraticPerLevel"))

    out = []
    p = out.append
    p("## Coin kaynakları\n")
    p("| Kaynak | Coin |\n|---|---|")
    p(f"| Polis | {police_coin} |")
    p(f"| Elit | {elite_lo}–{elite_hi} + sandık {chest} |")
    for i, (lo, hi) in enumerate(zip(bmin, bmax)):
        p(f"| Boss aşaması {i + 1} | {elite_lo}–{elite_hi} + sandık {chest} + bonus {lo}–{hi} |")

    p("\n## Market\n")
    p(f"Kademeli statların sınırı: Sv.{max_level}\n")
    p("| Kalem | Seviye başı etki | Sv.1 fiyat | Sınıra kadar toplam |\n|---|---|---|---|")
    total = meta["_miknatoCost"] + meta["_xpMultiplierCost"]
    for name, base, step, eff, unit in [
        ("Hız", "_speedBaseCost", "_speedCostPerLevel", "_speedBonusPerLevel", "%"),
        ("Max Can", "_maxHealthBaseCost", "_maxHealthCostPerLevel", "_maxHealthBonusPerLevel", ""),
        ("Kütle Kazancı", "_massGainBaseCost", "_massGainCostPerLevel", "_massGainBonusPerLevel", "%"),
        ("Coin Kazancı", "_coinGainBaseCost", "_coinGainCostPerLevel", "_coinGainBonusPerLevel", "%")]:
        cost = sum(meta[base] + i * meta[step] for i in range(max_level))
        total += cost
        e = meta[eff] * 100 if unit == "%" else meta[eff]
        p(f"| {name} | +{e:g}{unit} | {meta[base]:.0f} | {cost:,.0f} |")
    p(f"| Mıknato | karakter | {meta['_miknatoCost']:.0f} | {meta['_miknatoCost']:.0f} |")
    p(f"| XP Çarpanı | +{meta['_xpMultiplierBonus'] * 100:g}% XP | {meta['_xpMultiplierCost']:.0f} | {meta['_xpMultiplierCost']:.0f} |")
    p(f"\n**Market toplamı:** {total:,.0f} coin")

    p("\n## XP eğrisi\n")
    p("| Seviye | O seviye XP | Toplam |\n|---|---|---|")
    cum = 0
    for lvl in range(1, 11):
        need = xb + (lvl - 1) * xg + (lvl - 1) ** 2 * xq
        cum += need
        if lvl in (1, 4, 7, 10):
            p(f"| {lvl} → {lvl + 1} | {need:.0f} | {cum:,.0f} |")

    p("\n## Upgrade'ler\n")
    p("| Skill | Ağırlık | Max | Seviye başı |\n|---|---|---|---|")
    up_dir = os.path.join(ROOT, "Assets/_Project/Data/Upgrades")
    for f in sorted(os.listdir(up_dir)):
        if f.startswith("Upgrade_") and f.endswith(".asset"):
            t = read(f"Assets/_Project/Data/Upgrades/{f}")
            p(f"| {f[8:-6]} | {yaml_num(t, '_weight', 0):g} | {yaml_num(t, '_maxLevel', 0):g} | {yaml_num(t, '_perLevelValue', 0):g} |")

    # --- Ölçülen run'lar
    path = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser(
        "~/Library/Application Support/Zorse Studio/Left No Crumbs/analytics.json")
    p("\n## Ölçülen run'lar\n")
    runs = []
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            runs = [r for r in json.load(f).get("runs", []) if r.get("character")]
    if not runs:
        p(f"Ölçüm alanlı run yok ({path}). DEĞERLENDİRİLMEDİ: hedeflerle karşılaştırma için gerçek run gerekir.")
    else:
        n = len(runs)
        avg = lambda k: sum(r.get(k, 0) for r in runs) / n
        coin_total = sum(r.get(k, 0) for r in runs for k in ("coinsPolice", "coinsElite", "coinsBoss", "coinsChest")) or 1
        elite_share = sum(r.get("coinsElite", 0) + r.get("coinsChest", 0) for r in runs) / coin_total
        expired = sum(r.get("coinsExpired", 0) for r in runs) / max(1, sum(r.get("coinsEarned", 0) for r in runs))
        p(f"{n} run · ort. süre {avg('durationSeconds') / 60:.1f} dk · ort. coin {avg('coinsEarned'):.0f} "
          f"(kasaya {avg('coinsBanked'):.0f}) · polis {avg('policeKills'):.0f} · elit {avg('eliteKills'):.1f} · son seviye {avg('finalLevel'):.1f}\n")
        banked = avg("coinsBanked") or 1
        p("| Hedef (OYUN.md) | Ölçülen |\n|---|---|")
        p(f"| Mıknato 2.–3. run | {meta['_miknatoCost'] / banked:.1f}. run |")
        p(f"| Market'in tamamı ~40. run | {total / banked:.0f}. run |")
        p(f"| Elit + sandık payı %40–55 | %{elite_share * 100:.0f} |")
        p(f"| Kaybolan coin < %15 | %{expired * 100:.0f} |")

    print("\n".join(out))


if __name__ == "__main__":
    main()
