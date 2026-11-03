using System.IO;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-03: Canvas/SafeArea altına üst haber bandını (NewsTicker) kurar ve haber asset'ini oluşturur.
    // Tekrar çalıştırmak bandı silip yeniden kurar; asset varsa İÇERİĞİNE DOKUNMAZ (elle eklenen haberler korunur).
    // Temp/news_ticker.request varsa script yüklemesinde kendiliğinden çalışır.
    public static class NewsTickerBuilder
    {
        private const string RequestPath = "Temp/news_ticker.request";
        private const string ResultPath = "Temp/news_ticker.result";
        private const string RootName = "NewsTicker";
        private const string AssetFolder = "Assets/_Project/ScriptableObjects/UI";
        private const string AssetPath = AssetFolder + "/NewsHeadlines.asset";

        private const float BarHeight = 58f;
        private const float TagWidth = 150f;

        private static readonly Color Bone = new Color(0.96f, 0.94f, 0.88f, 1f);      // #F5F0E0
        private static readonly Color Night = new Color(0.05f, 0.05f, 0.17f, 0.8f);    // #0D0D2B
        private static readonly Color Crimson = new Color(0.545f, 0f, 0f, 1f);         // #8B0000

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                File.WriteAllText(ResultPath, EditorApplication.isPlayingOrWillChangePlaymode ? "ABORTED: Play Mode açık" : Build());
            };
        }

        [MenuItem("Tools/Blob.io/Haber Bandını Kur")]
        private static void BuildFromMenu() => Debug.Log("[NewsTickerBuilder] " + Build());

        private static string Build()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            Transform safeArea = canvas != null ? canvas.transform.Find("SafeArea") : null;
            if (safeArea == null) return "HATA: Canvas/SafeArea bulunamadı";

            NewsHeadlines headlines = LoadOrCreateHeadlines(out bool created);

            Transform existing = safeArea.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            // Üst kenar, sol boşluk 30; sağda duraklatma butonuna (100 + 30) yer bırakılır.
            RectTransform root = CreateRect(RootName, safeArea);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.offsetMin = new Vector2(30f, -30f - BarHeight);
            root.offsetMax = new Vector2(-150f, -30f);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Haber Bandı");

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false; // dokunmatik sürüklemeyi engellemesin

            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            RectTransform background = CreateRect("Background", root);
            Stretch(background);
            Image bg = background.gameObject.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = Night;
            bg.raycastTarget = false;

            RectTransform tag = CreateRect("Tag", root);
            tag.anchorMin = new Vector2(0f, 0f);
            tag.anchorMax = new Vector2(0f, 1f);
            tag.pivot = new Vector2(0f, 0.5f);
            tag.offsetMin = Vector2.zero;
            tag.offsetMax = new Vector2(TagWidth, 0f);
            Image tagImage = tag.gameObject.AddComponent<Image>();
            tagImage.sprite = sprite;
            tagImage.type = Image.Type.Sliced;
            tagImage.color = Crimson;
            tagImage.raycastTarget = false;
            TextMeshProUGUI tagLabel = CreateText("Label", tag, "HABER", 30, TextAlignmentOptions.Center);
            tagLabel.fontStyle = FontStyles.Bold;
            Stretch(tagLabel.rectTransform);

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(TagWidth + 16f, 0f);
            viewport.offsetMax = new Vector2(-16f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI text = CreateText("Headline", viewport, "", 32, TextAlignmentOptions.MidlineLeft);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = new Vector2(600f, 0f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            NewsTicker ticker = root.gameObject.AddComponent<NewsTicker>();
            SerializedObject so = new SerializedObject(ticker);
            so.FindProperty("_headlines").objectReferenceValue = headlines;
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_viewport").objectReferenceValue = viewport;
            so.FindProperty("_text").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            // HUD'un üstünde, duraklatma menüsünün altında çizilsin.
            Transform pauseMenu = safeArea.Find("PauseMenu");
            if (pauseMenu != null) root.SetSiblingIndex(pauseMenu.GetSiblingIndex());
            else root.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            return "OK: SafeArea/NewsTicker kuruldu" + (created ? ", NewsHeadlines.asset oluşturuldu" : " (mevcut NewsHeadlines.asset kullanıldı)")
                + ". Sahneyi kaydet (Cmd+S).";
        }

        private static NewsHeadlines LoadOrCreateHeadlines(out bool created)
        {
            NewsHeadlines asset = AssetDatabase.LoadAssetAtPath<NewsHeadlines>(AssetPath);
            created = asset == null;
            if (asset != null) return asset;

            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "UI");

            asset = ScriptableObject.CreateInstance<NewsHeadlines>();
            asset.EditorSetContent(DefaultWorldNews, DefaultBlobNews());
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static readonly string[] DefaultWorldNews =
        {
            "Kraliçenin kuğusu kayboldu, saray \"endişeye gerek yok\" diyor",
            "Bugün hava güzel, meteoroloji şaşkın",
            "Belediye: \"Kaldırımdaki çukur artık turistik bir mekân\"",
            "Mahallenin kedisi muhtar seçildi, ilk icraatı mama zammı",
            "Uzmanlar uyardı: Pazartesi yine geliyor",
            "Bilim insanları tostun neden hep yağlı yüzüne düştüğünü çözdü",
            "Simitçi rekor kırdı: Bir günde 4.000 simit",
            "Güvercinler meydanda greve başladı, talepleri belirsiz",
            "Şehirde trafik yok, sürücüler ne yapacağını bilemedi",
            "Ünlü fenomen suyu keşfetti: \"Çok ferahlatıcı\"",
            "Yeni araştırma: Kahve içenlerin kahve içtiği kanıtlandı",
            "Kayıp çorap vakası 34. gününde, hâlâ ipucu yok",
            "Martı turistin dondurmasını çaldı, olay yerinden uçarak kaçtı",
            "Hayvanat bahçesindeki panda bugün de hiçbir şey yapmadı",
            "Belediye fıskiyeyi açtı, halk sevinç gözyaşlarına boğuldu",
            "Komşu çocuk piyano dersine başladı, mahalle tedirgin",
            "Pizzaya ananas tartışması mecliste gerginlik çıkardı",
            "Otobüs tam zamanında geldi, yolcular şüpheli",
            "Dünyanın en büyük köftesi yapıldı, kimse bitiremedi",
            "Bir ördek kırmızı ışıkta durdu, sürücüler alkışladı",
            "Wi-Fi şifresi \"sifre123\" olan kafe hacklendi",
            "Emekli amca inşaatı 6 saat izledi: \"Kolon yamuk\"",
            "Bilim insanları: Uyumak yorgunluğa iyi geliyor",
            "Dondurmacı \"son top\" dedi, üç top daha verdi",
            "Şehrin en yaşlı kaplumbağası doğum günü pastasına hâlâ yürüyor",
            "Teyzeler bankta toplandı, şehrin tüm sırları çözüldü",
        };

        private static NewsHeadlines.TierHeadlines[] DefaultBlobNews() => new[]
        {
            new NewsHeadlines.TierHeadlines
            {
                Tier = BlobTier.Small,
                Headlines = new[]
                {
                    "Parkta \"sinirli bir top\" görüldüğü iddia edildi",
                    "Vatandaş: \"Çöpümü bir top yedi.\" Polis: \"Tabii, tabii.\"",
                    "Sokaklardaki çöpler gizemli şekilde azalıyor, belediye övgüleri topladı",
                    "Çocuk \"top beni kovaladı\" dedi, kimse inanmadı",
                    "Kayıp ilanı: \"Simidimi gören var mı?\"",
                    "Hayvan barınağı: \"Yuvarlak bir şey kaçmış olabilir\"",
                    "Devriye polisleri sinirli bir topu arıyor, ellerindeki tek ipucu bir top fotoğrafı",
                    "Emniyet \"sinirli top\" ihbarlarını ilk kez ciddiye aldı",
                    "Polis, kayıp çöp kutuları için \"yuvarlak bir şüpheli\" arıyor",
                },
            },
            new NewsHeadlines.TierHeadlines
            {
                Tier = BlobTier.Medium,
                Headlines = new[]
                {
                    "Polis yuvarlak şüpheliyi arıyor: \"Kırmızı, sinirli, aç\"",
                    "Yayalar uyarıldı: Yuvarlak şeylere yaklaşmayın",
                    "Uzmanlar tartışıyor: Top mu, blob mu, yoksa çok büyük bir kiraz mı?",
                    "Otobüs durağı kayboldu, yolcular hâlâ bekliyor",
                    "Emniyet: \"Durum kontrol altında.\" Durum: Kontrol altında değil",
                    "Sosyal medyada #TopGeliyor gündemde",
                    "Özel Harekât göreve çağrıldı: Hedef yuvarlak, kırmızı ve çok aç",
                    "Özel Harekât komutanı: \"Kariyerimde ilk kez bir topu kovalıyoruz\"",
                    "Özel Harekât, topa karşı yeni taktik deniyor: \"Biz de yuvarlanacağız\"",
                },
            },
            new NewsHeadlines.TierHeadlines
            {
                Tier = BlobTier.Large,
                Headlines = new[]
                {
                    "SON DAKİKA: Dev top arabaları yemeye başladı",
                    "Sigorta şirketleri \"top hasarı\"nı poliçelerden çıkardı",
                    "Belediye başkanı: \"Topla müzakere masasına oturmaya hazırız\"",
                    "Elit polis birlikleri sahada, top hâlâ aç",
                    "Bilim insanları: \"Top büyümeye devam ederse... büyümeye devam edecek\"",
                    "Halk evlerine kapandı, marketlerde ekmek kalmadı",
                },
            },
            new NewsHeadlines.TierHeadlines
            {
                Tier = BlobTier.Giant,
                Headlines = new[]
                {
                    "SON DAKİKA: Top binaları yiyor",
                    "Saray açıklama yaptı: Kuğuyu da o yemiş",
                    "Hükümet olağanüstü toplandı, tek gündem maddesi: \"O top\"",
                    "Haritacılar şehri yeniden çiziyor: Artık daha az şehir var",
                    "Top, \"Yılın Kişisi\" adayları arasında",
                    "Dünya liderlerinden ortak bildiri: \"Lütfen bizi yeme\"",
                    "Uzmanlar tek bir tavsiyede birleşti: Koşun",
                    "Şehirde yiyecek bir şey kalmadı, top hâlâ aç",
                    "Aşçılar seferber oldu: \"Ne pişirsek yetmiyor, tabağı da yiyor\"",
                    "Bilim insanları: \"Topun midesi yok, sadece iştahı var\"",
                },
            },
        };

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Bone;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}
