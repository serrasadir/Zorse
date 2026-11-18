using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;

namespace BlobSurvivor.UI
{
    // Ayarlar: Müzik ve Efekt sesi (sürgü), Titreşim (aç/kapa). Lobiden (dişli) ve duraklatma menüsünden açılır.
    // Kök Canvas'ın üstünde tam ekran; düzen ilk açılışta kodla bir kez kurulur. Değerler GameSettings'te.
    public class SettingsPanel : MonoBehaviour
    {
        private static SettingsPanel s_instance;

        private static readonly Color Dim = new Color(0.02f, 0.02f, 0.08f, 0.75f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.95f);
        private static readonly Color Track = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Off = new Color(0.3f, 0.3f, 0.42f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.02f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.8f);

        private Sprite _rounded, _circle;
        private bool _k; // arayüz kiti kurulu (UiKit.Ready): mor başlıklı krem pencere, kit sürgü/hap/buton
        private Slider _music, _sfx;
        private Image _vibrationImage;
        private TMP_Text _vibrationText, _musicValue, _sfxValue;

        public static void Open(Component anyUnderCanvas, Sprite rounded)
        {
            if (s_instance == null)
            {
                Canvas canvas = anyUnderCanvas.GetComponentInParent<Canvas>(true);
                if (canvas == null) return;
                var go = new GameObject("SettingsPanel", typeof(RectTransform));
                go.layer = canvas.gameObject.layer;
                s_instance = go.AddComponent<SettingsPanel>();
                s_instance.Build((RectTransform)canvas.rootCanvas.transform, rounded);
            }
            s_instance.transform.SetAsLastSibling();
            s_instance.Refresh();
            s_instance.gameObject.SetActive(true);
        }

        private void Close()
        {
            GameSettings.Save();
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            _music.SetValueWithoutNotify(GameSettings.MusicVolume);
            _sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            _musicValue.text = Percent(GameSettings.MusicVolume);
            _sfxValue.text = Percent(GameSettings.SfxVolume);
            bool on = GameSettings.Vibration;
            _vibrationText.text = on ? "AÇIK" : "KAPALI";
            if (_k)
            {
                UiKit.Paint(_vibrationImage, on ? "Pills/pill_green" : "Pills/pill_gray");
                UiKit.FitBorder(_vibrationImage);
                return;
            }
            _vibrationImage.color = on ? Gold : Off;
            _vibrationText.color = on ? Ink : Color.white;
        }

        private static string Percent(float v) => $"%{Mathf.RoundToInt(v * 100f)}";

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build(RectTransform root, Sprite rounded)
        {
            _rounded = rounded;
            _k = UiKit.Ready;
            _circle = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            var rect = (RectTransform)transform;
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Karartma: dokunmayı tutar, kartın dışına dokunmak kapatır. Kart kardeş (çocuğu olsa tıklama ona da çıkardı).
            RectTransform dim = Box("Dim", rect, Vector2.zero, Vector2.zero, Dim, null);
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            Image dimImage = dim.GetComponent<Image>();
            dimImage.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.targetGraphic = dimImage;
            dimButton.onClick.AddListener(Close);

            RectTransform card = Box("Card", rect, Vector2.zero, new Vector2(900f, 860f), CardColor, _rounded);
            Image cardImage = card.GetComponent<Image>();
            cardImage.raycastTarget = true; // karta dokunmak kapatmasın
            if (_k)
            {
                UiKit.Paint(cardImage, "Panels/modal_purple");
                cardImage.pixelsPerUnitMultiplier = 0.65f; // mor başlık şeridi ~140 birim
            }

            // Kit: başlık mor şeridin ortasında (kart üstü 430, şerit ortası ~80 aşağıda).
            TMP_Text title = Text("Title", card, new Vector2(0f, _k ? 352f : 340f), new Vector2(800f, 100f), _k ? 66f : 76f, TextAlignmentOptions.Center, Color.white);
            if (_k) UiKit.Outlined(title, Color.white, true);
            title.text = "AYARLAR";

            _music = SliderRow(card, 180f, "Müzik", out _musicValue, v => { GameSettings.MusicVolume = v; _musicValue.text = Percent(v); });
            _sfx = SliderRow(card, 20f, "Efekt", out _sfxValue, v => { GameSettings.SfxVolume = v; _sfxValue.text = Percent(v); });

            TMP_Text vibLabel = Text("VibrationLabel", card, new Vector2(_k ? LabelXKit : -180f, -140f), new Vector2(400f, 70f), 46f, TextAlignmentOptions.Left, Color.white);
            vibLabel.text = "Titreşim";
            if (_k) RowKit(card, vibLabel, -140f, "Glyphs/vibration");
            RectTransform pill = Box("VibrationToggle", card, new Vector2(240f, -140f), new Vector2(260f, 96f), Gold, _rounded);
            _vibrationImage = pill.GetComponent<Image>();
            _vibrationImage.raycastTarget = true;
            var toggle = pill.gameObject.AddComponent<Button>();
            toggle.targetGraphic = _vibrationImage;
            toggle.onClick.AddListener(() =>
            {
                GameSettings.Vibration = !GameSettings.Vibration;
                Refresh();
                Haptics.Impact(1); // açınca hissettir (kapalıyken çalmaz)
            });
            _vibrationText = Text("Label", pill, Vector2.zero, pill.sizeDelta, 40f, TextAlignmentOptions.Center, Ink);
            if (_k)
            {
                toggle.transition = Selectable.Transition.ColorTint; // hap rengi sprite'ta; basılınca hafif koyulaşır
                ColorBlock c = toggle.colors;
                c.normalColor = c.highlightedColor = c.selectedColor = Color.white;
                c.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
                toggle.colors = c;
                _vibrationText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                UiKit.Outlined(_vibrationText, Color.white);
            }

            RectTransform close = Box("CloseButton", card, new Vector2(0f, -320f), new Vector2(560f, 120f), Gold, _rounded);
            Image closeImage = close.GetComponent<Image>();
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            TMP_Text closeLabel = Text("Label", close, Vector2.zero, close.sizeDelta, 50f, TextAlignmentOptions.Center, Ink);
            closeLabel.text = "TAMAM";
            if (_k)
            {
                UiKit.StyleButton(closeButton, "green");
                if (closeImage.sprite != null) closeImage.pixelsPerUnitMultiplier = closeImage.sprite.rect.height / close.sizeDelta.y;
                closeLabel.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                UiKit.Outlined(closeLabel, Color.white);
            }
        }

        private const float LabelXKit = -100f; // kit: satır yazısı solundaki glife yer açar

        // Kit satırı: solda mor glif, koyu yazı (krem zemin).
        private static void RowKit(RectTransform card, TMP_Text label, float y, string glyph)
        {
            Image icon = UiKit.NewImage(label.name + "Icon", card, new Vector2(-366f, y), new Vector2(68f, 68f), glyph);
            icon.color = UiKit.Purple;
            UiKit.Text(label, true, UiKit.Ink);
        }

        private Slider SliderRow(RectTransform card, float y, string label, out TMP_Text value, UnityEngine.Events.UnityAction<float> onChange)
        {
            TMP_Text rowLabel = Text(label + "Label", card, new Vector2(_k ? LabelXKit : -180f, y + 50f), new Vector2(400f, 60f), 46f, TextAlignmentOptions.Left, Color.white);
            rowLabel.text = label;
            value = Text(label + "Value", card, new Vector2(290f, y + 50f), new Vector2(160f, 60f), 40f, TextAlignmentOptions.Right, Subtle);
            if (_k)
            {
                RowKit(card, rowLabel, y + 50f, label == "Müzik" ? "Glyphs/music" : "Glyphs/sound");
                UiKit.Text(value, true, UiKit.InkSoft);
            }

            // Dokunma alanı geniş (90), görünen ray ince (24).
            RectTransform area = NewRect(label + "Slider", card, new Vector2(0f, y - 20f), new Vector2(740f, 90f));
            var hit = area.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            RectTransform track = Box("Track", area, Vector2.zero, Vector2.zero, Track, _rounded);
            track.anchorMin = new Vector2(0f, 0.5f); track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, _k ? 44f : 24f);
            if (_k) KitBar(track.GetComponent<Image>(), "Bars/bar_track", 44f);

            RectTransform fillArea = NewRect("FillArea", area, Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(0f, 24f);
            RectTransform fill = Box("Fill", fillArea, Vector2.zero, Vector2.zero, Gold, _rounded);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = Vector2.zero;
            if (_k)
            {
                fillArea.sizeDelta = new Vector2(-12f, 28f); // dolgu rayın oluğunda
                KitBar(fill.GetComponent<Image>(), "Bars/bar_fill_yellow", 28f);
            }

            RectTransform handleArea = NewRect("HandleArea", area, Vector2.zero, Vector2.zero);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(30f, 0f); handleArea.offsetMax = new Vector2(-30f, 0f);
            RectTransform handle = Box("Handle", handleArea, Vector2.zero, new Vector2(60f, 60f), Color.white, _circle != null ? _circle : _rounded);
            handle.anchorMin = handle.anchorMax = new Vector2(0f, 0.5f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.preserveAspect = _circle != null;
            if (_k && UiKit.Get("Buttons/round_gold") != null)
            {
                handle.sizeDelta = new Vector2(76f, 76f);
                UiKit.Paint(handleImage, "Buttons/round_gold");
            }

            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        // Yatay 9-slice bar: kenar payı verilen yüksekliğe orantılı (FitBorder'ın rect'e bakmadan hali).
        private static void KitBar(Image img, string key, float height)
        {
            UiKit.Paint(img, key);
            if (img.sprite != null && img.sprite.border.y == 0f && img.sprite.border.w == 0f)
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / height;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; }
            return rect;
        }

        private static TMP_Text Text(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = alignment;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
