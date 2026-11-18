"""Game Center rozetlerini App Store Connect API ile yükler (rozet + EN/TR metin + görsel).
Var olan rozet (vendorIdentifier) atlanır, eksik dil ya da görsel tamamlanır: tekrar çalıştırmak güvenli.
Id'ler Assets/_Project/Scripts/Systems/Social/Achievements.cs, liste OYUN.md §10.1, görseller zorse-blender build_badges.py.

Anahtar repoya girmez; ortam değişkenleri:
  ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_PATH (.p8)
Kullanım (PyJWT + cryptography gerekir):
  python3 tools/asc_achievements.py --check     # sadece okur
  python3 tools/asc_achievements.py --upload
"""
import json
import os
import sys
import time
import urllib.error
import urllib.request

import jwt

BUNDLE_ID = "com.zorsestudio.leftnocrumbs"
BADGE_DIR = os.path.expanduser("~/Developer/zorse-blender/badges")
API = "https://api.appstoreconnect.apple.com/v1"

# id, puan, gizli, (TR ad, koşul, kazanınca), (EN ad, koşul, kazanınca)
ACHIEVEMENTS = [
    ("lnc_first_run", 10, False, ("Afiyet Olsun", "İlk run'ını bitir.", "İlk lokma hep en tatlısıdır."),
     ("Bon Appétit", "Finish your first run.", "The first bite is always the sweetest.")),
    ("lnc_quick_death", 10, True, ("Aperatif", "İlk dakikada öl.", "Meze sendin."),
     ("Appetizer", "Get caught in the first minute.", "Turns out you were the snack.")),
    ("lnc_reach_dev", 30, False, ("Şehir Bana Dar", "Dev boyuna ulaş.", "Şehir artık senin tabağın."),
     ("Too Big to Fail", "Reach Colossal size.", "The city is your plate now.")),
    ("lnc_first_win", 50, False, ("Kırıntı Bırakmadın", "Helikopteri yutup oyunu kazan.", "Tabak tertemiz."),
     ("Left No Crumbs", "Swallow the helicopter and win a run.", "Plate licked clean.")),
    ("lnc_no_damage", 60, False, ("Kılı Bile Kıpırdamadı", "10 dakika boyunca hiç hasar alma.", "Kılın yok ama olsa kıpırdamazdı."),
     ("Not a Scratch", "Take no damage for 10 minutes straight.", "No hair, but not a single one out of place.")),
    ("lnc_tank", 30, False, ("Tank Tatlısı", "Polis Tankı'nı alt et.", "Paletli tatlı, bol şerbetli."),
     ("Tank You Very Much", "Defeat the Police Tank.", "Treads included, no refunds.")),
    ("lnc_helicopter", 50, False, ("Pervaneli Dürüm", "Polis Helikopteri'ni yut.", "Pervanesi biraz dişe geldi."),
     ("Rotor Rolls", "Swallow the Police Helicopter.", "A little crunchy around the blades.")),
    ("lnc_police_feast", 50, False, ("Karakol Kahvaltısı", "Bir run'da 100 polis yut.", "Serpme kahvaltı, üniformalı."),
     ("Breakfast at the Precinct", "Swallow 100 police in one run.", "Full breakfast, uniforms included.")),
    ("lnc_riot_police", 20, False, ("Kalkan mı? Çatal Bıçak!", "Çevik kuvvet polisini yut.", "Kalkanı tabak niyetine kullandın."),
     ("Shield Snack", "Swallow a riot officer.", "Used the shield as a plate.")),
    ("lnc_motor_police", 20, False, ("Kask Zorunluymuş", "Motorlu polisi yut.", "Kaskı da yedin, güvenlik önemli."),
     ("Helmet Optional", "Swallow a motorcycle cop.", "Ate the helmet too. Safety first.")),
    ("lnc_robot_dog", 10, False, ("Mama Saati", "Robot köpek yut.", "Hav! (Metalik bir hav.)"),
     ("Good Boy, Good Bite", "Swallow a robot dog.", "Woof. (A metallic woof.)")),
    ("lnc_black_hole", 50, False, ("Midede Olay Ufku", "Kara Delik evrimini aç.", "Işık bile kaçamıyor, tatlı hiç kaçamaz."),
     ("Event Horizon Buffet", "Unlock the Black Hole evolution.", "Not even light escapes. Dessert never stood a chance.")),
    ("lnc_gas_kills", 30, True, ("Kimse Görmedi", "Sindirim Gazı'yla toplam 50 düşman devir.", "Kimse görmedi ama herkes anladı."),
     ("Silent but Deadly", "Take down 50 enemies with Digestive Gas.", "Nobody saw it. Everybody smelled it.")),
    ("lnc_slip_dogs", 30, False, ("Dikkat Islak Zemin", "Kaygan Zemin'de toplam 20 robot köpek kaydır.", "Uyarı tabelası vardı, okumadılar."),
     ("Caution: Wet Floor", "Make 20 robot dogs slip on Slippery Floor.", "There was a sign. They didn't read it.")),
    ("lnc_split", 20, False, ("Ben, Ben ve Yine Ben", "Bölünme kartını al.", "Kalabalık, ama hepsi sen."),
     ("Me, Myself & Me", "Pick the Split card.", "A crowd, and it's all you.")),
    ("lnc_fully_loaded", 50, False, ("Full Paket", "3 aktif ve 3 pasif skill'in hepsini max seviyeye çıkar.", "Menüde ne varsa."),
     ("Fully Loaded", "Max out all 3 active and 3 passive skills.", "Everything on the menu.")),
    ("lnc_ferris_wheel", 20, True, ("Çark-ı Felek", "Dönme dolaba dönüş.", "Lunapark bugün kapalı."),
     ("Spin Cycle", "Become the Ferris wheel.", "The fair is closed today.")),
    ("lnc_win_topik", 40, False, ("Rüzgar Gibi Geçti", "Topik ile kazan.", "Arkasında toz bile kalmadı."),
     ("Gone with the Wind", "Win with Topik.", "Not even dust left behind.")),
    ("lnc_win_miknato", 40, False, ("Çekim Merkezi", "Mıknato ile kazan.", "Her şey kendiliğinden geldi."),
     ("Personal Magnetism", "Win with Mıknato.", "Everything just came running.")),
    ("lnc_win_fisek", 40, False, ("Kutlamaya Değer", "Fişek ile kazan.", "Havai fişekler senin için."),
     ("Something to Celebrate", "Win with Fişek.", "The fireworks are for you.")),
    ("lnc_win_dalcik", 40, False, ("Dayak Cennetten Çıkma", "Dalcık ile kazan.", "Sopa her zaman haklıdır."),
     ("Stick to the Plan", "Win with Dalcık.", "The stick always wins.")),
    ("lnc_win_cano", 40, False, ("Sıfır Atık", "Cano ile kazan.", "Geri dönüşüm kazandırır."),
     ("Zero Waste", "Win with Cano.", "Recycling pays off.")),
    ("lnc_all_characters", 50, False, ("Aile Sofrası", "Tüm karakterleri aç.", "Sofra kuruldu, herkes burada."),
     ("Family Dinner", "Unlock every character.", "Table's set, everyone's here.")),
    ("lnc_win_all", 80, False, ("Beşi Bir Yerde", "Beş karakterle de kazan.", "Çorbadan tatlıya tam menü."),
     ("Full Course Menu", "Win with all five characters.", "Starter to dessert, the full menu.")),
    ("lnc_pedestrians", 40, False, ("Nüfus Sayımı Bozuldu", "Toplam 1.000 yaya yut.", "Belediye rakamları tutturamıyor."),
     ("Census Problem", "Swallow 1,000 pedestrians in total.", "City hall can't make the numbers add up.")),
    ("lnc_cars", 40, False, ("Trafik Rahatladı", "Toplam 500 araba yut.", "Şehirde ilk kez trafik yok."),
     ("Traffic Cleared", "Swallow 500 cars in total.", "First time this city has had no traffic.")),
    ("lnc_coins", 50, False, ("Kumbara Patladı", "Run'larda toplam 100.000 coin topla.", "Kumbara artık sensin."),
     ("Piggy Bank Burst", "Collect 100,000 coins across runs.", "You are the piggy bank now.")),
]
LOCALES = (("tr", 3), ("en-US", 4))  # dil kodu, ACHIEVEMENTS satırındaki sütun

_token = (None, 0.0)


def token():
    global _token
    if _token[0] and time.time() < _token[1] - 60:
        return _token[0]
    key = open(os.environ["ASC_KEY_PATH"]).read()
    exp = time.time() + 15 * 60
    t = jwt.encode({"iss": os.environ["ASC_ISSUER_ID"], "iat": int(time.time()), "exp": int(exp), "aud": "appstoreconnect-v1"},
                   key, algorithm="ES256", headers={"kid": os.environ["ASC_KEY_ID"], "typ": "JWT"})
    _token = (t, exp)
    return t


def call(method, path, body=None):
    url = path if path.startswith("http") else API + path
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method,
                                 headers={"Authorization": f"Bearer {token()}", "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        sys.exit(f"{method} {path} → {e.code}\n{e.read().decode()[:2000]}")


def get_all(path):
    out, url = [], path
    while url:
        page = call("GET", url)
        out += page.get("data", [])
        url = page.get("links", {}).get("next")
    return out


def rel(kind, rid):
    return {"data": {"type": kind, "id": rid}}


def upload_image(loc_id, png):
    size = os.path.getsize(png)
    img = call("POST", "/gameCenterAchievementImages", {"data": {
        "type": "gameCenterAchievementImages",
        "attributes": {"fileName": os.path.basename(png), "fileSize": size},
        "relationships": {"gameCenterAchievementLocalization": rel("gameCenterAchievementLocalizations", loc_id)}}})["data"]
    blob = open(png, "rb").read()
    for op in img["attributes"]["uploadOperations"]:
        chunk = blob[op["offset"]:op["offset"] + op["length"]]
        headers = {h["name"]: h["value"] for h in op.get("requestHeaders", [])}
        req = urllib.request.Request(op["url"], data=chunk, method=op["method"], headers=headers)
        urllib.request.urlopen(req).read()
    call("PATCH", f"/gameCenterAchievementImages/{img['id']}", {"data": {
        "type": "gameCenterAchievementImages", "id": img["id"], "attributes": {"uploaded": True}}})


def main():
    upload = "--upload" in sys.argv
    apps = call("GET", f"/apps?filter[bundleId]={BUNDLE_ID}")["data"]
    if not apps:
        sys.exit("Uygulama bulunamadı")
    app_id = apps[0]["id"]
    detail = call("GET", f"/apps/{app_id}/gameCenterDetail").get("data")
    if not detail:
        sys.exit("Game Center açık değil (gameCenterDetail yok)")
    detail_id = detail["id"]

    boards = get_all(f"/gameCenterDetails/{detail_id}/gameCenterLeaderboards?limit=200")
    print("Sıralamalar:", [b["attributes"]["vendorIdentifier"] for b in boards])
    existing = {a["attributes"]["vendorIdentifier"]: a for a in
                get_all(f"/gameCenterDetails/{detail_id}/gameCenterAchievements?limit=200")}
    print(f"Mevcut rozet: {len(existing)} · listede: {len(ACHIEVEMENTS)} · toplam puan: {sum(a[1] for a in ACHIEVEMENTS)}")

    for vid, points, hidden, *texts in ACHIEVEMENTS:
        png = os.path.join(BADGE_DIR, vid + ".png")
        if not os.path.exists(png):
            sys.exit(f"Görsel yok: {png}")
        if not upload:
            print(("VAR  " if vid in existing else "YENİ ") + vid)
            continue

        ach = existing.get(vid)
        if ach is None:
            ach = call("POST", "/gameCenterAchievements", {"data": {
                "type": "gameCenterAchievements",
                "attributes": {"referenceName": texts[0][0], "vendorIdentifier": vid, "points": points,
                               "showBeforeEarned": not hidden, "repeatable": False},
                "relationships": {"gameCenterDetail": rel("gameCenterDetails", detail_id)}}})["data"]
        locs = {l["attributes"]["locale"]: l for l in get_all(f"/gameCenterAchievements/{ach['id']}/localizations?limit=50")}
        for locale, col in LOCALES:
            name, before, after = (texts[0] if col == 3 else texts[1])
            loc = locs.get(locale)
            if loc is None:
                loc = call("POST", "/gameCenterAchievementLocalizations", {"data": {
                    "type": "gameCenterAchievementLocalizations",
                    "attributes": {"locale": locale, "name": name, "beforeEarnedDescription": before, "afterEarnedDescription": after},
                    "relationships": {"gameCenterAchievement": rel("gameCenterAchievements", ach["id"])}}})["data"]
            image = call("GET", f"/gameCenterAchievementLocalizations/{loc['id']}/gameCenterAchievementImage").get("data")
            if not image:
                upload_image(loc["id"], png)
        print("OK  ", vid)


main()
